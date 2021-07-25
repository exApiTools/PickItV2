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
            IsTargeted = () => itemItemOnGround?.GetComponent<Targetable>()?.isTargeted == true;
            IsValid = true;
        }
        public LabelOnGround LabelOnGround { get; }
        public float Distance { get; }
        public Entity GroundItem { get; }
    }
}