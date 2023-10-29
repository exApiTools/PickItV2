using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;

namespace PickIt;

public class ItemData
{
    public record SkillGemData(int Level, int MaxLevel, SkillGemQualityTypeE QualityType);

    public record StackData(int Count, int MaxCount);

    public record ChargesData(int Current, int Max, int PerUse);

    public record SocketData(int LargestLinkSize, int SocketNumber, IReadOnlyCollection<IReadOnlyCollection<int>> Links, IReadOnlyCollection<string> SocketGroups);

    public record FlaskData(int LifeRecovery, int ManaRecovery, Dictionary<GameStat, int> Stats);

    public record AttributeRequirementsData(int Strength, int Dexterity, int Intelligence);

    public record ArmourData(int Armour, int Evasion, int ES);

    public record ModsData(List<ItemMod> ItemMods, List<ItemMod> EnchantedMods, List<ItemMod> ExplicitMods, List<ItemMod> FracturedMods, List<ItemMod> ImplicitMods, List<ItemMod> ScourgeMods, List<ItemMod> SynthesisMods, List<ItemMod> CrucibleMods);

    public string Path { get; } = string.Empty;
    public string ClassName { get; } = string.Empty;
    public string BaseName { get; } = string.Empty;
    public string Name { get; } = string.Empty;
    public string PublicPrice { get; } = string.Empty;
    public string HeistContractJobType { get; } = string.Empty;
    public int ItemQuality { get; } = 0;
    public int VeiledModCount { get; } = 0;
    public int FracturedModCount { get; } = 0;
    public int ItemLevel { get; } = 0;
    public int MapTier { get; } = 0;
    public int DeliriumStacks { get; } = 0;
    public int HeistContractReqJobLevel { get; } = 0;
    public int ScourgeTier { get; } = 0;
    public bool IsIdentified { get; } = false;
    public Influence InfluenceFlags { get; set; }
    public bool IsCorrupted { get; } = false;
    public bool IsElder { get; } = false;
    public bool IsShaper { get; } = false;
    public bool IsCrusader { get; } = false;
    public bool IsRedeemer { get; } = false;
    public bool IsHunter { get; } = false;
    public bool IsWarlord { get; } = false;
    public bool IsInfluenced { get; } = false;
    public bool IsSynthesised { get; } = false;
    public bool IsBlightMap { get; } = false;
    public bool IsMap { get; } = false;
    public bool IsElderGuardianMap { get; } = false;
    public bool Enchanted { get; } = false;
    public ItemRarity Rarity { get; } = ItemRarity.Normal;
    public List<string> ModsNames { get; } = new List<string>();
    public LabelOnGround LabelOnGround { get; } = null;
    public SkillGemData GemInfo { get; } = new SkillGemData(0, 0, SkillGemQualityTypeE.Superior);
    public uint InventoryId { get; }
    public uint Id { get; }
    public int Height { get; } = 0;
    public int Width { get; } = 0;
    public bool IsWeapon { get; } = false;
    public int ShieldBlockChance { get; } = 0;
    public float Distance => LabelOnGround.ItemOnGround?.DistancePlayer ?? float.PositiveInfinity;
    public StackData StackInfo { get; } = new StackData(0, 0);
    public Entity Entity { get; }
    public SocketData SocketInfo { get; } = new SocketData(0, 0, new List<IReadOnlyCollection<int>>(), new List<string>());
    public ChargesData ChargeInfo { get; } = new ChargesData(0, 0, 0);
    public FlaskData FlaskInfo { get; } = new FlaskData(0, 0, new Dictionary<GameStat, int>());
    public AttributeRequirementsData AttributeRequirements { get; } = new AttributeRequirementsData(0, 0, 0);
    public ArmourData ArmourInfo { get; } = new ArmourData(0, 0, 0);
    public ModsData ModsInfo { get; } = new ModsData(new List<ItemMod>(), new List<ItemMod>(), new List<ItemMod>(), new List<ItemMod>(), new List<ItemMod>(), new List<ItemMod>(), new List<ItemMod>(), new List<ItemMod>());
    public string ResourcePath { get; } = string.Empty;
    public Dictionary<GameStat, int> LocalStats { get; } = new Dictionary<GameStat, int>();

    public int AttemptedPickups = 0;

    public ItemData(LabelOnGround queriedItem, FilesContainer fs) :
        this(queriedItem.ItemOnGround?.GetComponent<WorldItem>()?.ItemEntity, fs, queriedItem)
    {
    }

    public ItemData(Entity itemEntity, FilesContainer fs, LabelOnGround itemLabelOnGround)
    {
        if (itemEntity == null) return;
        var item = itemEntity;

        LabelOnGround = itemLabelOnGround;
        Entity = itemEntity;
        Path = item.Path;
        Id = item.Id;
        InventoryId = item.InventoryId;

        var baseItemType = fs.BaseItemTypes.Translate(Path);
        if (baseItemType != null)
        {
            ClassName = baseItemType.ClassName;
            BaseName = baseItemType.BaseName;
            Width = baseItemType.Width;
            Height = baseItemType.Height;
        }

        if (item.TryGetComponent<Quality>(out var quality))
        {
            ItemQuality = quality.ItemQuality;
        }

        if (item.TryGetComponent<Base>(out var baseComp))
        {
            Name = baseComp.Name ?? "";
            PublicPrice = baseComp.PublicPrice ?? "";
            InfluenceFlags = baseComp.InfluenceFlag;
            ScourgeTier = baseComp.ScourgedTier;
            IsElder = baseComp.isElder;
            IsShaper = baseComp.isShaper;
            IsHunter = baseComp.isHunter;
            IsWarlord = baseComp.isWarlord;
            IsCrusader = baseComp.isCrusader;
            IsRedeemer = baseComp.isRedeemer;
            IsCorrupted = baseComp.isCorrupted;
            IsInfluenced = IsCrusader || IsRedeemer || IsWarlord || IsHunter || IsShaper || IsElder;
        }

        if (item.TryGetComponent<Mods>(out var modsComp))
        {
            Rarity = modsComp.ItemRarity;
            IsIdentified = modsComp.Identified;
            ItemLevel = modsComp.ItemLevel;
            Name = string.IsNullOrWhiteSpace(modsComp.UniqueName) ? Name : modsComp.UniqueName;
            FracturedModCount = modsComp.CountFractured;
            IsSynthesised = modsComp.Synthesised;
            ModsInfo = new ModsData(modsComp.ItemMods, modsComp.EnchantedMods, modsComp.ExplicitMods, modsComp.FracturedMods, modsComp.ImplicitMods, modsComp.ScourgeMods, modsComp.SynthesisMods, modsComp.CrucibleMods);
            Enchanted = ModsInfo.EnchantedMods?.Count > 0;
            ModsNames = ModsInfo.ItemMods.Select(mod => mod.Name).ToList();
            VeiledModCount = ModsInfo.ItemMods.Count(m => m.DisplayName.Contains("Veil"));
            IsBlightMap = ModsInfo.ItemMods.Any(m => m.Name.Contains("InfectedMap"));
            DeliriumStacks = ModsInfo.ItemMods.Count(m => m.Name.Contains("AfflictionMapReward"));
            IsElderGuardianMap = ModsInfo.ItemMods.Any(m => m.Name.Contains("MapElderContainsBoss"));
        }

        if (item.TryGetComponent<Sockets>(out var socketComp))
        {
            // issue to be resolved in core, if a corrupted ring with sockets gets a new implicit it will still have the component but the component logic will throw an exception
            if (socketComp.NumberOfSockets > 0)
                SocketInfo = new SocketData(socketComp.LargestLinkSize, socketComp.NumberOfSockets, socketComp.Links, socketComp.SocketGroup);
        }

        if (item.TryGetComponent<SkillGem>(out var gemComp))
        {
            GemInfo = new SkillGemData(gemComp.Level, gemComp.MaxLevel, gemComp.QualityType);
        }

        if (item.TryGetComponent<Stack>(out var stackComp))
        {
            StackInfo = new StackData(stackComp.Size, stackComp.Info.MaxStackSize);
        }

        if (item.HasComponent<Weapon>())
        {
            IsWeapon = true;
        }

        if (item.TryGetComponent<ExileCore.PoEMemory.Components.Map>(out var mapComp))
        {
            MapTier = mapComp.Tier;
            IsMap = true;
        }

        if (item.TryGetComponent<HeistContract>(out var heistComp))
        {
            HeistContractJobType = heistComp.RequiredJob?.Name ?? "";
            HeistContractReqJobLevel = heistComp.RequiredJobLevel;
        }

        if (item.TryGetComponent<Charges>(out var chargesComp))
        {
            ChargeInfo = new ChargesData(chargesComp.NumCharges, chargesComp.ChargesMax, chargesComp.ChargesPerUse);
        }

        if (item.TryGetComponent<RenderItem>(out var renderItemComp))
        {
            ResourcePath = renderItemComp.ResourcePath;
        }

        if (item.TryGetComponent<Flask>(out var flaskComp))
        {
            FlaskInfo = new FlaskData(flaskComp.LifeRecover, flaskComp.ManaRecover, flaskComp.FlaskStatDictionary);
        }

        if (item.TryGetComponent<LocalStats>(out var localStatsComp))
        {
            LocalStats = localStatsComp.StatDictionary;
        }

        if (item.TryGetComponent<AttributeRequirements>(out var attributeReqComp))
        {
            AttributeRequirements = new AttributeRequirementsData(attributeReqComp.strength, attributeReqComp.intelligence, attributeReqComp.dexterity);
        }

        if (item.TryGetComponent<Armour>(out var armourComp))
        {
            ArmourInfo = new ArmourData(armourComp.ArmourScore, armourComp.EvasionScore, armourComp.EnergyShieldScore);
        }

        if (item.TryGetComponent<Shield>(out var shieldComp))
        {
            ShieldBlockChance = shieldComp.ChanceToBlock;
        }

        // Reserving as using VS for linq making is easier.
        var test = (ArmourInfo.Evasion + ArmourInfo.ES) >= 900;
    }

    public bool HasUnorderedSocketGroup(string groupText) =>
        SocketInfo.SocketGroups.Any(x =>
            x.ToLookup(char.ToLowerInvariant) is var lookup &&
            groupText.GroupBy(char.ToLowerInvariant).All(g => lookup[g.Key].Count() >= g.Count()));

    public List<ItemMod> FindMods(string wantedMod) => ModsInfo.ItemMods
        .Where(item => item.Name.Contains(wantedMod, StringComparison.OrdinalIgnoreCase)).ToList();

    public IReadOnlyDictionary<GameStat, int> ModStats(params string[] wantedMods)
    {
        if (ModsInfo.ItemMods == null)
        {
            return new DefaultDictionary<GameStat, int>(0);
        }

        return SumModStats(ModsInfo.ItemMods.IntersectBy(wantedMods, x => x.Name, StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyDictionary<GameStat, int> ItemStats
    {
        get
        {
            if (ModsInfo.ItemMods == null)
            {
                return new DefaultDictionary<GameStat, int>(0);
            }

            return SumModStats(ModsInfo.ItemMods);
        }
    }

    public IReadOnlyDictionary<GameStat, float> ModWeightedStatSum(params (string, float)[] wantedMods)
    {
        if (ModsInfo.ItemMods == null)
        {
            return new DefaultDictionary<GameStat, float>(0);
        }

        return SumModStats(ModsInfo.ItemMods.Join(wantedMods, x => x.Name, x => x.Item1, (mod, w) => (mod, w.Item2), StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyDictionary<GameStat, float> ModWeightedStatSum(Dictionary<string, float> wantedMods)
    {
        if (ModsInfo.ItemMods == null)
        {
            return new DefaultDictionary<GameStat, float>(0);
        }

        return SumModStats(ModsInfo.ItemMods.Join(wantedMods, x => x.Name, x => x.Key, (mod, w) => (mod, w.Value), StringComparer.OrdinalIgnoreCase));
    }

    public bool HasMods(params string[] wantedMods)
    {
        return ModsInfo.ItemMods != null &&
               ModsInfo.ItemMods.IntersectBy(wantedMods, x => x.Name, StringComparer.OrdinalIgnoreCase)
                   .Count() == wantedMods.Length;
    }

    public static IReadOnlyDictionary<GameStat, int> SumModStats(IEnumerable<ItemMod> mods)
    {
        return new DefaultDictionary<GameStat, int>(mods
            .SelectMany(x => x.ModRecord.StatNames.Zip(x.Values, (name, value) => (name.MatchingStat, value)))
            .GroupBy(x => x.MatchingStat, x => x.value, (stat, values) => (stat, values.Sum()))
            .ToDictionary(x => x.stat, x => x.Item2), 0);
    }

    public static IReadOnlyDictionary<GameStat, int> SumModStats(params ItemMod[] mods) =>
        SumModStats((IEnumerable<ItemMod>)mods);

    public static IReadOnlyDictionary<GameStat, float> SumModStats(IEnumerable<(ItemMod mod, float weight)> mods)
    {
        return new DefaultDictionary<GameStat, float>(mods
            .SelectMany(x => x.mod.ModRecord.StatNames.Zip(x.mod.Values, (name, value) => (name.MatchingStat, value: value * x.weight)))
            .GroupBy(x => x.MatchingStat, x => x.value, (stat, values) => (stat, values.Sum()))
            .ToDictionary(x => x.stat, x => x.Item2), 0);
    }

    public static IReadOnlyDictionary<GameStat, float> SumModStats(params (ItemMod mod, float weight)[] mods) =>
        SumModStats((IEnumerable<(ItemMod mod, float weight)>)mods);


    public override string ToString()
    {
        return $"{BaseName} ({ClassName}) Dist: {Distance}";
    }
}