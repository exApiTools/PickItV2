using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using Map = ExileCore.PoEMemory.Components.Map;

namespace PickIt
{
    public class CustomItem
    {
        public Func<bool> IsTargeted;
        public bool IsValid;

        public CustomItem(LabelOnGround item, FilesContainer fs, float distance, Dictionary<string, int> weightsRules)
        {
            LabelOnGround = item;
            Distance = distance;
            var itemItemOnGround = item.ItemOnGround;
            var worldItem = itemItemOnGround?.GetComponent<WorldItem>();
            if (worldItem == null) return;
            var groundItem = worldItem.ItemEntity;
            GroundItem = groundItem;
            if (GroundItem == null) return;

            if (PickIt.Plugin.Settings.Flasks || PickIt.Plugin.Settings.Gems)
                Quality = (int)GroundItem.GetComponent<Quality>()?.ItemQuality;

            if (PickIt.Plugin.Settings.Maps)
                MapTier = GroundItem.HasComponent<Map>() ? GroundItem.GetComponent<Map>().Tier : 0;

            IsTargeted = () => itemItemOnGround?.GetComponent<Targetable>()?.isTargeted == true;
            IsValid = true;

            var mods = GroundItem.GetComponent<Mods>();
            if (mods != null)
            {
                if (PickIt.Plugin.Settings.PickUpInfluenced)
                {
                    IsVeiled = (bool)mods.ItemMods?.Any(m => m.DisplayName.Contains("Veil"));
                    IsFractured = mods.HaveFractured;
                }

                if (PickIt.Plugin.Settings.AllUniques
                    || PickIt.Plugin.Settings.UniqueMap
                    || PickIt.Plugin.Settings.FullRareSetManager
                    )
                    Rarity = mods.ItemRarity;

                if (PickIt.Plugin.Settings.FullRareSetManager)
                {
                    IsIdentified = mods.Identified;
                    ItemLevel = mods.ItemLevel;
                }
            }

            var sockets = GroundItem.GetComponent<Sockets>();
            if (sockets != null)
            {
                if (PickIt.Plugin.Settings.RGB)
                    IsRGB = sockets.IsRGB;
                if (PickIt.Plugin.Settings.SixSockets)
                    Sockets = sockets.NumberOfSockets;
                if (PickIt.Plugin.Settings.SixLinks)
                    LargestLink = sockets.LargestLinkSize;
            }

            var @base = GroundItem.GetComponent<Base>();
            if (PickIt.Plugin.Settings.PickUpInfluenced && @base != null)
            {
                IsElder = @base.isElder;
                IsShaper = @base.isShaper;
                IsHunter = @base.isHunter;
                IsRedeemer = @base.isRedeemer;
                IsCrusader = @base.isCrusader;
                IsWarlord = @base.isWarlord;
            }

            Path = GroundItem.Path;
            var baseItemType = fs.BaseItemTypes.Translate(Path);
            if (baseItemType != null)
            {
                if (PickIt.Plugin.Settings.PickUpByHardcodedNames)
                    BaseName = baseItemType.BaseName;

                if (PickIt.Plugin.Settings.Flasks
                    || PickIt.Plugin.Settings.Gems
                    || PickIt.Plugin.Settings.AllCurrency
                    || PickIt.Plugin.Settings.AllDivs
                    || PickIt.Plugin.Settings.QuestItems
                    || PickIt.Plugin.Settings.MapFragments
                    || PickIt.Plugin.Settings.FullRareSetManager
                    )
                    ClassName = baseItemType.ClassName;

                if (PickIt.Plugin.Settings.RGB)
                {
                    Width = baseItemType.Width;
                    Height = baseItemType.Height;
                }
            }

            if (PickIt.Plugin.Settings.FullRareSetManager && GroundItem.HasComponent<Weapon>())
            {
                IsWeapon = true;
            }
        }
        public LabelOnGround LabelOnGround { get; }
        public float Distance { get; }
        public Entity GroundItem { get; }
        public string BaseName { get; } = "";
        public string ClassName { get; } = "";
        public int Quality { get; }
        public bool IsVeiled { get; }
        public bool IsFractured { get; }
        public bool IsRGB { get; }
        public int Sockets { get; }
        public int LargestLink { get; }
        public bool IsShaper { get; }
        public bool IsHunter { get; }
        public bool IsRedeemer { get; }
        public bool IsCrusader { get; }
        public bool IsWarlord { get; }
        public bool IsElder { get; }
        public int Height { get; }
        public int Width { get; }
        public string Path { get; }
        public ItemRarity Rarity { get; }
        public bool IsIdentified { get; }
        public bool IsWeapon { get; }
        public int ItemLevel { get; }
        public int MapTier { get; }
    }
}