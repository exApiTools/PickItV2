using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ItemFilterLibrary;

namespace PickIt;

public class PickItItemData : ItemData
{
    public PickItItemData(ItemsOnGroundLabelElement.VisibleGroundItemDescription queriedItem, GameController gc)
        : base(queriedItem.Entity?.GetComponent<WorldItem>()?.ItemEntity, queriedItem.Entity, gc)
    {
        QueriedItem = queriedItem;
    }

    public ItemsOnGroundLabelElement.VisibleGroundItemDescription QueriedItem { get; }
    public int AttemptedPickups { get; set; }
}