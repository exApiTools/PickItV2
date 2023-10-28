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
    public record SkillGemInfo(int Level, int MaxLevel, SkillGemQualityTypeE QualityType);

    public record StackData(int Count, int MaxCount);

    public record SocketData(int LargestLinkSize, int SocketNumber, IReadOnlyCollection<IReadOnlyCollection<int>> Links, IReadOnlyCollection<string> SocketGroups);

    private const string ModNameVeil = "Veil";
    private const string ModNameInfectedMap = "InfectedMap";
    private const string ModNameAfflictionMapReward = "AfflictionMapReward";
    private const string ModNameMapElderContainsBoss = "MapElderContainsBoss";

    public string Path { get; }
    public string ClassName { get; }
    public string BaseName { get; }
    public string Name { get; }
    public string HeistContractJobType { get; }
    public int ItemQuality { get; } = 0;
    public int VeiledModCount { get; }
    public int FracturedModCount { get; }
    public int ItemLevel { get; }
    public int? MapTier { get; }
    public int DeliriumStacks { get; }
    public int HeistContractReqJobLevel { get; }
    public int ScourgeTier { get; }
    public bool IsIdentified { get; }
    public Influence InfluenceFlags { get; set; }
    public bool IsCorrupted { get; }
    public bool IsElder { get; }
    public bool IsShaper { get; }
    public bool IsCrusader { get; }
    public bool IsRedeemer { get; }
    public bool IsHunter { get; }
    public bool IsWarlord { get; }
    public bool IsInfluenced { get; }
    public bool IsSynthesised { get; }
    public bool IsBlightMap { get; }
    public bool IsMap { get; }
    public bool IsElderGuardianMap { get; }
    public bool Enchanted { get; }
    public ItemRarity Rarity { get; }
    public List<string> ModsNames { get; }
    public List<ItemMod> ItemMods { get; }
    public LabelOnGround LabelOnGround { get; }
    public SkillGemInfo GemInfo { get; }
    public uint InventoryId { get; }
    public int Height { get; }
    public int Width { get; }
    public bool IsWeapon { get; }
    public float Distance => LabelOnGround.ItemOnGround?.DistancePlayer ?? float.PositiveInfinity;
    public StackData StackInfo { get; }
    public Entity GroundItem { get; }
    public SocketData SocketInfo { get; } = new SocketData(0, 0, new List<IReadOnlyCollection<int>>(), new List<string>());

    public int AttemptedPickups = 0;

    public ItemData(LabelOnGround queriedItem, FilesContainer fs) :
        this(queriedItem, queriedItem.ItemOnGround?.GetComponent<WorldItem>()?.ItemEntity, fs)
    {
    }

    public ItemData(LabelOnGround queriedItem, Entity groundItem, FilesContainer fs)
    {
        LabelOnGround = queriedItem;
        if (groundItem == null) return;
        GroundItem = groundItem;
        var item = groundItem;
        Path = item.Path;
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

        if (GroundItem.TryGetComponent<Base>(out var baseComp))
        {
            Name = baseComp.Name ?? "";
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
            ItemMods = modsComp.ItemMods;
            Name = string.IsNullOrWhiteSpace(modsComp.UniqueName) ? Name : modsComp.UniqueName;
            FracturedModCount = modsComp.CountFractured;
            IsSynthesised = modsComp.Synthesised;
            Enchanted = modsComp.EnchantedMods?.Count > 0;
            ModsNames = modsComp.ItemMods.Select(mod => mod.Name).ToList();
            VeiledModCount = modsComp.ItemMods.Count(m => m.DisplayName.Contains(ModNameVeil));
            IsBlightMap = modsComp.ItemMods.Any(m => m.Name.Contains(ModNameInfectedMap));
            DeliriumStacks = modsComp.ItemMods.Count(m => m.Name.Contains(ModNameAfflictionMapReward));
            IsElderGuardianMap = modsComp.ItemMods.Any(m => m.Name.Contains(ModNameMapElderContainsBoss));
        }

        if (item.TryGetComponent<Sockets>(out var sockets))
        {
            // issue to be resolved in core, if a corrupted ring with sockets gets a new implicit it will still have the component but the component logic will throw an exception
            if (sockets.NumberOfSockets > 0)
                SocketInfo = new SocketData(sockets.LargestLinkSize, sockets.NumberOfSockets, sockets.Links, sockets.SocketGroup);
        }

        if (item.TryGetComponent<SkillGem>(out var gem))
        {
            GemInfo = new SkillGemInfo(gem.Level, gem.MaxLevel, gem.QualityType);
        }

        if (item.TryGetComponent<Stack>(out var stack))
        {
            StackInfo = new StackData(stack.Size, stack.Info.MaxStackSize);
        }

        if (GroundItem.HasComponent<Weapon>())
        {
            IsWeapon = true;
        }

        if (item.TryGetComponent<ExileCore.PoEMemory.Components.Map>(out var map))
        {
            MapTier = map.Tier;
            IsMap = true;
        }

        if (item.TryGetComponent<HeistContract>(out var heistComp))
        {
            HeistContractJobType = heistComp.RequiredJob?.Name ?? "";
            HeistContractReqJobLevel = heistComp.RequiredJobLevel;
        }

        InventoryId = item.InventoryId;

        var test = !IsIdentified && Rarity == ItemRarity.Rare && ItemLevel <= 74 && ItemLevel >= 60 && ClassName == "Ring";
    }

    public bool HasUnorderedSocketGroup(string groupText) =>
        SocketInfo.SocketGroups.Any(x =>
            x.ToLookup(char.ToLowerInvariant) is var lookup &&
            groupText.GroupBy(char.ToLowerInvariant).All(g => lookup[g.Key].Count() >= g.Count()));

    public List<ItemMod> FindMods(string wantedMod) => ItemMods
        .Where(item => item.Name.Contains(wantedMod, StringComparison.OrdinalIgnoreCase)).ToList();

    public IReadOnlyDictionary<GameStat, int> ModStats(params string[] wantedMods)
    {
        if (ItemMods == null)
        {
            return new DefaultDictionary<GameStat, int>(0);
        }

        return SumModStats(ItemMods.IntersectBy(wantedMods, x => x.Name, StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyDictionary<GameStat, int> ItemStats
    {
        get
        {
            if (ItemMods == null)
            {
                return new DefaultDictionary<GameStat, int>(0);
            }

            return SumModStats(ItemMods);
        }
    }

    public IReadOnlyDictionary<GameStat, float> ModWeightedStatSum(params (string, float)[] wantedMods)
    {
        if (ItemMods == null)
        {
            return new DefaultDictionary<GameStat, float>(0);
        }

        return SumModStats(ItemMods.Join(wantedMods, x => x.Name, x => x.Item1, (mod, w) => (mod, w.Item2), StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyDictionary<GameStat, float> ModWeightedStatSum(Dictionary<string, float> wantedMods)
    {
        if (ItemMods == null)
        {
            return new DefaultDictionary<GameStat, float>(0);
        }

        return SumModStats(ItemMods.Join(wantedMods, x => x.Name, x => x.Key, (mod, w) => (mod, w.Value), StringComparer.OrdinalIgnoreCase));
    }

    public bool HasMods(params string[] wantedMods)
    {
        return ItemMods != null &&
               ItemMods.IntersectBy(wantedMods, x => x.Name, StringComparer.OrdinalIgnoreCase)
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