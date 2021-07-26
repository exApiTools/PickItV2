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

            Quality = (int)GroundItem.GetComponent<Quality>()?.ItemQuality;
            IsTargeted = () => itemItemOnGround?.GetComponent<Targetable>()?.isTargeted == true;
            IsValid = true;

            var mods = GroundItem.GetComponent<Mods>();
            if (mods != null)
            {
                IsVeiled = (bool)mods.ItemMods?.Any(m => m.DisplayName.Contains("Veil"));
                IsFractured = mods.HaveFractured;
            }

            var sockets = GroundItem.GetComponent<Sockets>();
            if (sockets != null)
            {
                IsRGB = sockets.IsRGB;
                Sockets = sockets.NumberOfSockets;
                LargestLink = sockets.LargestLinkSize;
            }

            var @base = GroundItem.GetComponent<Base>();
            if (@base != null)
            {
                IsElder = @base.isElder;
                IsShaper = @base.isShaper;
                IsHunter = @base.isHunter;
                IsRedeemer = @base.isRedeemer;
                IsCrusader = @base.isCrusader;
                IsWarlord = @base.isWarlord;
            }

            var path = GroundItem.Path;
            var baseItemType = fs.BaseItemTypes.Translate(path);
            if (baseItemType != null)
            {
                BaseName = baseItemType.BaseName;
                ClassName = baseItemType.ClassName;
                Width = baseItemType.Width;
                Height = baseItemType.Height;
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
    }
}