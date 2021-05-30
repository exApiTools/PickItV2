using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using Random_Features.Libs;
using SharpDX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ExileCore.PoEMemory.Elements;
using Newtonsoft.Json;
using Input = ExileCore.Input;
using nuVector2 = System.Numerics.Vector2;
// ReSharper disable ConstantConditionalAccessQualifier

namespace PickIt
{
    public class PickIt : BaseSettingsPlugin<PickItSettings>
    {
        private const string PickitRuleDirectory = "Pickit Rules";
        private readonly List<Entity> _entities = new List<Entity>();
        private readonly Stopwatch _pickUpTimer = Stopwatch.StartNew();
        private readonly Stopwatch DebugTimer = Stopwatch.StartNew();
        private readonly WaitTime toPick = new WaitTime(1);
        private readonly WaitTime wait1ms = new WaitTime(1);
        private readonly WaitTime wait2ms = new WaitTime(2);
        private readonly WaitTime wait3ms = new WaitTime(3);
        private readonly WaitTime waitForNextTry = new WaitTime(1);
        private Vector2 _clickWindowOffset;
        private HashSet<string> _magicRules;
        private HashSet<string> _normalRules;
        private HashSet<string> _rareRules;
        private HashSet<string> _uniqueRules;
        private HashSet<string> _ignoreRules;
        private Dictionary<string, int> _weightsRules = new Dictionary<string, int>();
        private WaitTime _workCoroutine;
        public DateTime buildDate;
        private uint coroutineCounter;
        private Vector2 cursorBeforePickIt;
        private bool FullWork = true;
        private Element LastLabelClick;
        public string MagicRuleFile;
        private WaitTime mainWorkCoroutine = new WaitTime(5);
        public string NormalRuleFile;
        private Coroutine pickItCoroutine;
        public string RareRuleFile;
        private WaitTime tryToPick = new WaitTime(7);
        public string UniqueRuleFile;
        private WaitTime waitPlayerMove = new WaitTime(10);
        private List<string> _customItems = new List<string>();
        public int[,] inventorySlots { get; set; } = new int[0,0];
        public ServerInventory InventoryItems { get; set; }
        public static PickIt Controller { get; set; }
        public FRSetManagerPublishInformation FullRareSetManagerData = new FRSetManagerPublishInformation();
        private bool _enabled;

        public PickIt()
        {
            Name = "Pickit";
        }

        public string PluginVersion { get; set; }
        private List<string> PickitFiles { get; set; }

        public override bool Initialise()
        {
            #region Register keys

            Settings.PickUpKey.OnValueChanged += () => Input.RegisterKey(Settings.PickUpKey);
            Input.RegisterKey(Settings.PickUpKey);
            Input.RegisterKey(Keys.Escape);

            #endregion
            
            Controller = this;
            pickItCoroutine = new Coroutine(MainWorkCoroutine(), this, "Pick It");
            Core.ParallelRunner.Run(pickItCoroutine);
            pickItCoroutine.Pause();
            DebugTimer.Reset();
            _workCoroutine = new WaitTime(Settings.ExtraDelay);
            Settings.ExtraDelay.OnValueChanged += (sender, i) => _workCoroutine = new WaitTime(i);
            LoadRuleFiles();
            //LoadCustomItems();
            return true;
        }

        // bad idea to add hard coded pickups.
        private void LoadCustomItems()
        {
            _customItems.Add("Treasure Key");
            _customItems.Add("Silver Key");
            _customItems.Add("Golden Key");
            _customItems.Add("Flashpowder Keg");
            _customItems.Add("Divine Life Flask");
            _customItems.Add("Quicksilver Flask");
            _customItems.Add("Stone of Passage");
        }

        private IEnumerator MainWorkCoroutine()
        {
            while (true)
            {
                yield return FindItemToPick();

                coroutineCounter++;
                pickItCoroutine.UpdateTicks(coroutineCounter);
                yield return _workCoroutine;
            }
        }

        public override void DrawSettings()
        {
            Settings.ShowInventoryView.Value = ImGuiExtension.Checkbox("Show Inventory Slots", Settings.ShowInventoryView.Value);
            Settings.MoveInventoryView.Value = ImGuiExtension.Checkbox("Moveable Inventory Slots", Settings.MoveInventoryView.Value);

            Settings.PickUpKey = ImGuiExtension.HotkeySelector("Pickup Key: " + Settings.PickUpKey.Value.ToString(), Settings.PickUpKey);
            Settings.LeftClickToggleNode.Value = ImGuiExtension.Checkbox("Mouse Button: " + (Settings.LeftClickToggleNode ? "Left" : "Right"), Settings.LeftClickToggleNode);
            Settings.LeftClickToggleNode.Value = ImGuiExtension.Checkbox("Return Mouse To Position Before Click", Settings.ReturnMouseToBeforeClickPosition);
            Settings.GroundChests.Value = ImGuiExtension.Checkbox("Click Chests If No Items Around", Settings.GroundChests);
            Settings.PickupRange.Value = ImGuiExtension.IntSlider("Pickup Radius", Settings.PickupRange);
            Settings.ChestRange.Value = ImGuiExtension.IntSlider("Chest Radius", Settings.ChestRange);
            Settings.ExtraDelay.Value = ImGuiExtension.IntSlider("Extra Click Delay", Settings.ExtraDelay);
            Settings.MouseSpeed.Value = ImGuiExtension.FloatSlider("Mouse speed", Settings.MouseSpeed);
            Settings.TimeBeforeNewClick.Value = ImGuiExtension.IntSlider("Time wait for new click", Settings.TimeBeforeNewClick);

            if (ImGui.CollapsingHeader("Pickit Rules", ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Button("Reload All Files")) LoadRuleFiles();
                Settings.NormalRuleFile = ImGuiExtension.ComboBox("Normal Rules", Settings.NormalRuleFile, PickitFiles, out var tempRef);
                if (tempRef) _normalRules = LoadPickit(Settings.NormalRuleFile);
                Settings.MagicRuleFile = ImGuiExtension.ComboBox("Magic Rules", Settings.MagicRuleFile, PickitFiles, out tempRef);
                if (tempRef) _magicRules = LoadPickit(Settings.MagicRuleFile);
                Settings.RareRuleFile = ImGuiExtension.ComboBox("Rare Rules", Settings.RareRuleFile, PickitFiles, out tempRef);
                if (tempRef) _rareRules = LoadPickit(Settings.RareRuleFile);
                Settings.UniqueRuleFile = ImGuiExtension.ComboBox("Unique Rules", Settings.UniqueRuleFile, PickitFiles, out tempRef);
                if (tempRef) _uniqueRules = LoadPickit(Settings.UniqueRuleFile);
                Settings.WeightRuleFile = ImGuiExtension.ComboBox("Weight Rules", Settings.WeightRuleFile, PickitFiles, out tempRef);
                if (tempRef) _weightsRules = LoadWeights(Settings.WeightRuleFile);
                Settings.IgnoreRuleFile = ImGuiExtension.ComboBox("Ignore Rules", Settings.IgnoreRuleFile, PickitFiles, out tempRef);
                if (tempRef) _ignoreRules = LoadPickit(Settings.IgnoreRuleFile);
            }

            if (ImGui.CollapsingHeader("Item Logic", ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.TreeNode("Influence Types"))
                {
                    Settings.ShaperItems.Value = ImGuiExtension.Checkbox("Shaper Items", Settings.ShaperItems);
                    Settings.ElderItems.Value = ImGuiExtension.Checkbox("Elder Items", Settings.ElderItems);
                    Settings.HunterItems.Value = ImGuiExtension.Checkbox("Hunter Items", Settings.HunterItems);
                    Settings.CrusaderItems.Value = ImGuiExtension.Checkbox("Crusader Items", Settings.CrusaderItems);
                    Settings.WarlordItems.Value = ImGuiExtension.Checkbox("Warlord Items", Settings.WarlordItems);
                    Settings.RedeemerItems.Value = ImGuiExtension.Checkbox("Redeemer Items", Settings.RedeemerItems);
                    Settings.FracturedItems.Value = ImGuiExtension.Checkbox("Fractured Items", Settings.FracturedItems);
                    Settings.VeiledItems.Value = ImGuiExtension.Checkbox("Veiled Items", Settings.VeiledItems);
                    ImGui.Spacing();
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Links/Sockets/RGB"))
                {
                    Settings.RGB.Value = ImGuiExtension.Checkbox("RGB Items", Settings.RGB);
                    Settings.RGBWidth.Value = ImGuiExtension.IntSlider("Maximum Width##RGBWidth", Settings.RGBWidth);
                    Settings.RGBHeight.Value = ImGuiExtension.IntSlider("Maximum Height##RGBHeight", Settings.RGBHeight);
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Spacing();
                    Settings.TotalSockets.Value = ImGuiExtension.IntSlider("##Sockets", Settings.TotalSockets);
                    ImGui.SameLine();
                    Settings.Sockets.Value = ImGuiExtension.Checkbox("Sockets", Settings.Sockets);
                    Settings.LargestLink.Value = ImGuiExtension.IntSlider("##Links", Settings.LargestLink);
                    ImGui.SameLine();
                    Settings.Links.Value = ImGuiExtension.Checkbox("Links", Settings.Links);
                    ImGui.Separator();
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Overrides"))
                {
                    Settings.IgnoreScrollOfWisdom.Value = ImGuiExtension.Checkbox("Ignore Scroll Of Wisdom", Settings.IgnoreScrollOfWisdom);
                    Settings.AllDivs.Value = ImGuiExtension.Checkbox("All Divination Cards", Settings.AllDivs);
                    Settings.AllCurrency.Value = ImGuiExtension.Checkbox("All Currency", Settings.AllCurrency);
                    Settings.AllUniques.Value = ImGuiExtension.Checkbox("All Uniques", Settings.AllUniques);
                    Settings.QuestItems.Value = ImGuiExtension.Checkbox("Quest Items", Settings.QuestItems);
                    Settings.Maps.Value = ImGuiExtension.Checkbox("##Maps", Settings.Maps);
                    ImGui.SameLine();
                    if (ImGui.TreeNode("Maps"))
                    {
                        Settings.MapTier.Value = ImGuiExtension.IntSlider("Lowest Tier", Settings.MapTier);
                        Settings.UniqueMap.Value = ImGuiExtension.Checkbox("All Unique Maps", Settings.UniqueMap);
                        Settings.MapFragments.Value = ImGuiExtension.Checkbox("Fragments", Settings.MapFragments);
                        ImGui.Spacing();
                        ImGui.TreePop();
                    }

                    Settings.GemQuality.Value = ImGuiExtension.IntSlider("##Gems", "Lowest Quality", Settings.GemQuality);
                    ImGui.SameLine();
                    Settings.Gems.Value = ImGuiExtension.Checkbox("Gems", Settings.Gems);

                    Settings.FlasksQuality.Value = ImGuiExtension.IntSlider("##Flasks", "Lowest Quality", Settings.FlasksQuality);
                    ImGui.SameLine();
                    Settings.Flasks.Value = ImGuiExtension.Checkbox("Flasks", Settings.Flasks);
                    ImGui.Separator();
                    ImGui.TreePop();
                }
                Settings.HeistItems.Value = ImGuiExtension.Checkbox("Heist Items", Settings.HeistItems);

                Settings.Rares.Value = ImGuiExtension.Checkbox("##Rares", Settings.Rares);
                ImGui.SameLine();
                if (ImGui.TreeNode("Rares##asd"))
                {
                    Settings.RareJewels.Value = ImGuiExtension.Checkbox("Jewels", Settings.RareJewels);
                    Settings.RareRingsilvl.Value = ImGuiExtension.IntSlider("##RareRings", "Lowest iLvl", Settings.RareRingsilvl);
                    ImGui.SameLine();
                    Settings.RareRings.Value = ImGuiExtension.Checkbox("Rings", Settings.RareRings);
                    Settings.RareAmuletsilvl.Value = ImGuiExtension.IntSlider("##RareAmulets", "Lowest iLvl", Settings.RareAmuletsilvl);
                    ImGui.SameLine();
                    Settings.RareAmulets.Value = ImGuiExtension.Checkbox("Amulets", Settings.RareAmulets);
                    Settings.RareBeltsilvl.Value = ImGuiExtension.IntSlider("##RareBelts", "Lowest iLvl", Settings.RareBeltsilvl);
                    ImGui.SameLine();
                    Settings.RareBelts.Value = ImGuiExtension.Checkbox("Belts", Settings.RareBelts);
                    Settings.RareGlovesilvl.Value = ImGuiExtension.IntSlider("##RareGloves", "Lowest iLvl", Settings.RareGlovesilvl);
                    ImGui.SameLine();
                    Settings.RareGloves.Value = ImGuiExtension.Checkbox("Gloves", Settings.RareGloves);
                    Settings.RareBootsilvl.Value = ImGuiExtension.IntSlider("##RareBoots", "Lowest iLvl", Settings.RareBootsilvl);
                    ImGui.SameLine();
                    Settings.RareBoots.Value = ImGuiExtension.Checkbox("Boots", Settings.RareBoots);
                    Settings.RareHelmetsilvl.Value = ImGuiExtension.IntSlider("##RareHelmets", "Lowest iLvl", Settings.RareHelmetsilvl);
                    ImGui.SameLine();
                    Settings.RareHelmets.Value = ImGuiExtension.Checkbox("Helmets", Settings.RareHelmets);
                    Settings.RareArmourilvl.Value = ImGuiExtension.IntSlider("##RareArmours", "Lowest iLvl", Settings.RareArmourilvl);
                    ImGui.SameLine();
                    Settings.RareArmour.Value = ImGuiExtension.Checkbox("Armours", Settings.RareArmour);
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Spacing();
                    Settings.RareShieldilvl.Value = ImGuiExtension.IntSlider("##Shields", "Lowest iLvl", Settings.RareShieldilvl);
                    ImGui.SameLine();
                    Settings.RareShield.Value = ImGuiExtension.Checkbox("Shields", Settings.RareShield);
                    Settings.RareShieldWidth.Value = ImGuiExtension.IntSlider("Maximum Width##RareShieldWidth", Settings.RareShieldWidth);
                    Settings.RareShieldHeight.Value = ImGuiExtension.IntSlider("Maximum Height##RareShieldHeight", Settings.RareShieldHeight);
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Spacing();
                    Settings.RareWeaponilvl.Value = ImGuiExtension.IntSlider("##RareWeapons", "Lowest iLvl", Settings.RareWeaponilvl);
                    ImGui.SameLine();
                    Settings.RareWeapon.Value = ImGuiExtension.Checkbox("Weapons", Settings.RareWeapon);
                    Settings.RareWeaponWidth.Value = ImGuiExtension.IntSlider("Maximum Width##RareWeaponWidth", Settings.RareWeaponWidth);
                    Settings.RareWeaponHeight.Value = ImGuiExtension.IntSlider("Maximum Height##RareWeaponHeight", Settings.RareWeaponHeight);
                    if (ImGui.TreeNode("Full Rare Set Manager Integration##FRSMI"))
                    {
                        ImGui.BulletText("You must use github.com/DetectiveSquirrel/FullRareSetManager in order to utilize this section\nThis will determine what items are still needed to be picked up\nfor the chaos recipe, it uses FRSM's count to check this.'");
                        ImGui.Spacing();
                        Settings.FullRareSetManagerOverride.Value = ImGuiExtension.Checkbox("Override Rare Pickup with Full Rare Set Managers' needed pieces", Settings.FullRareSetManagerOverride);

                        Settings.FullRareSetManagerOverrideAllowIdentifiedItems.Value = ImGuiExtension.Checkbox("Pickup Identified items?", Settings.FullRareSetManagerOverrideAllowIdentifiedItems);
                        ImGui.Spacing();
                        ImGui.Spacing();
                        ImGui.BulletText("Set the number you wish to pickup for Full Rare Set Manager overrides\nDefault: -1\n-1 will disable these overrides");
                        ImGui.Spacing();
                        Settings.FullRareSetManagerPickupOverrides.Weapons = ImGuiExtension.IntSlider("Max Weapons(s)##FRSMOverrides1", Settings.FullRareSetManagerPickupOverrides.Weapons, -1, 100);
                        Settings.FullRareSetManagerPickupOverrides.Helmets = ImGuiExtension.IntSlider("Max Helmets##FRSMOverrides2", Settings.FullRareSetManagerPickupOverrides.Helmets, -1, 100);
                        Settings.FullRareSetManagerPickupOverrides.BodyArmors = ImGuiExtension.IntSlider("Max Body Armors##FRSMOverrides3", Settings.FullRareSetManagerPickupOverrides.BodyArmors, -1, 100);
                        Settings.FullRareSetManagerPickupOverrides.Gloves = ImGuiExtension.IntSlider("Max Gloves##FRSMOverrides4", Settings.FullRareSetManagerPickupOverrides.Gloves, -1, 100);
                        Settings.FullRareSetManagerPickupOverrides.Boots = ImGuiExtension.IntSlider("Max Boots##FRSMOverrides5", Settings.FullRareSetManagerPickupOverrides.Boots, -1, 100);
                        Settings.FullRareSetManagerPickupOverrides.Belts = ImGuiExtension.IntSlider("Max Belts##FRSMOverrides6", Settings.FullRareSetManagerPickupOverrides.Belts, -1, 100);
                        Settings.FullRareSetManagerPickupOverrides.Amulets = ImGuiExtension.IntSlider("Max Amulets##FRSMOverrides7", Settings.FullRareSetManagerPickupOverrides.Amulets, -1, 100);
                        Settings.FullRareSetManagerPickupOverrides.Rings = ImGuiExtension.IntSlider("Max Ring Sets##FRSMOverrides8", Settings.FullRareSetManagerPickupOverrides.Rings, -1, 100);
                        ImGui.Spacing();
                        ImGui.Spacing();
                        ImGui.BulletText("Set the ilvl Min/Max you wish to pickup for Full Rare Set Manager overrides\nIt is up to you how to use these two features\nit does not change how FullRareSetManager counts its sets.\nDefault: -1\n-1 will disable these overrides");
                        ImGui.Spacing();
                        Settings.FullRareSetManagerPickupOverrides.MinItemLevel = ImGuiExtension.IntSlider("Minimum Item Level##FRSMOverrides9", Settings.FullRareSetManagerPickupOverrides.MinItemLevel, -1, 100);
                        Settings.FullRareSetManagerPickupOverrides.MaxItemLevel = ImGuiExtension.IntSlider("Max Item Level##FRSMOverrides10", Settings.FullRareSetManagerPickupOverrides.MaxItemLevel, -1, 100);
                        ImGui.TreePop();
                    }
                    ImGui.TreePop();

                }
            }
        }

        public override Job Tick()
        {
            InventoryItems = GameController.Game.IngameState.ServerData.PlayerInventories[0].Inventory;
            inventorySlots = Misc.GetContainer2DArray(InventoryItems);
            if (Input.GetKeyState(Keys.Escape))
            {
                _enabled = false;
                pickItCoroutine.Pause();
            }

            if (_enabled || Input.GetKeyState(Settings.PickUpKey.Value))
            {
                DebugTimer.Restart();

                if (pickItCoroutine.IsDone)
                {
                    var firstOrDefault = Core.ParallelRunner.Coroutines.FirstOrDefault(x => x.OwnerName == nameof(PickIt));

                    if (firstOrDefault != null)
                        pickItCoroutine = firstOrDefault;
                }

                pickItCoroutine.Resume();
                FullWork = false;
            }
            else
            {
                if (FullWork)
                {
                    pickItCoroutine.Pause();
                    DebugTimer.Reset();
                }
            }

            if (DebugTimer.ElapsedMilliseconds > 300)
            {
                FullWork = true;
                //LogMessage("Error pick it stop after time limit 300 ms", 1);
                DebugTimer.Reset();
            }
            //Graphics.DrawText($@"PICKIT :: Debug Tick Timer ({DebugTimer.ElapsedMilliseconds}ms)", new Vector2(100, 100), FontAlign.Left);
            //DebugTimer.Reset();

            return null;
        }

        public override void ReceiveEvent(string eventId, object args)
        {
            if (eventId == "start_pick_it")
            {
                _enabled = true;
            }
            if (eventId == "end_pick_it")
            {
                _enabled = false;
            }
            
            if (!Settings.Enable.Value) return;

            if (eventId == "frsm_display_data")
            {

                var argSerialised = JsonConvert.SerializeObject(args);
                FullRareSetManagerData = JsonConvert.DeserializeObject<FRSetManagerPublishInformation>(argSerialised);
            }
        }

        private IEnumerator FindItemToPick()
        {
            if (!GameController.Window.IsForeground()) yield break;
            var portalLabel = GetLabel(@"Metadata/MiscellaneousObjects/MultiplexPortal");
            var window = GameController.Window.GetWindowRectangleTimeCache;
            var rect = new RectangleF(window.X, window.X, window.X + window.Width, window.Y + window.Height);

            List<CustomItem> currentLabels;
            var morphPath = "Metadata/MiscellaneousObjects/Metamorphosis/MetamorphosisMonsterMarker";
            currentLabels = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabelsVisible
                .Where(x => x.Address != 0 &&
                    x.ItemOnGround?.Path != null &&
                    x.IsVisible && x.Label.GetClientRectCache.Center.PointInRectangle(rect) &&
                    x.CanPickUp && (x.MaxTimeForPickUp.TotalSeconds <= 0) || x.ItemOnGround?.Path == morphPath)
                .Select(x => new CustomItem(x, GameController.Files,
                    x.ItemOnGround.DistancePlayer, _weightsRules, x.ItemOnGround?.Path == morphPath))
                .OrderBy(x => x.Distance).ToList();

            GameController.Debug["PickIt"] = currentLabels;
            var rectangleOfGameWindow = GameController.Window.GetWindowRectangleTimeCache;
            rectangleOfGameWindow.Inflate(-36, -36);
            var pickUpThisItem = currentLabels.FirstOrDefault(x =>
                x.Distance < Settings.PickupRange && x.GroundItem != null &&
                rectangleOfGameWindow.Intersects(new RectangleF(x.LabelOnGround.Label.GetClientRectCache.Center.X,
                    x.LabelOnGround.Label.GetClientRectCache.Center.Y, 3, 3)) && Misc.CanFitInventory(x));
            
            if (_enabled || Input.GetKeyState(Settings.PickUpKey.Value))
            {
                yield return TryToPickV2(pickUpThisItem, portalLabel);
                FullWork = true;
            }
        }
       
        private IEnumerator TryToPickV2(CustomItem pickItItem, LabelOnGround portalLabel)
        {
            if (!pickItItem.IsValid)
            {
                FullWork = true;
                //LogMessage("PickItem is not valid.", 5, Color.Red);
                yield break;
            }

            var centerOfItemLabel = pickItItem.LabelOnGround.Label.GetClientRectCache.Center;
            var rectangleOfGameWindow = GameController.Window.GetWindowRectangleTimeCache;

            _clickWindowOffset = rectangleOfGameWindow.TopLeft;
            rectangleOfGameWindow.Inflate(-36, -36);
            centerOfItemLabel.X += rectangleOfGameWindow.Left;
            centerOfItemLabel.Y += rectangleOfGameWindow.Top;
            if (!rectangleOfGameWindow.Intersects(new RectangleF(centerOfItemLabel.X, centerOfItemLabel.Y, 3, 3)))
            {
                FullWork = true;
                //LogMessage($"Label outside game window. Label: {centerOfItemLabel} Window: {rectangleOfGameWindow}", 5, Color.Red);
                yield break;
            }

            var tryCount = 0;

            while (tryCount < 3)
            {
                var completeItemLabel = pickItItem.LabelOnGround?.Label;

                if (completeItemLabel == null)
                {
                    if (tryCount > 0)
                    {
                        //LogMessage("Probably item already picked.", 3);
                        yield break;
                    }

                    //LogError("Label for item not found.", 5);
                    yield break;
                }

                //while (GameController.Player.GetComponent<Actor>().isMoving)
                //{
                //    yield return waitPlayerMove;
                //}
                Vector2 vector2;
                if (IsPortalNearby(portalLabel, pickItItem.LabelOnGround))
                    vector2 = completeItemLabel.GetClientRect().ClickRandom() + _clickWindowOffset;
                else
                    vector2 = completeItemLabel.GetClientRect().Center + _clickWindowOffset;

                if (!rectangleOfGameWindow.Intersects(new RectangleF(vector2.X, vector2.Y, 3, 3)))
                {
                    FullWork = true;
                    //LogMessage($"x,y outside game window. Label: {centerOfItemLabel} Window: {rectangleOfGameWindow}", 5, Color.Red);
                    yield break;
                }

                Input.SetCursorPos(vector2);
                yield return wait2ms;

                if (pickItItem.IsTargeted())
                {
                    // in case of portal nearby do 3 checks with delays
                    if (IsPortalNearby(portalLabel, pickItItem.LabelOnGround) && !IsPortalTargeted(portalLabel))
                    {
                        yield return new WaitTime(25);
                        if (IsPortalNearby(portalLabel, pickItItem.LabelOnGround) && !IsPortalTargeted(portalLabel))
                        {
                            Input.Click(MouseButtons.Left);
                        }
                    }
                    else if (!IsPortalNearby(portalLabel, pickItItem.LabelOnGround))
                    {
                        Input.Click(MouseButtons.Left);
                    }
                }

                yield return toPick;
                tryCount++;
            }

            tryCount = 0;

            while (GameController.Game.IngameState.IngameUi.ItemsOnGroundLabelsVisible.FirstOrDefault(
                       x => x.Address == pickItItem.LabelOnGround.Address) != null && tryCount < 6)
            {
                tryCount++;
                //yield return waitForNextTry;
            }

            //yield return waitForNextTry;

            //   Mouse.MoveCursorToPosition(oldMousePosition);
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
                GameController.IngameState.UIHoverElement.Address == portalLabel.Label.Address || // this is the right one
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

            var labelQuery =
                from labelOnGround in labels
                let label = labelOnGround?.Label
                where label?.IsValid == true &&
                      label?.Address > 0 &&
                      label?.IsVisible == true
                let itemOnGround = labelOnGround?.ItemOnGround
                where itemOnGround != null &&
                      itemOnGround?.Metadata?.Contains(id) == true
                let dist = GameController?.Player?.GridPos.DistanceSquared(itemOnGround.GridPos)
                orderby dist
                select labelOnGround;

            return labelQuery.FirstOrDefault();
        }
        
        #region (Re)Loading Rules

        private void LoadRuleFiles()
        {
            var PickitConfigFileDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "Compiled", nameof(PickIt),
                PickitRuleDirectory);

            if (!Directory.Exists(PickitConfigFileDirectory))
            {
                Directory.CreateDirectory(PickitConfigFileDirectory);
                return;
            }

            var dirInfo = new DirectoryInfo(PickitConfigFileDirectory);

            PickitFiles = dirInfo.GetFiles("*.txt").Select(x => Path.GetFileNameWithoutExtension(x.Name)).ToList();
            _normalRules = LoadPickit(Settings.NormalRuleFile);
            _magicRules = LoadPickit(Settings.MagicRuleFile);
            _rareRules = LoadPickit(Settings.RareRuleFile);
            _uniqueRules = LoadPickit(Settings.UniqueRuleFile);
            _weightsRules = LoadWeights(Settings.WeightRuleFile);
            _ignoreRules = LoadPickit(Settings.IgnoreRuleFile);
        }

        public HashSet<string> LoadPickit(string fileName)
        {
            var hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (fileName == string.Empty)
            {
                return hashSet;
            }

            var pickitFile = $@"{DirectoryFullName}\{PickitRuleDirectory}\{fileName}.txt";

            if (!File.Exists(pickitFile))
            {
                return hashSet;
            }

            var lines = File.ReadAllLines(pickitFile);

            foreach (var x in lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")))
            {
                hashSet.Add(x.Trim());
            }

            LogMessage($"PICKIT :: (Re)Loaded {fileName}", 5, Color.GreenYellow);
            return hashSet;
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
                    DebugWindow.LogError($"{nameof(PickIt)} => Error when parse weight.");
                }
            }

            LogMessage($"PICKIT :: (Re)Loaded {fileName}", 5, Color.Cyan);
            return result;
        }

        public override void OnPluginDestroyForHotReload()
        {
            pickItCoroutine.Done(true);
        }

        #endregion

        #region Adding / Removing Entities

        public override void EntityAdded(Entity Entity)
        {
        }

        public override void EntityRemoved(Entity Entity)
        {
        }

        #endregion
    }
}
