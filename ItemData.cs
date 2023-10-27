using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using Map = ExileCore.PoEMemory.Components.Map;

namespace PickIt
{
    public class ItemData
    {
        public record SkillGemInfo(int Level, int MaxLevel, SkillGemQualityTypeE QualityType);
        public record StackData(int Count, int MaxCount);

        private const string ClassNameHeistContract = "HeistContract";
        private const string ModNameVeil = "Veil";
        private const string ModNameInfectedMap = "InfectedMap";
        private const string ModNameAfflictionMapReward = "AfflictionMapReward";
        private const string ModNameMapElderContainsBoss = "MapElderContainsBoss";

        public string Path { get; }
        public string ClassName { get; }
        public string BaseName { get; }
        public string Name { get; }
        public string Description { get; }
        public string ProphecyName { get; }
        public string ProphecyDescription { get; }
        public string HeistContractJobType { get; }
        public int ItemQuality { get; }
        public int Veiled { get; }
        public int Fractured { get; }
        public int ItemLevel { get; }
        public int MapTier { get; }
        public int NumberOfSockets { get; }
        public int LargestLinkSize { get; }
        public int DeliriumStacks { get; }
        public int HeistContractReqJobLevel { get; }
        public int ScourgeTier { get; }
        public bool BIdentified { get; }
        public bool isCorrupted { get; }
        public bool isElder { get; }
        public bool isShaper { get; }
        public bool isCrusader { get; }
        public bool isRedeemer { get; }
        public bool isHunter { get; }
        public bool isWarlord { get; }
        public bool isInfluenced { get; }
        public bool Synthesised { get; }
        public bool isBlightMap { get; }
        public bool isMap { get; }
        public bool isElderGuardianMap { get; }
        public bool Enchanted { get; }
        public ItemRarity Rarity { get; }
        public int SkillGemLevel { get; }
        public int SkillGemQualityType { get; }
        public List<string> ModsNames { get; }
        public List<ItemMod> ItemMods { get; } = new List<ItemMod>();
        public List<int[]> SocketLinks { get; }
        public LabelOnGround LabelOnGround { get; }
        public SkillGemInfo GemInfo { get; }
        public uint InventoryID { get; }
        public int Height { get; }
        public int Width { get; }
        public float Distance => LabelOnGround.ItemOnGround?.DistancePlayer ?? float.PositiveInfinity;
        public StackData StackInfo { get; }

        public Entity GroundItem { get; }

        public int AttemptedPickups = 0;
        public bool IsValid;

        public ItemData(LabelOnGround queriedItem, FilesContainer fs)
        {
            LabelOnGround = queriedItem;
            var itemItemOnGround = queriedItem.ItemOnGround;
            var worldItem = itemItemOnGround?.GetComponent<WorldItem>();
            var groundItem = worldItem?.ItemEntity;
            if (groundItem == null) return;
            GroundItem = groundItem;

            // Component Declarations
            var item = groundItem;
            item.TryGetComponent<Map>(out var mapComp);
            item.TryGetComponent<Base>(out var baseComp);
            item.TryGetComponent<Mods>(out var modsComp);
            item.TryGetComponent<Sockets>(out var socketsComp);
            item.TryGetComponent<Quality>(out var qualityComp);
            item.TryGetComponent<SkillGem>(out var skillGemComp);
            item.TryGetComponent<HeistContract>(out var heistComp);
            item.TryGetComponent<Stack>(out var stackComp);


            // Processing Components
            Path = item.Path;
            var baseItemType = fs.BaseItemTypes.Translate(Path);

            // Processing base comp
            InventoryID = item.InventoryId;
            ScourgeTier = baseComp?.ScourgedTier ?? 0;
            isElder = baseComp?.isElder ?? false;
            isShaper = baseComp?.isShaper ?? false;
            isHunter = baseComp?.isHunter ?? false;
            isWarlord = baseComp?.isWarlord ?? false;
            isCrusader = baseComp?.isCrusader ?? false;
            isRedeemer = baseComp?.isRedeemer ?? false;
            isCorrupted = baseComp?.isCorrupted ?? false;
            isInfluenced = isCrusader || isRedeemer || isWarlord || isHunter || isShaper || isElder;

            if (baseItemType.ClassName == ClassNameHeistContract)
            {
                Width = baseItemType.Width;
                Height = baseItemType.Height;
                HeistContractJobType = heistComp?.RequiredJob?.Name ?? "";
                HeistContractReqJobLevel = heistComp?.RequiredJobLevel ?? 0;
            }

            // Processing Mods
            ItemMods = modsComp?.ItemMods;
            Name = modsComp?.UniqueName ?? Name;
            ItemLevel = modsComp?.ItemLevel ?? 0;
            Fractured = modsComp?.CountFractured ?? 0;
            BIdentified = modsComp?.Identified ?? true;
            Synthesised = modsComp?.Synthesised ?? false;
            Enchanted = modsComp?.EnchantedMods?.Count > 0;
            Rarity = modsComp?.ItemRarity ?? ItemRarity.Normal;
            ModsNames = modsComp?.ItemMods?.Select(mod => mod.Name).ToList();
            Veiled = modsComp?.ItemMods?.Count(m => m.DisplayName.Contains(ModNameVeil)) ?? 0;
            isBlightMap = modsComp?.ItemMods?.Count(m => m.Name.Contains(ModNameInfectedMap)) > 0;
            isMap = mapComp?.Tier != null ? true : false;
            DeliriumStacks = modsComp?.ItemMods?.Count(m => m.Name.Contains(ModNameAfflictionMapReward)) ?? 0;
            isElderGuardianMap = modsComp?.ItemMods?.Count(m => m.Name.Contains(ModNameMapElderContainsBoss)) > 0;

            // Processing Skill Gem
            if (skillGemComp != null)
            {
                GemInfo = new SkillGemInfo(skillGemComp.Level, skillGemComp.MaxLevel, skillGemComp.QualityType);
            }

            if (stackComp != null)
            {
                StackInfo = new StackData(stackComp.Size, stackComp.Info.MaxStackSize);
            }


            // Processing Sockets
            NumberOfSockets = socketsComp?.NumberOfSockets ?? 0;
            LargestLinkSize = socketsComp?.LargestLinkSize ?? 0;
            SocketLinks = socketsComp?.Links ?? new List<int[]>();

            // Processing Quality
            ItemQuality = qualityComp?.ItemQuality ?? 0;

            // Other Assignments
            Description = "";
            Name = baseComp?.Name ?? "";
            BaseName = baseItemType?.BaseName;
            ClassName = baseItemType?.ClassName;

            // Processing Map
            MapTier = mapComp?.Tier ?? 0;

            IsValid = true;
        }

        #region Importing all enums for use otherwise Linq complains, keeping them the same with prefix "I" to keep it simple. Its long but what ever.
        public ActionFlags IActionFlags { get; set; }
        public DamageType IDamageType { get; set; }
        public Direction IDirection { get; set; }
        public EntityType IEntityType { get; set; }
        public FontAlign IFontAlign { get; set; }
        public GameStat IGameStat { get; set; }
        public HeistJobE IHeistJobE { get; set; }
        public Influence IInfluence { get; set; }
        public ItemRarity IItemRarity { get; set; }
        public ItemStatEnum IItemStatEnum { get; set; }
        public LeagueType ILeague { get; set; }
        public MapType IMapType { get; set; }
        public ModDomain IiModDomain { get; set; }
        public ModType IModType { get; set; }
        public PartyStatus IPartyStatus { get; set; }
        public SkillGemQualityTypeE ISkillGemQualityTypeE { get; set; }
        public SocketColor ISocketColor { get; set; }
        public StatType IStatType { get; set; }

        #endregion
    }
}