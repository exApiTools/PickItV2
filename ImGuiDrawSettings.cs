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

            PickIt.Plugin.Settings.PickUpKey.Value = ImGuiExtension.HotkeySelector("Pick up Key", PickIt.Plugin.Settings.PickUpKey.Value);
            PickIt.Plugin.Settings.PickUpRange.Value = ImGuiExtension.IntSlider("Pick up Range", PickIt.Plugin.Settings.PickUpRange);
            PickIt.Plugin.Settings.ExtraDelay.Value = ImGuiExtension.IntSlider("Extra Delay", PickIt.Plugin.Settings.ExtraDelay);
            PickIt.Plugin.Settings.TimeBeforeNewClick.Value = ImGuiExtension.IntSlider("Time Before New Click", PickIt.Plugin.Settings.TimeBeforeNewClick);
            ImGui.Separator();
            PickIt.Plugin.Settings.PickUpEverything.Value = ImGuiExtension.Checkbox("Pick up Everything", PickIt.Plugin.Settings.PickUpEverything.Value);

            if (!PickIt.Plugin.Settings.PickUpEverything)
            {
                ImGui.Separator();
                try
                {
                    if (PickIt.Plugin.Settings.PickUpInfluenced)
                        ImGui.PushStyleColor(ImGuiCol.Header, green);
                    else
                        ImGui.PushStyleColor(ImGuiCol.Header, red);

                    if (ImGui.TreeNodeEx("Influence", collapsingHeaderFlags))
                    {
                        PickIt.Plugin.Settings.PickUpInfluenced.Value = ImGuiExtension.Checkbox("Pick up Influenced", PickIt.Plugin.Settings.PickUpInfluenced.Value);
                        if (PickIt.Plugin.Settings.PickUpInfluenced)
                        {
                            ImGui.Separator();
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

                    if (ImGui.TreeNodeEx("Sockets", collapsingHeaderFlags))
                    {
                        PickIt.Plugin.Settings.RGB.Value = ImGuiExtension.Checkbox("Pick up RGB", PickIt.Plugin.Settings.RGB.Value);
                        PickIt.Plugin.Settings.SixSockets.Value = ImGuiExtension.Checkbox("Pick up Six-Socketed items", PickIt.Plugin.Settings.SixSockets.Value);
                        PickIt.Plugin.Settings.SixLinks.Value = ImGuiExtension.Checkbox("Pick up Six-Linked items", PickIt.Plugin.Settings.SixLinks.Value);
                        ImGui.Separator();
                    }

                    PickIt.Plugin.Settings.Flasks.Value = ImGuiExtension.Checkbox("Pick up flasks", PickIt.Plugin.Settings.Flasks.Value);
                    if (PickIt.Plugin.Settings.Flasks)
                    {
                        PickIt.Plugin.Settings.FlasksQuality.Value = ImGuiExtension.IntSlider("Minimum quality of flasks", PickIt.Plugin.Settings.FlasksQuality);
                        ImGui.Separator();
                    }

                    PickIt.Plugin.Settings.Gems.Value = ImGuiExtension.Checkbox("Pick up gems", PickIt.Plugin.Settings.Gems.Value);
                    if (PickIt.Plugin.Settings.Gems)
                    {
                        PickIt.Plugin.Settings.GemsQuality.Value = ImGuiExtension.IntSlider("Minimum quality of gems", PickIt.Plugin.Settings.GemsQuality);
                        ImGui.Separator();
                    }


                }
                catch (Exception e) { PickIt.Plugin.LogError(e.ToString()); }









            }
        }
    }
}
