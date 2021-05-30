using System.Windows.Forms;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace PickIt
{
    public class PickItSettings : ISettings
    {
        public HotkeyNode PickUpKey { get; set; } = Keys.F;
        public RangeNode<int> PickupRange { get; set; } = new RangeNode<int>(600, 1, 1000);
        public RangeNode<int> ExtraDelay { get; set; } = new RangeNode<int>(0, 0, 200);
        public RangeNode<int> TimeBeforeNewClick { get; set; } = new RangeNode<int>(500, 0, 1500);
        public ToggleNode Enable { get; set; } = new ToggleNode(false);
    }
}