using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
using Random_Features.Libs;
using SharpDX;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ExileCore.PoEMemory;
using Input = ExileCore.Input;
using nuVector2 = System.Numerics.Vector2;
using ExileCore.Shared.Enums;
using FilterCore;

// ReSharper disable ConstantConditionalAccessQualifier

namespace PickIt
{
    public class PickIt : BaseSettingsPlugin<PickItSettings>
    {
        private const string PickitRuleDirectory = "Pickit Rules";
        private TimeCache<List<LabelOnGround>> ChestLabelCacheList { get; set; }
        private readonly WaitRandom _toPick = new WaitRandom(100, 150, "topick");
        private readonly YieldBase _wait2Ms = new WaitTime(2, "2ms");
        private ItemFilterProcessor _filterProcessor;
        private Dictionary<string, int> _weightsRules = new Dictionary<string, int>();
        private WaitTime _workCoroutine;
        private uint coroutineCounter;
        private bool _fullWork = true;
        private Coroutine _pickItCoroutine;
        public int[,] inventorySlots { get; set; } = new int[0,0];
        public ServerInventory InventoryItems { get; set; }
        public static PickIt Controller { get; set; }
        private TimeCache<List<CustomItem>> _currentLabels;

        public PickIt()
        {
            Name = "Pickit";
        }

        private List<string> PickitFiles { get; set; }

        public override bool Initialise()
        {
            _currentLabels = new TimeCache<List<CustomItem>>(UpdateCurrentLabels, 500); // alexs idea <3
            
            #region Register keys

            Settings.PickUpKey.OnValueChanged += () => Input.RegisterKey(Settings.PickUpKey);
            Input.RegisterKey(Settings.PickUpKey);
            Input.RegisterKey(Keys.Escape);

            #endregion
            
            Controller = this;
            _pickItCoroutine = new Coroutine(MainWorkCoroutine(), this, "Pick It");
            Core.ParallelRunner.Run(_pickItCoroutine);
            _pickItCoroutine.Pause();
            _workCoroutine = new WaitTime(Settings.ExtraDelay);
            Settings.ExtraDelay.OnValueChanged += (_, i) => _workCoroutine = new WaitTime(i);
            ChestLabelCacheList = new TimeCache<List<LabelOnGround>>(UpdateChestList, 200);
            LoadRuleFiles();
            return true;
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

                coroutineCounter++;
                _pickItCoroutine.UpdateTicks(coroutineCounter);
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

        public override void DrawSettings()
        {
            Settings.ShowInventoryView.Value = ImGuiExtension.Checkbox("Show Inventory Slots", Settings.ShowInventoryView.Value);
            Settings.MoveInventoryView.Value = ImGuiExtension.Checkbox("Moveable Inventory Slots", Settings.MoveInventoryView.Value);

            Settings.PickUpKey = ImGuiExtension.HotkeySelector("Pickup Key: " + Settings.PickUpKey.Value.ToString(), Settings.PickUpKey);
            Settings.LeftClickToggleNode.Value = ImGuiExtension.Checkbox("Mouse Button: " + (Settings.LeftClickToggleNode ? "Left" : "Right"), Settings.LeftClickToggleNode);
            Settings.PickUpEvenInventoryFull.Value = ImGuiExtension.Checkbox("Try to pickup even if the item does not fit in the inventory", Settings.PickUpEvenInventoryFull);
            Settings.PickupRange.Value = ImGuiExtension.IntSlider("Pickup Radius", Settings.PickupRange);
            Settings.ExtraDelay.Value = ImGuiExtension.IntSlider("Extra Click Delay", Settings.ExtraDelay);
            Settings.LazyLooting.Value = ImGuiExtension.Checkbox("Use Lazy Looting", Settings.LazyLooting);
            if (Settings.LazyLooting)
                Settings.NoLazyLootingWhileEnemyClose.Value = ImGuiExtension.Checkbox("No lazy looting while enemy is close", Settings.NoLazyLootingWhileEnemyClose);
            Settings.LazyLootingPauseKey.Value = ImGuiExtension.HotkeySelector("Pause lazy looting for 2 sec: " + Settings.LazyLootingPauseKey.Value, Settings.LazyLootingPauseKey);

            if (ImGui.CollapsingHeader("Pickit Rules", ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Button("Reload All Files")) LoadRuleFiles();
                bool tempRef;
                Settings.FilterFile = ImGuiExtension.ComboBox("Item filter file", Settings.FilterFile, PickitFiles, out tempRef);
                if (tempRef) LoadRuleFiles();
                if (tempRef) _weightsRules = LoadWeights(Settings.WeightRuleFile);
            }

            if (ImGui.CollapsingHeader("Item Logic", ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.TreeNode("Overrides"))
                {
                    Settings.UseWeight.Value = ImGuiExtension.Checkbox("Use Weight", Settings.UseWeight);
                    Settings.PickUpEverything.Value = ImGuiExtension.Checkbox("Pickup Everything", Settings.PickUpEverything);
                    ImGui.TreePop();
                }
                Settings.ExpeditionChests.Value = ImGuiExtension.Checkbox("Expedition Chests", Settings.ExpeditionChests);
            }
        }

        private DateTime DisableLazyLootingTill { get; set; }

        public override Job Tick()
        {
            var playerInvCount = GameController?.Game?.IngameState?.ServerData?.PlayerInventories?.Count;
            if (playerInvCount == null || playerInvCount == 0)
                return null;

            InventoryItems = GameController.Game.IngameState.ServerData.PlayerInventories[0].Inventory;
            inventorySlots = Misc.GetContainer2DArray(InventoryItems);
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
                    var firstOrDefault =
                        Core.ParallelRunner.Coroutines.FirstOrDefault(x => x.OwnerName == nameof(PickIt));

                    if (firstOrDefault != null)
                        _pickItCoroutine = firstOrDefault;
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

            var _opened = true;

            var MoveableFlag = ImGuiWindowFlags.NoScrollbar |
                               ImGuiWindowFlags.NoTitleBar |
                               ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoSavedSettings;

            var NonMoveableFlag = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground |
                                  ImGuiWindowFlags.NoTitleBar |
                                  ImGuiWindowFlags.NoInputs |
                                  ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoSavedSettings;

            ImGui.SetNextWindowPos(Settings.InventorySlotsVector2, ImGuiCond.Always, nuVector2.Zero);

            if (ImGui.Begin($"{Name}", ref _opened,
                Settings.MoveInventoryView.Value ? MoveableFlag : NonMoveableFlag))
            {
                var _numb = 1;
                for (var i = 0; i < 5; i++)
                for (var j = 0; j < 12; j++)
                {
                    var toggled = Convert.ToBoolean(inventorySlots[i, j]);
                    if (ImGui.Checkbox($"##{_numb}IgnoredCells", ref toggled)) inventorySlots[i, j] ^= 1;

                    if ((_numb - 1) % 12 < 11) ImGui.SameLine();

                    _numb += 1;
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
        public override void ReceiveEvent(string eventId, object args)
        {
        }

        private List<CustomItem> UpdateCurrentLabels()
        {
            var window = GameController.Window.GetWindowRectangleTimeCache;
            var rect = new RectangleF(window.X, window.X, window.X + window.Width, window.Y + window.Height);
            var labels = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabelsVisible;

            if (Settings.UseWeight)
            {
                return labels.Where(x => x.Address != 0 && x.ItemOnGround?.Path != null && x.IsVisible
                             && x.Label.GetClientRectCache.Center.PointInRectangle(rect)
                             && x.CanPickUp && x.MaxTimeForPickUp.TotalSeconds <= 0)
                             .Select(x => new CustomItem(x, GameController.Files, x.ItemOnGround.DistancePlayer, _weightsRules))
                             .OrderByDescending(x => x.Weight)
                             .ThenBy(x => x.Distance)
                             .ToList();
            }
            else
            {
                return labels.Where(x => x.Address != 0 && x.ItemOnGround?.Path != null && x.IsVisible
                             && x.Label.GetClientRectCache.Center.PointInRectangle(rect)
                             && x.CanPickUp && x.MaxTimeForPickUp.TotalSeconds <= 0)
                             .Select(x => new CustomItem(x, GameController.Files, x.ItemOnGround.DistancePlayer, _weightsRules))
                             .OrderBy(x => x.Distance)
                             .ToList();
            }
        }
        
        private List<LabelOnGround> UpdateChestList() =>
            GameController?.Game?.IngameState?.IngameUi?.ItemsOnGroundLabelsVisible.Where(x => x.Address != 0 &&
                x.ItemOnGround?.Path != null &&
                x.IsVisible &&
                x.CanPickUp && x.ItemOnGround.Path.Contains("LeaguesExpedition") &&
                x.ItemOnGround.HasComponent<Chest>()).OrderBy(x => x.ItemOnGround.DistancePlayer).ToList();

        private IEnumerator FindItemToPick()
        {
            if (!GameController.Window.IsForeground()) yield break;
            var pickUpThisItem = _currentLabels.Value.FirstOrDefault(x => DoWePickThis(x)
                                                                       && x.Distance < Settings.PickupRange
                                                                       && x.GroundItem != null
                                                                       && IsLabelClickable(x.LabelOnGround)
                                                                       && (Settings.PickUpEvenInventoryFull || Misc.CanFitInventory(x)));

            var workMode = GetWorkMode();
            if (workMode == WorkMode.Manual || workMode == WorkMode.Lazy && ShouldLazyLoot(pickUpThisItem))
            {
                if (Settings.ExpeditionChests.Value)
                {
                    var chestLabel = ChestLabelCacheList?.Value.FirstOrDefault(x =>
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
            try { if (Settings.NoLazyLootingWhileEnemyClose && GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster]
                    .Any(x => x?.GetComponent<Monster>() != null && x.IsValid && x.IsHostile && x.IsAlive
                    && !x.IsHidden && !x.Path.Contains("ElementalSummoned")
                    && Vector3.Distance(GameController.Player.Pos, x.GetComponent<Render>().Pos) < Settings.PickupRange)) return false;
            } catch (NullReferenceException) { }

            return true;
        }
        
        /// <summary>
        /// LazyLoot item dependent checks
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool ShouldLazyLoot(CustomItem item)
        {
            var itemPos = item.LabelOnGround.ItemOnGround.Pos;
            var playerPos = GameController.Player.Pos;
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
                        DebugWindow.LogDebug(">next");
                        yield break;
                    }
                    yield return new WaitTime(100, "ismoving");
                }

                if (wasMoving)
                {
                    yield return new WaitTime(100, "topickaftermove");
                }

                if (!IsLabelClickable(pickItItem.LabelOnGround))
                {
                    DebugWindow.LogDebug(">next");
                    yield break;
                }

                var completeItemLabel = pickItItem.LabelOnGround.Label;
                var vector2 = completeItemLabel.GetClientRect().ClickCenterRandom(5, 3) + GameController.Window.GetWindowRectangleTimeCache.TopLeft;
                if (!pickItItem.IsTargeted())
                    yield return SmartSetCursorPosition(vector2);
                if (pickItItem.IsTargeted())
                {
                    // in case of portal nearby do extra checks with delays
                    if (IsPortalNearby(portalLabel, pickItItem.LabelOnGround))
                    {
                        if (IsPortalTargeted(portalLabel))
                        {
                            yield break;
                        }

                        yield return new WaitTime(25, "portal");
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
                else
                {
                    DebugWindow.LogDebug("item not targeted!");
                }

                yield return _toPick;
                tryCount++;
            }
            DebugWindow.LogDebug("tries 3");
        }

        private static YieldBase SmartSetCursorPosition(Vector2 vector2)
        {
            void Move()
            {
                var enume = Input.SetCursorPositionSmooth(vector2);
                while (enume.MoveNext())
                {
                    Thread.Sleep(10);
                }

                Thread.Sleep(50);
            }
            DebugWindow.LogDebug("Moving mouse");
            var moveTask = Task.Run(Move);
            return new WaitFunction(() => !moveTask.IsCompleted);
        }

        private bool IsLabelClickable(LabelOnGround label)
        {
            var completeItemLabel = label?.Label;
            if (completeItemLabel == null)
            {
                return false;
            }

            var vector3 = completeItemLabel.GetClientRect().Center;

            var gameWindowRect = GameController.Window.GetWindowRectangleTimeCache;
            gameWindowRect.Location = Vector2.Zero;
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

            if (!string.IsNullOrWhiteSpace(Settings.FilterFile))
            {
                var filterFilePath = Path.Combine(pickitConfigFileDirectory, $"{Settings.FilterFile}.txt");
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

            var dirInfo = new DirectoryInfo(pickitConfigFileDirectory);
            PickitFiles = dirInfo.GetFiles("*.txt").Select(x => Path.GetFileNameWithoutExtension(x.Name)).ToList();
            _weightsRules = LoadWeights(Settings.WeightRuleFile);
        }

        public Dictionary<string, int> LoadWeights(string fileName)
        {
            var result = new Dictionary<string, int>();
            var filePath = $@"{DirectoryFullName}\{PickitRuleDirectory}\{fileName}.txt";
            if (!File.Exists(filePath)) return result;

            var lines = File.ReadAllLines(filePath);

            foreach (var x in lines.Where(x => !string.IsNullOrEmpty(x) && !x.StartsWith("#") && x.IndexOf('=') > 0))
            {
                try
                {
                    var s = x.Split('=');
                    if (s.Length == 2) result[s[0].Trim()] = int.Parse(s[1]);
                }
                catch (Exception e)
                {
                    DebugWindow.LogError($"{nameof(PickIt)} => Error when parsing weight: {e}");
                }
            }

            LogMessage($"PICKIT :: (Re)Loaded {fileName}", 5, Color.Cyan);
            return result;
        }

        public override void OnPluginDestroyForHotReload()
        {
            _pickItCoroutine.Done(true);
        }

        #endregion
    }
}