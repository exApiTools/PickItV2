using System.Collections.Generic;
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
    public ToggleNode PickUpEverything { get; set; } = new ToggleNode(false);
    public ToggleNode ClickChests { get; set; } = new ToggleNode(true);

    [JsonIgnore]
    public TextNode FilterTest { get; set; } = new TextNode();

    [JsonIgnore]
    public ButtonNode ReloadFilters { get; set; } = new ButtonNode();

    [Menu("Use a Custom \"\\config\\custom_folder\" folder ")]
    public TextNode CustomConfigDir { get; set; } = new TextNode();

    public List<PickitRule> PickitRules { get; set; } = new List<PickitRule>();
}

public record PickitRule(string Name, string Location, bool Enabled)
{
    public bool Enabled = Enabled;
}