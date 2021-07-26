using System.Windows.Forms;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace PickIt
{
    public class PickItSettings : ISettings
    {
        public HotkeyNode PickUpKey { get; set; } = Keys.F;
        public RangeNode<int> PickUpRange { get; set; } = new RangeNode<int>(600, 1, 1000);
        public RangeNode<int> ExtraDelay { get; set; } = new RangeNode<int>(0, 0, 200);
        public RangeNode<int> TimeBeforeNewClick { get; set; } = new RangeNode<int>(500, 0, 1500);
        public ToggleNode PickUpEverything { get; set; } = new ToggleNode(false);
        public ToggleNode PickUpInfluenced { get; set; } = new ToggleNode(true);
        public ToggleNode Veiled { get; set; } = new ToggleNode(true);
        public ToggleNode Fractured { get; set; } = new ToggleNode(true);
        public ToggleNode Elder { get; set; } = new ToggleNode(true);
        public ToggleNode Shaper { get; set; } = new ToggleNode(true);
        public ToggleNode Hunter { get; set; } = new ToggleNode(true);
        public ToggleNode Redeemer { get; set; } = new ToggleNode(true);
        public ToggleNode Crusader { get; set; } = new ToggleNode(true);
        public ToggleNode Warlord { get; set; } = new ToggleNode(true);
        public ToggleNode RGB { get; set; } = new ToggleNode(true);
        public ToggleNode SixSockets { get; set; } = new ToggleNode(true);
        public ToggleNode SixLinks { get; set; } = new ToggleNode(true);
        public ToggleNode Flasks { get; set; } = new ToggleNode(true);
        public RangeNode<int> FlasksQuality { get; set; } = new RangeNode<int>(0, 1, 20);
        public ToggleNode Gems { get; set; }
        public RangeNode<int> GemsQuality { get; set; } = new RangeNode<int>(0, 1, 20);
        public ToggleNode Enable { get; set; } = new ToggleNode(false);
    }
}