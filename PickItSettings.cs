using System.Numerics;
using System.Windows.Forms;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using Newtonsoft.Json;

namespace PickIt;

public class PickItSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(false);
    public ToggleNode ShowInventoryView { get; set; } = new ToggleNode(true);
    public ToggleNode MoveInventoryView { get; set; } = new ToggleNode(false);
    public HotkeyNode PickUpKey { get; set; } = Keys.F;
    public ToggleNode PickUpWhenInventoryIsFull { get; set; } = new ToggleNode(false);
    public RangeNode<int> PickupRange { get; set; } = new RangeNode<int>(600, 1, 1000);
    public RangeNode<int> PauseBetweenClicks { get; set; } = new RangeNode<int>(100, 0, 500);
    public ToggleNode LazyLooting { get; set; } = new ToggleNode(false);
    public ToggleNode NoLazyLootingWhileEnemyClose { get; set; } = new ToggleNode(false);
    public HotkeyNode LazyLootingPauseKey { get; set; } = new HotkeyNode(Keys.Space);

    [JsonIgnore]
    public ButtonNode ReloadFilters { get; set; } = new ButtonNode();

    public ListNode FilterFile { get; set; } = new ListNode();
    public ToggleNode PickUpEverything { get; set; } = new ToggleNode(false);
    public ToggleNode ExpeditionChests { get; set; } = new ToggleNode(true);

    [Menu("Ignore \"Can pick up\" flag")]
    public ToggleNode IgnoreCanPickUp { get; set; } = new ToggleNode(false);
    public Vector2 InventorySlotsVector2 { get; set; } = new Vector2(0, 0);
}