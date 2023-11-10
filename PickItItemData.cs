using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Elements;
using ItemFilterLibrary;

namespace PickIt;

public class PickItItemData : ItemData
{
    public PickItItemData(LabelOnGround queriedItem, GameController gc) : base(queriedItem, gc)
    {
    }

    public int AttemptedPickups { get; set; }
}