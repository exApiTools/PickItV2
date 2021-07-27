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
        public RangeNode<int> RGBWidth { get; set; } = new RangeNode<int>(2, 1, 2);
        public RangeNode<int> RGBHeight { get; set; } = new RangeNode<int>(4, 1, 4);
        public ToggleNode SixSockets { get; set; } = new ToggleNode(true);
        public ToggleNode SixLinks { get; set; } = new ToggleNode(true);
        public ToggleNode Flasks { get; set; } = new ToggleNode(true);
        public RangeNode<int> FlasksQuality { get; set; } = new RangeNode<int>(0, 1, 20);
        public ToggleNode Gems { get; set; } = new ToggleNode(true);
        public RangeNode<int> GemsQuality { get; set; } = new RangeNode<int>(0, 1, 20);
        public ToggleNode PickUpByHardcodedNames { get; set; } = new ToggleNode(true);
        public ToggleNode AllCurrency { get; set; } = new ToggleNode(true);
        public ToggleNode AllDivs { get; set; } = new ToggleNode(true);
        public ToggleNode AllUniques { get; set; } = new ToggleNode(true);
        public ToggleNode QuestItems { get; set; } = new ToggleNode(true);
        public ToggleNode Maps { get; set; } = new ToggleNode(true);
        public RangeNode<int> MapTier { get; set; } = new RangeNode<int>(1, 1, 16);
        public ToggleNode UniqueMap { get; set; } = new ToggleNode(true);
        public ToggleNode MapFragments { get; set; } = new ToggleNode(true);
        public ToggleNode FullRareSetManager { get; set; } = new ToggleNode(false);
        public FRSMOverrides FullRareSetManagerPickupOverrides { get; set; } = new FRSMOverrides();
        public class FRSMOverrides
        {
            public int Weapons { get; set; } = -1;
            public int Helmets { get; set; } = -1;
            public int BodyArmors { get; set; } = -1;
            public int Gloves { get; set; } = -1;
            public int Boots { get; set; } = -1;
            public int Belts { get; set; } = -1;
            public int Amulets { get; set; } = -1;
            public int Rings { get; set; } = -1;
        }

        public ToggleNode RareRings { get; set; } = new ToggleNode(true);
        public ToggleNode RareAmulets { get; set; } = new ToggleNode(true);
        public ToggleNode RareBelts { get; set; } = new ToggleNode(true);
        public ToggleNode RareGloves { get; set; } = new ToggleNode(true);
        public ToggleNode RareBoots { get; set; } = new ToggleNode(true);
        public ToggleNode RareHelmets { get; set; } = new ToggleNode(true);
        public ToggleNode RareArmour { get; set; } = new ToggleNode(true);
        public ToggleNode RareWeapon { get; set; } = new ToggleNode(true);
        public RangeNode<int> RareWeaponWidth { get; set; } = new RangeNode<int>(2, 1, 2);
        public RangeNode<int> RareWeaponHeight { get; set; } = new RangeNode<int>(4, 1, 4);
        public ToggleNode Enable { get; set; } = new ToggleNode(false);
    }
}