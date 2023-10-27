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
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ExileCore.Shared.Enums;
using FilterCore;
using SDxVector2 = SharpDX.Vector2;

namespace PickIt;

public partial class PickIt : BaseSettingsPlugin<PickItSettings>
{
    private const string PickitRuleDirectory = "Pickit Rules";
    private readonly TimeCache<List<LabelOnGround>> _chestLabels;
    private readonly TimeCache<List<ItemData>> _itemLabels;
    private readonly CachedValue<LabelOnGround> _portalLabel;
    private readonly CachedValue<bool[,]> _inventorySlotsCache;
    private ServerInventory _inventoryItems;
    private SyncTask<bool> _pickUpTask;
    private List<ItemFilterData> _itemFilterData;
    private bool[,] InventorySlots => _inventorySlotsCache.Value;
    private readonly Stopwatch _sinceLastClick = Stopwatch.StartNew();

    public PickIt()
    {
        Name = "PickIt";
        _inventorySlotsCache = new FrameCache<bool[,]>(() => GetContainer2DArray(_inventoryItems));
        _itemLabels = new TimeCache<List<ItemData>>(UpdateCurrentLabels, 500);
        _chestLabels = new TimeCache<List<LabelOnGround>>(UpdateChestList, 200);
        _portalLabel = new TimeCache<LabelOnGround>(() => GetLabel(@"Metadata/MiscellaneousObjects/.*Portal"), 200);
    }

    public override bool Initialise()
    {
        #region Register keys

        Settings.PickUpKey.OnValueChanged += () => Input.RegisterKey(Settings.PickUpKey);
        Input.RegisterKey(Settings.PickUpKey);
        Input.RegisterKey(Keys.Escape);

        #endregion

        Settings.FilterFile.OnValueSelected = _ => LoadRuleFiles();
        Settings.ReloadFilters.OnPressed = LoadRuleFiles;
        LoadRuleFiles();
        return true;
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
            !Settings.Enable ||
            Input.GetKeyState(Keys.Escape))
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

        return null;
    }

    public override void Render()
    {
        if (GetWorkMode() != WorkMode.Stop)
        {
            TaskUtils.RunOrRestart(ref _pickUpTask, RunPickerIterationAsync);
        }
        else
        {
            _pickUpTask = null;
        }
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

    private bool DoWePickThis(ItemData item, List<ItemFilterData> filterData)
    {
        foreach (var cachedQuery in filterData)
        {
            try
            {
                var result = cachedQuery.CompiledQuery.DynamicInvoke(item);
                if (result is bool && (bool)result)
                {
                    DebugWindow.LogMsg($"Evaluation Result: ({(bool)result}) Line # {cachedQuery.InitialLine} Entry({cachedQuery.Query}) on Item({item.BaseName})", 10);
                    return true; // Stop further checks once a match is found
                }
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Evaluation Error! Line # {cachedQuery.InitialLine} Entry: '{cachedQuery.Query}' Trigger Item {item.BaseName}\n{ex.StackTrace}");
                return false;
            }
        }
        return false;
    }

    private List<ItemData> UpdateCurrentLabels()
    {
        var window = GameController.Window.GetWindowRectangleTimeCache with { Location = SDxVector2.Zero };
        var labels = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabelsVisible;

        return labels?.Where(x => x.Address != 0 && x.ItemOnGround?.Path != null && x.IsVisible
                                  && window.Contains(x.Label.GetClientRectCache.Center)
                                  && (Settings.IgnoreCanPickUp || x.CanPickUp)
                                  && x.MaxTimeForPickUp.TotalSeconds <= 0)
            .Select(x => new ItemData(x, GameController.Files))
            .ToList();
    }

    private List<LabelOnGround> UpdateChestList() =>
        GameController?.Game?.IngameState?.IngameUi?.ItemsOnGroundLabelsVisible
            .Where(x => x.Address != 0 &&
                        x.IsVisible &&
                        (Settings.IgnoreCanPickUp || x.CanPickUp) &&
                        x.ItemOnGround.Path is {} path && 
                        (path.StartsWith("Metadata/Chests/LeaguesExpedition/") || 
                         path.StartsWith("Metadata/Chests/LegionChests/") ||
                         path.StartsWith("Metadata/Chests/Blight") ||
                         path.StartsWith("Metadata/Chests/Breach/") ||
                         path.StartsWith("Metadata/Chests/IncursionChest")) &&
                        x.ItemOnGround.HasComponent<Chest>())
            .OrderBy(x => x.ItemOnGround.DistancePlayer)
            .ToList();

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

    private bool ShouldLazyLoot(ItemData item)
    {
        var itemPos = item.LabelOnGround.ItemOnGround.PosNum;
        var playerPos = GameController.Player.PosNum;
        return Math.Abs(itemPos.Z - playerPos.Z) <= 50 &&
               itemPos.Xy().DistanceSquared(playerPos.Xy()) <= 275 * 275;
    }

    private bool IsLabelClickable(LabelOnGround label)
    {
        if (label?.Label is not { IsValid: true, IsVisible: true, IndexInParent: not null } validLabel)
        {
            return false;
        }

        var vector3 = validLabel.GetClientRect().Center;

        var gameWindowRect = GameController.Window.GetWindowRectangleTimeCache with { Location = SDxVector2.Zero };
        gameWindowRect.Inflate(-36, -36);
        return gameWindowRect.Contains(vector3.X, vector3.Y);
    }

    private bool IsPortalTargeted(LabelOnGround portalLabel)
    {
        if (portalLabel == null)
        {
            return false;
        }

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

    private static bool IsPortalNearby(LabelOnGround portalLabel, LabelOnGround pickItItem)
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
            where labelOnGround?.Label is { IsValid: true, Address: > 0, IsVisible: true }
            let itemOnGround = labelOnGround.ItemOnGround
            where itemOnGround?.Metadata is { } metadata && regex.IsMatch(metadata)
            let dist = GameController?.Player?.GridPosNum.DistanceSquared(itemOnGround.GridPosNum)
            orderby dist
            select labelOnGround;

        return labelQuery.FirstOrDefault();
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
                _itemFilterData = ItemFilter.Load(filterFilePath);
            }
            else
            {
                _itemFilterData = null;
                LogError("Item filter file not found, plugin will not work");
            }
        }
    }

    private async SyncTask<bool> RunPickerIterationAsync()
    {
        if (!GameController.Window.IsForeground()) return true;
        var pickUpThisItem = _itemLabels
            .Value?
            .Where(x => x.GroundItem != null
                        && x.AttemptedPickups == 0
                        && x.Distance < Settings.PickupRange
                        && IsLabelClickable(x.LabelOnGround)
                        && DoWePickThis(x, _itemFilterData)
                        && (Settings.PickUpWhenInventoryIsFull || CanFitInventory(x)))
            .MinBy(x => x.Distance);

        var workMode = GetWorkMode();
        if (workMode == WorkMode.Manual || workMode == WorkMode.Lazy && ShouldLazyLoot(pickUpThisItem))
        {
            if (Settings.ExpeditionChests.Value)
            {
                var chestLabel = _chestLabels?.Value.FirstOrDefault(x =>
                    x.ItemOnGround.DistancePlayer < Settings.PickupRange && x.ItemOnGround != null &&
                    IsLabelClickable(x));

                if (chestLabel != null && (pickUpThisItem == null || pickUpThisItem.Distance >= chestLabel.ItemOnGround.DistancePlayer))
                {
                    await PickAsync(chestLabel);
                    return true;
                }
            }

            if (pickUpThisItem is not { IsValid: true })
            {
                return true;
            }

            pickUpThisItem.AttemptedPickups++;
            await PickAsync(pickUpThisItem.LabelOnGround);
        }

        return true;
    }

    private async SyncTask<bool> PickAsync(LabelOnGround label)
    {
        var tryCount = 0;
        while (tryCount < 3)
        {
            if (!IsLabelClickable(label))
            {
                _itemLabels.ForceUpdate();
                _chestLabels.ForceUpdate();
                return true;
            }

            if (GameController.Player.GetComponent<Actor>().isMoving)
            {
                await TaskUtils.NextFrame();
                continue;
            }

            var completeItemLabel = label.Label;
            var vector2 = completeItemLabel.GetClientRect().ClickRandomNum(5, 3) + GameController.Window.GetWindowRectangleTimeCache.TopLeft.ToVector2Num();
            if (_sinceLastClick.ElapsedMilliseconds > Settings.PauseBetweenClicks)
            {
                if (!IsTargeted(label))
                {
                    await SetCursorPositionAsync(vector2, label);
                }
                else
                {
                    // in case of portal nearby do extra checks with delays
                    if (IsPortalNearby(_portalLabel.Value, label))
                    {
                        if (IsPortalTargeted(_portalLabel.Value))
                        {
                            return true;
                        }

                        await Task.Delay(25);
                        if (IsPortalTargeted(_portalLabel.Value))
                        {
                            return true;
                        }
                    }

                    Input.Click(MouseButtons.Left);
                    _sinceLastClick.Restart();
                    tryCount++;
                }
            }

            await TaskUtils.NextFrame();
        }

        return true;
    }

    private static bool IsTargeted(LabelOnGround label)
    {
        return label?.ItemOnGround?.GetComponent<Targetable>()?.isTargeted == true;
    }

    private static async SyncTask<bool> SetCursorPositionAsync(Vector2 vector2, LabelOnGround item)
    {
        DebugWindow.LogMsg($"Set cursor pos: {vector2}");
        Input.SetCursorPos(vector2);
        return await TaskUtils.CheckEveryFrame(() => IsTargeted(item), new CancellationTokenSource(60).Token);
    }

    #endregion
}