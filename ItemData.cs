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

    public ItemData(LabelOnGround queriedItem, FilesContainer fs)
    {
        LabelOnGround = queriedItem;
        var itemItemOnGround = queriedItem.ItemOnGround;
        var worldItem = itemItemOnGround?.GetComponent<WorldItem>();
        var groundItem = worldItem?.ItemEntity;
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

    public List<ItemMod> FindMods(string wantedMod) => ItemMods.Where(item => item.Name.ToLower().Contains(wantedMod.ToLower()))
                    .Select(item => item)
                    .ToList();

    public List<int> MatchModsSum(string[] wantedMods)
    {
        List<List<int>> foundValues = new List<List<int>>();

        foreach (var wantedMod in wantedMods)
        {
            var foundMod = ItemMods.FirstOrDefault(item => item.Name.ToLower() == wantedMod.ToLower());
            if (foundMod == null)
            {
                return null;
            }

            foundValues.Add(foundMod.Values);
        }

        if (foundValues.Count == wantedMods.Length)
        {
            return SumIntListValues(foundValues);
        }

        return null;
    }

    public List<int> MatchModsWeightedSum(Dictionary<string, int> wantedMods)
    {
        List<List<int>> foundValues = new List<List<int>>();

        foreach (var wantedMod in wantedMods)
        {
            var foundMod = ItemMods?.FirstOrDefault(item => item.Name.ToLower() == wantedMod.Key);
            if (foundMod == null)
            {
                return null;
            }

            var valueList = new List<int>();
            foreach (var value in foundMod.Values)
            {
                valueList.Add(value * wantedMod.Value);
            }
            foundValues.Add(valueList);
        }

        if (foundValues.Count == wantedMods.Count)
        {
            return SumIntListValues(foundValues);
        }

        return null;
    }

    public List<int> AnyModsSum(string[] wantedMods)
    {
        List<List<int>> foundValues = new List<List<int>>();

        foreach (var wantedMod in wantedMods)
        {
            var foundMod = ItemMods?.FirstOrDefault(item => item.Name.ToLower() == wantedMod.ToLower());
            if (foundMod != null)
            {
                foundValues.Add(foundMod.Values);
            }
        }

        return SumIntListValues(foundValues);
    }

    // currently doesnt work due to Type Dictionary not found
    public List<int> AnyModsWeightedSum(Dictionary<string, int> wantedMods)
    {
        List<List<int>> foundValues = new List<List<int>>();

        foreach (var wantedMod in wantedMods)
        {
            var foundMod = ItemMods?.FirstOrDefault(item => item.Name.ToLower() == wantedMod.Key);
            if (foundMod != null)
            {
                var valueList = new List<int>();
                foreach (var value in foundMod.Values) 
                {
                    valueList.Add(value * wantedMod.Value);
                }
                foundValues.Add(valueList);
            }
        }

        return SumIntListValues(foundValues);
    }

    public bool HasMods(string[] wantedMods)
    {
        List<List<int>> foundValues = new List<List<int>>();

        foreach (var wantedMod in wantedMods)
        {
            var foundMod = ItemMods?.FirstOrDefault(item => item.Name.ToLower() == wantedMod.ToLower());
            if (foundMod != null)
            {
                foundValues.Add(foundMod.Values);
            }
        }

        if (foundValues.Count == wantedMods.Length)
        {
            return true;
        }

        return false;
    }

    public List<int> SumIntListValues(List<List<int>> lists)
    {
        int maxLength = lists.SelectMany(list => list).Count();
        List<int> combinedList = new List<int>();

        for (int i = 0; i < maxLength; i++)
        {
            int sum = 0;
            foreach (var list in lists)
            {
                if (i < list.Count)
                {
                    sum += list[i];
                }
            }
            combinedList.Add(sum);
        }

        return combinedList;
    }

    public override string ToString()
    {
        return $"{BaseName} ({ClassName}) Dist: {Distance}";
    }
}