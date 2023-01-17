using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using System;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
using ExileCore.Shared.Enums;
using FilterCore;
using SDxVector2 = SharpDX.Vector2;

namespace PickIt;

public partial class PickIt : BaseSettingsPlugin<PickItSettings>
{
    private const string PickitRuleDirectory = "Pickit Rules";
    private readonly TimeCache<List<LabelOnGround>> _chestLabelCacheList;
    private readonly TimeCache<List<CustomItem>> _currentLabels;
    private readonly CachedValue<bool[,]> _inventorySlotsCache;
    private readonly WaitRandom _toPick = new WaitRandom(100, 150);
    private readonly YieldBase _wait2Ms = new WaitTime(2);
    private ItemFilterProcessor _filterProcessor;
    private WaitTime _workCoroutine;
    private uint _coroutineCounter;
    private bool _fullWork = true;
    private Coroutine _pickItCoroutine;
    private ServerInventory _inventoryItems;
    private bool[,] InventorySlots => _inventorySlotsCache.Value;

    public PickIt()
    {
        Name = "PickIt";
        _inventorySlotsCache = new FrameCache<bool[,]>(() => GetContainer2DArray(_inventoryItems));
        _currentLabels = new TimeCache<List<CustomItem>>(UpdateCurrentLabels, 500);
        _chestLabelCacheList = new TimeCache<List<LabelOnGround>>(UpdateChestList, 200);
    }

    public override bool Initialise()
    {
        #region Register keys

        Settings.PickUpKey.OnValueChanged += () => Input.RegisterKey(Settings.PickUpKey);
        Input.RegisterKey(Settings.PickUpKey);
        Input.RegisterKey(Keys.Escape);

        #endregion

        StartCoroutine();
        _pickItCoroutine.Pause();
        _workCoroutine = new WaitTime(Settings.ExtraDelay);
        Settings.ExtraDelay.OnValueChanged += (_, i) => _workCoroutine = new WaitTime(i);
        Settings.FilterFile.OnValueSelected = _ => LoadRuleFiles();
        Settings.ReloadFilters.OnPressed = LoadRuleFiles;
        LoadRuleFiles();
        return true;
    }

    private void StartCoroutine()
    {
        _pickItCoroutine = new Coroutine(MainWorkCoroutine(), this, "Pick It");
        Core.ParallelRunner.Run(_pickItCoroutine);
    }

    private IEnumerator MainWorkCoroutine()
    {
        while (true)
        {
            foreach (var item in FindItemToPick().Drill())
            {
                if (GetWorkMode() != WorkMode.Stop)
                {
                    yield return item;
                }
                else
                {
                    break;
                }
            }

            _coroutineCounter++;
            _pickItCoroutine.UpdateTicks(_coroutineCounter);

            yield return _workCoroutine;
        }
    }

    private enum WorkMode
    {
        Stop,
        Lazy,
        Manual
    }

    private WorkMode GetWorkMode()
    {
        if (!GameController.Window.IsForeground() ||
            !Settings.Enable)
        {
            return WorkMode.Stop;
        }

        if (Input.GetKeyState(Settings.PickUpKey.Value))
        {
            return WorkMode.Manual;
        }

        if (CanLazyLoot())
        {
            return WorkMode.Lazy;
        }

        return WorkMode.Stop;
    }

    private DateTime DisableLazyLootingTill { get; set; }

    public override Job Tick()
    {
        var playerInvCount = GameController?.Game?.IngameState?.Data?.ServerData?.PlayerInventories?.Count;
        if (playerInvCount is null or 0)
            return null;

        _inventoryItems = GameController.Game.IngameState.Data.ServerData.PlayerInventories[0].Inventory;
        DrawIgnoredCellsSettings();
        if (Input.GetKeyState(Settings.LazyLootingPauseKey)) DisableLazyLootingTill = DateTime.Now.AddSeconds(2);
        if (Input.GetKeyState(Keys.Escape))
        {
            _pickItCoroutine.Pause();
        }

        if (GetWorkMode() != WorkMode.Stop)
        {
            if (_pickItCoroutine.IsDone)
            {
                var firstOrDefault = Core.ParallelRunner.Coroutines.FirstOrDefault(x => x.OwnerName == nameof(PickIt));

                if (firstOrDefault != null)
                    _pickItCoroutine = firstOrDefault;
                else
                    StartCoroutine();
            }

            _pickItCoroutine.Resume();
            _fullWork = false;
        }
        else
        {
            if (_fullWork)
            {
                _pickItCoroutine.Pause();
            }
        }

        return null;
    }

    //TODO: Make function pretty
    private void DrawIgnoredCellsSettings()
    {
        if (!Settings.ShowInventoryView.Value)
            return;

        var opened = true;

        const ImGuiWindowFlags moveableFlag = ImGuiWindowFlags.NoScrollbar |
                                              ImGuiWindowFlags.NoTitleBar |
                                              ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoSavedSettings;

        const ImGuiWindowFlags nonMoveableFlag = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground |
                                                 ImGuiWindowFlags.NoTitleBar |
                                                 ImGuiWindowFlags.NoInputs |
                                                 ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoSavedSettings;

        ImGui.SetNextWindowPos(Settings.InventorySlotsVector2, ImGuiCond.Always, Vector2.Zero);

        if (ImGui.Begin($"{Name}", ref opened,
                Settings.MoveInventoryView.Value ? moveableFlag : nonMoveableFlag))
        {
            var numb = 0;
            for (var i = 0; i < 5; i++)
            for (var j = 0; j < 12; j++)
            {
                var toggled = Convert.ToBoolean(InventorySlots[i, j]);
                if (ImGui.Checkbox($"##{numb}IgnoredCells", ref toggled)) InventorySlots[i, j] = toggled;

                if (j != 11) ImGui.SameLine();

                numb += 1;
            }

            if (Settings.MoveInventoryView.Value)
                Settings.InventorySlotsVector2 = ImGui.GetWindowPos();

            ImGui.End();
        }
    }

    public bool DoWePickThis(CustomItem itemEntity)
    {
        if (!itemEntity.IsValid)
            return false;

        if (Settings.PickUpEverything)
        {
            return true;
        }

        return _filterProcessor?.ShowItem(itemEntity) ?? false;
    }

    private List<CustomItem> UpdateCurrentLabels()
    {
        var window = GameController.Window.GetWindowRectangleTimeCache with { Location = SDxVector2.Zero };
        var labels = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabelsVisible;

        return labels?.Where(x => x.Address != 0 && x.ItemOnGround?.Path != null && x.IsVisible
                                  && window.Contains(x.Label.GetClientRectCache.Center)
                                  && (Settings.IgnoreCanPickUp || x.CanPickUp)
                                  && x.MaxTimeForPickUp.TotalSeconds <= 0)
            .Select(x => new CustomItem(x, GameController.Files))
            .ToList();
    }

    private List<LabelOnGround> UpdateChestList() =>
        GameController?.Game?.IngameState?.IngameUi?.ItemsOnGroundLabelsVisible
            .Where(x => x.Address != 0 &&
                        x.IsVisible &&
                        (Settings.IgnoreCanPickUp || x.CanPickUp) &&
                        x.ItemOnGround.Path?.Contains("LeaguesExpedition") == true &&
                        x.ItemOnGround.HasComponent<Chest>())
            .OrderBy(x => x.ItemOnGround.DistancePlayer)
            .ToList();

    private IEnumerator FindItemToPick()
    {
        if (!GameController.Window.IsForeground()) yield break;
        var pickUpThisItem = _currentLabels
            .Value?
            .Where(x => x.GroundItem != null
                        && x.AttemptedPickups == 0
                        && x.Distance < Settings.PickupRange
                        && IsLabelClickable(x.LabelOnGround)
                        && DoWePickThis(x)
                        && (Settings.PickUpWhenInventoryIsFull || CanFitInventory(x)))
            .MinBy(x => x.Distance);

        var workMode = GetWorkMode();
        if (workMode == WorkMode.Manual || workMode == WorkMode.Lazy && ShouldLazyLoot(pickUpThisItem))
        {
            if (Settings.ExpeditionChests.Value)
            {
                var chestLabel = _chestLabelCacheList?.Value.FirstOrDefault(x =>
                    x.ItemOnGround.DistancePlayer < Settings.PickupRange && x.ItemOnGround != null &&
                    IsLabelClickable(x));

                if (chestLabel != null && (pickUpThisItem == null || pickUpThisItem.Distance >= chestLabel.ItemOnGround.DistancePlayer))
                {
                    yield return TryToOpenExpeditionChest(chestLabel);
                    _fullWork = true;
                    yield break;
                }
            }

            if (pickUpThisItem == null)
            {
                _fullWork = true;
                yield break;
            }

            var portalLabel = GetLabel(@"Metadata/MiscellaneousObjects/.*Portal");
            pickUpThisItem.AttemptedPickups++;
            yield return TryToPickV2(pickUpThisItem, portalLabel);
        }
    }

    /// <summary>
    /// LazyLoot item independent checks
    /// </summary>
    /// <returns></returns>
    private bool CanLazyLoot()
    {
        if (!Settings.LazyLooting) return false;
        if (DisableLazyLootingTill > DateTime.Now) return false;
        try
        {
            if (Settings.NoLazyLootingWhileEnemyClose && GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster]
                    .Any(x => x?.GetComponent<Monster>() != null && x.IsValid && x.IsHostile && x.IsAlive
                              && !x.IsHidden && !x.Path.Contains("ElementalSummoned")
                              && Vector3.Distance(GameController.Player.PosNum, x.GetComponent<Render>().PosNum) < Settings.PickupRange)) return false;
        }
        catch (NullReferenceException)
        {
        }

        return true;
    }

    /// <summary>
    /// LazyLoot item dependent checks
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    private bool ShouldLazyLoot(CustomItem item)
    {
        var itemPos = item.LabelOnGround.ItemOnGround.PosNum;
        var playerPos = GameController.Player.PosNum;
        if (Math.Abs(itemPos.Z - playerPos.Z) > 50) return false;
        var dx = itemPos.X - playerPos.X;
        var dy = itemPos.Y - playerPos.Y;
        if (dx * dx + dy * dy > 275 * 275) return false;

        if (item.IsHeist)
            return true;

        if (item.Rarity == ItemRarity.Rare && item.Width * item.Height > 1) return false;

        return true;
    }

    private IEnumerator TryToPickV2(CustomItem pickItItem, LabelOnGround portalLabel)
    {
        if (!pickItItem.IsValid)
        {
            //LogMessage("PickItem is not valid.", 5, Color.Red);
            yield break;
        }

        var tryCount = 0;
        while (tryCount < 3)
        {
            bool wasMoving = false;
            while (GameController.Player.GetComponent<Actor>().isMoving)
            {
                wasMoving = true;
                if (!IsLabelClickable(pickItItem.LabelOnGround))
                {
                    yield break;
                }

                yield return new WaitTime(100);
            }

            if (wasMoving)
            {
                yield return new WaitTime(100);
            }

            if (!IsLabelClickable(pickItItem.LabelOnGround))
            {
                yield break;
            }

            var completeItemLabel = pickItItem.LabelOnGround.Label;
            var vector2 = (completeItemLabel.GetClientRect().ClickCenterRandom(5, 3) + GameController.Window.GetWindowRectangleTimeCache.TopLeft).ToVector2Num();
            if (!pickItItem.IsTargeted())
                yield return SmartSetCursorPosition(vector2, pickItItem);
            if (pickItItem.IsTargeted())
            {
                // in case of portal nearby do extra checks with delays
                if (IsPortalNearby(portalLabel, pickItItem.LabelOnGround))
                {
                    if (IsPortalTargeted(portalLabel))
                    {
                        yield break;
                    }

                    yield return new WaitTime(25);
                    if (IsPortalTargeted(portalLabel))
                    {
                        yield break;
                    }
                }

                Input.Click(MouseButtons.Left);
                if (pickItItem.Distance < 10)
                {
                    //assume it was picked up
                    yield break;
                }
            }

            yield return _toPick;
            tryCount++;
        }
    }

    private static YieldBase SmartSetCursorPosition(Vector2 vector2, CustomItem item)
    {
        Input.SetCursorPos(vector2);
        return new WaitFunctionTimed(item.IsTargeted, maxWait: 60);
    }

    private bool IsLabelClickable(LabelOnGround label)
    {
        var completeItemLabel = label?.Label;
        if (completeItemLabel == null)
        {
            return false;
        }

        var vector3 = completeItemLabel.GetClientRect().Center;

        var gameWindowRect = GameController.Window.GetWindowRectangleTimeCache with { Location = SDxVector2.Zero };
        gameWindowRect.Inflate(-36, -36);
        if (!gameWindowRect.Contains(vector3.X, vector3.Y))
        {
            //LogMessage($"x,y outside game window. Label: {centerOfItemLabel} Window: {rectangleOfGameWindow}", 5, Color.Red);
            return false;
        }

        return true;
    }

    private bool IsPortalTargeted(LabelOnGround portalLabel)
    {
        // extra checks in case of HUD/game update. They are easy on CPU
        return
            GameController.IngameState.UIHover.Address == portalLabel.Address ||
            GameController.IngameState.UIHover.Address == portalLabel.ItemOnGround.Address ||
            GameController.IngameState.UIHover.Address == portalLabel.Label.Address ||
            GameController.IngameState.UIHoverElement.Address == portalLabel.Address ||
            GameController.IngameState.UIHoverElement.Address == portalLabel.ItemOnGround.Address ||
            GameController.IngameState.UIHoverElement.Address ==
            portalLabel.Label.Address || // this is the right one
            GameController.IngameState.UIHoverTooltip.Address == portalLabel.Address ||
            GameController.IngameState.UIHoverTooltip.Address == portalLabel.ItemOnGround.Address ||
            GameController.IngameState.UIHoverTooltip.Address == portalLabel.Label.Address ||
            portalLabel?.ItemOnGround?.HasComponent<Targetable>() == true &&
            portalLabel?.ItemOnGround?.GetComponent<Targetable>()?.isTargeted == true;
    }

    private bool IsPortalNearby(LabelOnGround portalLabel, LabelOnGround pickItItem)
    {
        if (portalLabel == null || pickItItem == null) return false;
        var rect1 = portalLabel.Label.GetClientRectCache;
        var rect2 = pickItItem.Label.GetClientRectCache;
        rect1.Inflate(100, 100);
        rect2.Inflate(100, 100);
        return rect1.Intersects(rect2);
    }

    private LabelOnGround GetLabel(string id)
    {
        var labels = GameController?.Game?.IngameState?.IngameUi?.ItemsOnGroundLabels;
        if (labels == null)
        {
            return null;
        }

        var regex = new Regex(id);
        var labelQuery =
            from labelOnGround in labels
            let label = labelOnGround?.Label
            where label?.IsValid == true &&
                  label?.Address > 0 &&
                  label?.IsVisible == true
            let itemOnGround = labelOnGround?.ItemOnGround
            where itemOnGround?.Metadata != null && regex.IsMatch(itemOnGround?.Metadata)
            let dist = GameController?.Player?.GridPos.DistanceSquared(itemOnGround.GridPos)
            orderby dist
            select labelOnGround;

        return labelQuery.FirstOrDefault();
    }

    private IEnumerator TryToOpenExpeditionChest(LabelOnGround labelOnGround)
    {
        var tryCount = 0;
        while (tryCount < 3)
        {
            if (!IsLabelClickable(labelOnGround)) yield break;

            var clientRectCenter = labelOnGround.Label.GetClientRect().Center;
            var vector2 = clientRectCenter + GameController.Window.GetWindowRectangleTimeCache.TopLeft;

            yield return Input.SetCursorPositionSmooth(vector2);
            yield return _wait2Ms;
            Input.Click(MouseButtons.Left);

            yield return _toPick;
            tryCount++;
        }
    }

    #region (Re)Loading Rules

    private void LoadRuleFiles()
    {
        var pickitConfigFileDirectory = Path.Combine(DirectoryFullName, PickitRuleDirectory);

        if (!Directory.Exists(pickitConfigFileDirectory))
        {
            Directory.CreateDirectory(pickitConfigFileDirectory);
            return;
        }

        var dirInfo = new DirectoryInfo(pickitConfigFileDirectory);
        Settings.FilterFile.Values = dirInfo.GetFiles("*.txt").Select(x => Path.GetFileNameWithoutExtension(x.Name)).ToList();
        if (Settings.FilterFile.Values.Any() && !Settings.FilterFile.Values.Contains(Settings.FilterFile.Value))
        {
            Settings.FilterFile.Value = Settings.FilterFile.Values.First();
        }

        if (!string.IsNullOrWhiteSpace(Settings.FilterFile.Value))
        {
            var filterFilePath = Path.Combine(pickitConfigFileDirectory, $"{Settings.FilterFile.Value}.txt");
            if (File.Exists(filterFilePath))
            {
                var fileContent = File.ReadAllLines(filterFilePath);
                var filter = new Filter(fileContent.ToList());
                _filterProcessor = new ItemFilterProcessor(filter);
            }
            else
            {
                _filterProcessor = null;
                LogError("Item filter file not found, plugin will not work");
            }
        }
    }

    public override void OnPluginDestroyForHotReload()
    {
        _pickItCoroutine.Done(true);
    }

    #endregion
}