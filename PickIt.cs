using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using ItemFilterLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SDxVector2 = SharpDX.Vector2;

namespace PickIt;

public partial class PickIt : BaseSettingsPlugin<PickItSettings>
{
    private readonly TimeCache<List<LabelOnGround>> _chestLabels;
    private readonly TimeCache<List<PickItItemData>> _itemLabels;
    private readonly CachedValue<LabelOnGround> _portalLabel;
    private readonly CachedValue<bool[,]> _inventorySlotsCache;
    private ServerInventory _inventoryItems;
    private SyncTask<bool> _pickUpTask;
    private List<ItemFilter> _itemFilters;
    private bool[,] InventorySlots => _inventorySlotsCache.Value;
    private readonly Stopwatch _sinceLastClick = Stopwatch.StartNew();

    public PickIt()
    {
        Name = "PickIt With Linq";
        _inventorySlotsCache = new FrameCache<bool[,]>(() => GetContainer2DArray(_inventoryItems));
        _itemLabels = new TimeCache<List<PickItItemData>>(UpdateCurrentLabels, 500);
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

        if (Settings.FilterTest.Value is { Length: > 0 } &&
            GameController.IngameState.UIHover is { Address: not 0 } h &&
            h.Entity.IsValid)
        {
            var f = ItemFilter.LoadFromString(Settings.FilterTest);
            var matched = f.Matches(new ItemData(h.Entity, GameController));
            DebugWindow.LogMsg($"Debug item match: {matched}");
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
                                              ImGuiWindowFlags.NoFocusOnAppearing;

        const ImGuiWindowFlags nonMoveableFlag = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground |
                                                 ImGuiWindowFlags.NoTitleBar |
                                                 ImGuiWindowFlags.NoInputs |
                                                 ImGuiWindowFlags.NoFocusOnAppearing;

        if (ImGui.Begin($"{Name}##InventoryCellMap", ref opened,
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

            ImGui.End();
        }
    }

    private bool DoWePickThis(PickItItemData item)
    {
        return Settings.PickUpEverything || (_itemFilters?.Any(filter => filter.Matches(item)) ?? false);
    }

    private List<PickItItemData> UpdateCurrentLabels()
    {
        var window = GameController.Window.GetWindowRectangleTimeCache with { Location = SDxVector2.Zero };
        var labels = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabelsVisible;

        return labels?.Where(x => x.Address != 0 && x.ItemOnGround?.Path != null && x.IsVisible
                                  && window.Contains(x.Label.GetClientRectCache.Center)
                                  && (Settings.IgnoreCanPickUp || x.CanPickUp)
                                  && x.MaxTimeForPickUp.TotalSeconds <= 0)
            .Select(x => new PickItItemData(x, GameController))
            .ToList();
    }

    private List<LabelOnGround> UpdateChestList() =>
        GameController?.Game?.IngameState?.IngameUi?.ItemsOnGroundLabelsVisible
            .Where(x => x.Address != 0 &&
                        x.IsVisible &&
                        (Settings.IgnoreCanPickUp || x.CanPickUp) &&
                        x.ItemOnGround.Path is { } path &&
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
            portalLabel.ItemOnGround?.HasComponent<Targetable>() == true &&
            portalLabel.ItemOnGround?.GetComponent<Targetable>()?.isTargeted == true;
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
        var pickitConfigFileDirectory = ConfigDirectory;
        var existingRules = Settings.PickitRules;

        if (!string.IsNullOrEmpty(Settings.CustomConfigDir))
        {
            var customConfigFileDirectory = Path.Combine(Path.GetDirectoryName(ConfigDirectory), Settings.CustomConfigDir);

            if (Directory.Exists(customConfigFileDirectory))
            {
                pickitConfigFileDirectory = customConfigFileDirectory;
            }
            else
            {
                DebugWindow.LogError("[Pickit] custom config folder does not exist.", 15);
            }
        }

        try
        {
            var newRules = new DirectoryInfo(pickitConfigFileDirectory).GetFiles("*.ifl")
                .Select(x => new PickitRule(x.Name, Path.GetRelativePath(pickitConfigFileDirectory, x.FullName), false))
                .ExceptBy(existingRules.Select(x => x.Location), x => x.Location)
                .ToList();
            foreach (var groundRule in existingRules)
            {
                var fullPath = Path.Combine(pickitConfigFileDirectory, groundRule.Location);
                if (File.Exists(fullPath))
                {
                    newRules.Add(groundRule);
                }
                else
                {
                    LogError($"File '{groundRule.Name}' not found.");
                }
            }

            _itemFilters = newRules
                .Where(rule => rule.Enabled)
                .Select(rule => ItemFilter.LoadFromPath(Path.Combine(pickitConfigFileDirectory, rule.Location)))
                .ToList();

            Settings.PickitRules = newRules;
        }
        catch (Exception e)
        {

        }
    }

    public record FilterDirItem(string Name, string Path);

    public override void DrawSettings()
    {
        base.DrawSettings();

        if (ImGui.Button("Open Build Folder"))
        {
            var configDir = ConfigDirectory;
            var customConfigFileDirectory = !string.IsNullOrEmpty(Settings.CustomConfigDir)
                ? Path.Combine(Path.GetDirectoryName(ConfigDirectory), Settings.CustomConfigDir)
                : null;

            var directoryToOpen = Directory.Exists(customConfigFileDirectory)
                ? customConfigFileDirectory
                : configDir;

            Process.Start("explorer.exe", directoryToOpen);
        }

        ImGui.Separator();
        ImGui.BulletText("Select Rules To Load");
        ImGui.BulletText("Ordering rule sets so general items will match first rather than last will improve performance");

        var tempNPCInvRules = new List<PickitRule>(Settings.PickitRules); // Create a copy

        for (int i = 0; i < tempNPCInvRules.Count; i++)
        {
            if (ImGui.ArrowButton($"##upButton{i}", ImGuiDir.Up) && i > 0)
                (tempNPCInvRules[i - 1], tempNPCInvRules[i]) = (tempNPCInvRules[i], tempNPCInvRules[i - 1]);

            ImGui.SameLine(); ImGui.Text(" "); ImGui.SameLine();

            if (ImGui.ArrowButton($"##downButton{i}", ImGuiDir.Down) && i < tempNPCInvRules.Count - 1)
                (tempNPCInvRules[i + 1], tempNPCInvRules[i]) = (tempNPCInvRules[i], tempNPCInvRules[i + 1]);

            ImGui.SameLine(); ImGui.Text(" - "); ImGui.SameLine();

            var refToggle = tempNPCInvRules[i].Enabled;
            if (ImGui.Checkbox($"{tempNPCInvRules[i].Name}##checkbox{i}", ref refToggle))
                tempNPCInvRules[i].Enabled = refToggle;
        }

        Settings.PickitRules = tempNPCInvRules;
    }

    private async SyncTask<bool> RunPickerIterationAsync()
    {
        if (!GameController.Window.IsForeground()) return true;
        var pickUpThisItem = _itemLabels
            .Value?
            .Where(x => x.Entity != null
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
                var chestLabel = _chestLabels?.Value.FirstOrDefault(x =>
                    x.ItemOnGround.DistancePlayer < Settings.PickupRange && x.ItemOnGround != null &&
                    IsLabelClickable(x));

                if (chestLabel != null && (pickUpThisItem == null || pickUpThisItem.Distance >= chestLabel.ItemOnGround.DistancePlayer))
                {
                    await PickAsync(chestLabel);
                    return true;
                }
            }

            if (pickUpThisItem == null)
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