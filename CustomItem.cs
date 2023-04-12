using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Static;
using Map = ExileCore.PoEMemory.Components.Map;

namespace PickIt;

public record SocketInfo(int LargestLinkSize, int SocketNumber, IReadOnlyCollection<IReadOnlyCollection<int>> Links);
public record SkillGemInfo(int Level, int MaxLevel, SkillGemQualityTypeE QualityType);

public record StackInfo(int Count, int MaxCount);

public class CustomItem
{
    public bool IsValid;
    public int AttemptedPickups = 0;
    public CustomItem(LabelOnGround item, FilesContainer fs)
    {
        LabelOnGround = item;
        var itemItemOnGround = item.ItemOnGround;
        var worldItem = itemItemOnGround?.GetComponent<WorldItem>();
        var groundItem = worldItem?.ItemEntity;
        if (groundItem == null) return;
        GroundItem = groundItem;
        Path = groundItem.Path;

        if (Path is { Length: < 1 })
        {
            DebugWindow.LogMsg($"World: {worldItem.Address:X} P: {Path}", 2);
            DebugWindow.LogMsg($"Ground: {GroundItem.Address:X} P {Path}", 2);
            return;
        }

        var baseItemType = fs.BaseItemTypes.Translate(Path);

        if (baseItemType != null)
        {
            ClassName = new ItemClasses().contents.TryGetValue(baseItemType.ClassName, out var @class) 
                ? @class.ClassName 
                : baseItemType.ClassName;
            BaseName = baseItemType.BaseName;
            Width = baseItemType.Width;
            Height = baseItemType.Height;
            if (ClassName.StartsWith("Heist")) IsHeist = true;
        }

        if (GroundItem.HasComponent<Quality>())
        {
            var quality = GroundItem.GetComponent<Quality>();
            Quality = quality.ItemQuality;
        }

        if (GroundItem.HasComponent<Base>())
        {
            var @base = GroundItem.GetComponent<Base>();
            InfluenceFlags = @base.InfluenceFlag;
        }

        if (GroundItem.HasComponent<Mods>())
        {
            var mods = GroundItem.GetComponent<Mods>();
            Rarity = mods.ItemRarity;
            IsIdentified = mods.Identified;
            ItemLevel = mods.ItemLevel;
            IsFractured = mods.HaveFractured;
            IsVeiled = mods.ItemMods.Any(m => m.DisplayName.Contains("Veil"));
        }

        if (GroundItem.HasComponent<Sockets>())
        {
            var sockets = GroundItem.GetComponent<Sockets>();
            SocketInfo = new SocketInfo(sockets.LargestLinkSize, sockets.NumberOfSockets, sockets.Links);
        }

        if (GroundItem.HasComponent<SkillGem>())
        {
            var gem = GroundItem.GetComponent<SkillGem>();
            GemInfo = new SkillGemInfo(gem.Level, gem.MaxLevel, gem.QualityType);
        }
            
        if (GroundItem.HasComponent<Stack>())
        {
            var stack = GroundItem.GetComponent<Stack>();
            StackInfo = new StackInfo(stack.Size, stack.Info.MaxStackSize);
        }

        if (GroundItem.HasComponent<Weapon>()) IsWeapon = true;

        MapTier = GroundItem.GetComponent<Map>()?.Tier;
        IsValid = true;
    }

    public Influence? InfluenceFlags { get; }
    public SkillGemInfo GemInfo { get; }
    public StackInfo StackInfo { get; }
    public SocketInfo SocketInfo { get; }
    public string BaseName { get; } = "";
    public string ClassName { get; } = "";
    public LabelOnGround LabelOnGround { get; }
    public float Distance => LabelOnGround.ItemOnGround?.DistancePlayer ?? float.PositiveInfinity;
    public Entity GroundItem { get; }

    public int Height { get; }
    public bool IsIdentified { get; }
    public bool IsHeist { get; }
    public bool IsVeiled { get; }
    public bool IsWeapon { get; }
    public int ItemLevel { get; }
    public int? MapTier { get; }
    public string Path { get; }
    public int Quality { get; }
    public ItemRarity Rarity { get; }
    public int Width { get; }
    public bool IsFractured { get; }

    public override string ToString()
    {
        return $"{BaseName} ({ClassName}) Dist: {Distance}";
    }
}