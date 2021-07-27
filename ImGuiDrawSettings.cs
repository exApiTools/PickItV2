using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickIt
{
    internal class ImGuiDrawSettings
    {
        internal static void DrawSettings()
        {
            System.Numerics.Vector4 green = new System.Numerics.Vector4(0.102f, 0.388f, 0.106f, 1.000f);
            System.Numerics.Vector4 red = new System.Numerics.Vector4(0.388f, 0.102f, 0.102f, 1.000f);
            ImGuiTreeNodeFlags collapsingHeaderFlags = ImGuiTreeNodeFlags.CollapsingHeader;

            try
            {
                PickIt.Plugin.Settings.PickUpKey.Value = ImGuiExtension.HotkeySelector("Pick up Key", PickIt.Plugin.Settings.PickUpKey.Value);
                PickIt.Plugin.Settings.PickUpRange.Value = ImGuiExtension.IntSlider("Pick up Range", PickIt.Plugin.Settings.PickUpRange);
                PickIt.Plugin.Settings.ExtraDelay.Value = ImGuiExtension.IntSlider("Extra Delay", PickIt.Plugin.Settings.ExtraDelay);
                PickIt.Plugin.Settings.TimeBeforeNewClick.Value = ImGuiExtension.IntSlider("Time Before New Click", PickIt.Plugin.Settings.TimeBeforeNewClick);
                ImGui.Separator();
                PickIt.Plugin.Settings.PickUpEverything.Value = ImGuiExtension.Checkbox("Pick up Everything", PickIt.Plugin.Settings.PickUpEverything.Value);
                ImGui.Separator();

                if (!PickIt.Plugin.Settings.PickUpEverything)
                {
                    PickIt.Plugin.Settings.AllCurrency.Value = ImGuiExtension.Checkbox("All Currency", PickIt.Plugin.Settings.AllCurrency.Value);
                    PickIt.Plugin.Settings.AllDivs.Value = ImGuiExtension.Checkbox("All Divination Cards", PickIt.Plugin.Settings.AllDivs.Value);
                    PickIt.Plugin.Settings.AllUniques.Value = ImGuiExtension.Checkbox("All Uniques", PickIt.Plugin.Settings.AllUniques.Value);
                    PickIt.Plugin.Settings.QuestItems.Value = ImGuiExtension.Checkbox("Quest Items", PickIt.Plugin.Settings.QuestItems.Value);
                    PickIt.Plugin.Settings.PickUpByHardcodedNames.Value = ImGuiExtension.Checkbox("Pick up by hardcoded names", PickIt.Plugin.Settings.PickUpByHardcodedNames.Value);

                    PickIt.Plugin.Settings.Flasks.Value = ImGuiExtension.Checkbox("Pick up flasks", PickIt.Plugin.Settings.Flasks.Value);
                    if (PickIt.Plugin.Settings.Flasks)
                    {
                        PickIt.Plugin.Settings.FlasksQuality.Value = ImGuiExtension.IntSlider("Minimum quality of flasks", PickIt.Plugin.Settings.FlasksQuality);
                    }

                    PickIt.Plugin.Settings.Gems.Value = ImGuiExtension.Checkbox("Pick up gems", PickIt.Plugin.Settings.Gems.Value);
                    if (PickIt.Plugin.Settings.Gems)
                    {
                        PickIt.Plugin.Settings.GemsQuality.Value = ImGuiExtension.IntSlider("Minimum quality of gems", PickIt.Plugin.Settings.GemsQuality);
                    }

                    if (PickIt.Plugin.Settings.Maps)
                        ImGui.PushStyleColor(ImGuiCol.Header, green);
                    else
                        ImGui.PushStyleColor(ImGuiCol.Header, red);

                    if (ImGui.TreeNodeEx("Maps", collapsingHeaderFlags))
                    {
                        PickIt.Plugin.Settings.Maps.Value = ImGuiExtension.Checkbox("Maps", PickIt.Plugin.Settings.Maps.Value);
                        PickIt.Plugin.Settings.MapTier.Value = ImGuiExtension.IntSlider("Lowest Tier", PickIt.Plugin.Settings.MapTier);
                        PickIt.Plugin.Settings.UniqueMap.Value = ImGuiExtension.Checkbox("All Unique Maps", PickIt.Plugin.Settings.UniqueMap.Value);
                        PickIt.Plugin.Settings.MapFragments.Value = ImGuiExtension.Checkbox("Fragments", PickIt.Plugin.Settings.MapFragments.Value);
                        ImGui.Separator();
                    }
                    
                    if (PickIt.Plugin.Settings.PickUpInfluenced)
                        ImGui.PushStyleColor(ImGuiCol.Header, green);
                    else
                        ImGui.PushStyleColor(ImGuiCol.Header, red);

                    if (ImGui.TreeNodeEx("Influence", collapsingHeaderFlags))
                    {
                        PickIt.Plugin.Settings.PickUpInfluenced.Value = ImGuiExtension.Checkbox("Pick up Influenced", PickIt.Plugin.Settings.PickUpInfluenced.Value);
                        if (PickIt.Plugin.Settings.PickUpInfluenced)
                        {
                            PickIt.Plugin.Settings.Veiled.Value = ImGuiExtension.Checkbox("Veiled items", PickIt.Plugin.Settings.Veiled.Value);
                            PickIt.Plugin.Settings.Fractured.Value = ImGuiExtension.Checkbox("Fractured items", PickIt.Plugin.Settings.Fractured.Value);
                            PickIt.Plugin.Settings.Elder.Value = ImGuiExtension.Checkbox("Elder items", PickIt.Plugin.Settings.Elder.Value);
                            PickIt.Plugin.Settings.Shaper.Value = ImGuiExtension.Checkbox("Shaper items", PickIt.Plugin.Settings.Shaper.Value);
                            PickIt.Plugin.Settings.Hunter.Value = ImGuiExtension.Checkbox("Hunter items", PickIt.Plugin.Settings.Hunter.Value);
                            PickIt.Plugin.Settings.Redeemer.Value = ImGuiExtension.Checkbox("Redeemer items", PickIt.Plugin.Settings.Redeemer.Value);
                            PickIt.Plugin.Settings.Crusader.Value = ImGuiExtension.Checkbox("Crusader items", PickIt.Plugin.Settings.Crusader.Value);
                            PickIt.Plugin.Settings.Warlord.Value = ImGuiExtension.Checkbox("Warlord items", PickIt.Plugin.Settings.Warlord.Value);
                            ImGui.Separator();
                        }
                    }

                    if (PickIt.Plugin.Settings.RGB
                        || PickIt.Plugin.Settings.SixSockets
                        || PickIt.Plugin.Settings.SixLinks)
                        ImGui.PushStyleColor(ImGuiCol.Header, green);
                    else
                        ImGui.PushStyleColor(ImGuiCol.Header, red);

                    if (ImGui.TreeNodeEx("Sockets", collapsingHeaderFlags))
                    {
                        PickIt.Plugin.Settings.RGB.Value = ImGuiExtension.Checkbox("Pick up RGB", PickIt.Plugin.Settings.RGB.Value);
                        if (PickIt.Plugin.Settings.RGB)
                        {
                            PickIt.Plugin.Settings.RGBWidth.Value = ImGuiExtension.IntSlider("Maximum Width##RGBWidth", PickIt.Plugin.Settings.RGBWidth);
                            PickIt.Plugin.Settings.RGBHeight.Value = ImGuiExtension.IntSlider("Maximum Height##RGBHeight", PickIt.Plugin.Settings.RGBHeight);
                        }
                        PickIt.Plugin.Settings.SixSockets.Value = ImGuiExtension.Checkbox("Pick up Six-Socketed items", PickIt.Plugin.Settings.SixSockets.Value);
                        PickIt.Plugin.Settings.SixLinks.Value = ImGuiExtension.Checkbox("Pick up Six-Linked items", PickIt.Plugin.Settings.SixLinks.Value);
                        ImGui.Separator();
                    }

                    if (PickIt.Plugin.Settings.FullRareSetManager)
                        ImGui.PushStyleColor(ImGuiCol.Header, green);
                    else
                        ImGui.PushStyleColor(ImGuiCol.Header, red);

                    if (ImGui.TreeNodeEx("Full Rare Set Manager Integration##FRSMI", collapsingHeaderFlags))
                    {

                        ImGui.BulletText("You must use github.com/DetectiveSquirrel/FullRareSetManager in order to utilize this section." +
                            "\nThis will determine what items are still needed to be picked up for the chaos recipe, it uses FRSM's count to check this.");
                        PickIt.Plugin.Settings.FullRareSetManager.Value = ImGuiExtension.Checkbox("Enable FRSM integration", PickIt.Plugin.Settings.FullRareSetManager.Value);
                        ImGui.Spacing();
                        ImGui.Spacing();
                        ImGui.BulletText("Set the number you wish to pickup for Full Rare Set Manager overrides\nDefault: -1\n-1 will disable these overrides");
                        ImGui.Spacing();
                        
                        PickIt.Plugin.Settings.RareWeapon.Value = ImGuiExtension.Checkbox("Weapons", PickIt.Plugin.Settings.RareWeapon.Value);
                        if (PickIt.Plugin.Settings.RareWeapon)
                        {
                            PickIt.Plugin.Settings.FullRareSetManagerPickupOverrides.Weapons = ImGuiExtension.IntSlider("Maximum Weapons##FRSMOverrides1", PickIt.Plugin.Settings.FullRareSetManagerPickupOverrides.Weapons, -1, 100);
                            PickIt.Plugin.Settings.RareWeaponWidth.Value = ImGuiExtension.IntSlider("Maximum Width##RareWeaponWidth", PickIt.Plugin.Settings.RareWeaponWidth);
                            PickIt.Plugin.Settings.RareWeaponHeight.Value = ImGuiExtension.IntSlider("Maximum Height##RareWeaponWidth", PickIt.Plugin.Settings.RareWeaponHeight);
                        }

                        PickIt.Plugin.Settings.RareHelmets.Value = ImGuiExtension.Checkbox("Helmets", PickIt.Plugin.Settings.RareHelmets.Value);
                        if (PickIt.Plugin.Settings.RareHelmets)
                            PickIt.Plugin.Settings.FullRareSetManagerPickupOverrides.Helmets = ImGuiExtension.IntSlider("Maximum Helmets##FRSMOverrides2", PickIt.Plugin.Settings.FullRareSetManagerPickupOverrides.Helmets, -1, 100);
                        
                        PickIt.Plugin.Settings.RareArmour.Value = ImGuiExtension.Checkbox("Armors", PickIt.Plugin.Settings.RareArmour.Value);
                        if (PickIt.Plugin.Settings.RareArmour)
                            PickIt.Plugin.Settings.FullRareSetManagerPickupOverrides.BodyArmors = ImGuiExtension.IntSlider("Maximum Body Armors##FRSMOverrides3", PickIt.Plugin.Settings.FullRareSetManagerPickupOverrides.BodyArmors, -1, 100);
                        
                        PickIt.Plugin.Settings.RareGloves.Value = ImGuiExtension.Checkbox("Gloves", PickIt.Plugin.Settings.RareGloves.Value);
                        if (PickIt.Plugin.Settings.RareGloves)
                            PickIt.Plugin.Settings.FullRareSetManagerPickupOverrides.Gloves = ImGuiExtension.IntSlider("Maximum Gloves##FRSMOverrides4", PickIt.Plugin.Settings.FullRareSetManagerPickupOverrides.Gloves, -1, 100);
                        
                        PickIt.Plugin.Settings.RareBoots.Value = ImGuiExtension.Checkbox("Boots", PickIt.Plugin.Settings.RareBoots.Value);
                        if (PickIt.Plugin.Settings.RareBoots)
                            PickIt.Plugin.Settings.FullRareSetManagerPickupOverrides.Boots = ImGuiExtension.IntSlider("Maximum Boots##FRSMOverrides5", PickIt.Plugin.Settings.FullRareSetManagerPickupOverrides.Boots, -1, 100);
                        
                        PickIt.Plugin.Settings.RareBelts.Value = ImGuiExtension.Checkbox("Belts", PickIt.Plugin.Settings.RareBelts.Value);
                        if (PickIt.Plugin.Settings.RareBelts)
                            PickIt.Plugin.Settings.FullRareSetManagerPickupOverrides.Belts = ImGuiExtension.IntSlider("Maximum Belts##FRSMOverrides6", PickIt.Plugin.Settings.FullRareSetManagerPickupOverrides.Belts, -1, 100);
                        
                        PickIt.Plugin.Settings.RareAmulets.Value = ImGuiExtension.Checkbox("Amulets", PickIt.Plugin.Settings.RareAmulets.Value);
                        if (PickIt.Plugin.Settings.RareAmulets)
                            PickIt.Plugin.Settings.FullRareSetManagerPickupOverrides.Amulets = ImGuiExtension.IntSlider("Maximum Amulets##FRSMOverrides7", PickIt.Plugin.Settings.FullRareSetManagerPickupOverrides.Amulets, -1, 100);
                        
                        PickIt.Plugin.Settings.RareRings.Value = ImGuiExtension.Checkbox("Rings", PickIt.Plugin.Settings.RareRings.Value);
                        if (PickIt.Plugin.Settings.RareRings)
                            PickIt.Plugin.Settings.FullRareSetManagerPickupOverrides.Rings = ImGuiExtension.IntSlider("Maximum Ring Sets##FRSMOverrides8", PickIt.Plugin.Settings.FullRareSetManagerPickupOverrides.Rings, -1, 100);
                        ImGui.Separator();
                    }
                }
            }
            catch (Exception e) { PickIt.Plugin.LogError(e.ToString()); }
        }
    }
}
