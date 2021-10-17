using System.Numerics;
using System.Windows.Forms;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace PickIt
{
    public class PickItSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(false);
        public HotkeyNode PickUpKey { get; set; } = Keys.F;
        public RangeNode<int> PickupRange { get; set; } = new RangeNode<int>(600, 1, 1000);
        public RangeNode<int> ExtraDelay { get; set; } = new RangeNode<int>(0, 0, 200);
        public ToggleNode ExpeditionChests { get; set; } = new ToggleNode(true);
        public ToggleNode PickUpEverything { get; set; } = new ToggleNode(false);
        public ToggleNode LeftClickToggleNode { get; set; } = new ToggleNode(true);
        public string FilterFile { get; set; }
        public string WeightRuleFile { get; set; } = string.Empty;
        public ToggleNode PickUpEvenInventoryFull { get; set; } = new ToggleNode(false);
        public ToggleNode UseWeight { get; set; } = new ToggleNode(false);
        public ToggleNode LazyLooting { get; set; } = new ToggleNode(false);
        public ToggleNode NoLazyLootingWhileEnemyClose { get; set; } = new ToggleNode(false);
        public HotkeyNode LazyLootingPauseKey { get; set; } = new HotkeyNode(Keys.Space);

        public ToggleNode ShowInventoryView { get; set; } = new ToggleNode(true);
        public ToggleNode MoveInventoryView { get; set; } = new ToggleNode(false);
        public Vector2 InventorySlotsVector2 { get; set; } = new Vector2(0,0);
    }
}
