using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Elements;
using ItemFilterLibrary;

namespace PickIt;

public class PickItItemData : ItemData
{
    public PickItItemData(LabelOnGround queriedItem, FilesContainer fs) : base(queriedItem, fs)
    {
    }

    public int AttemptedPickups { get; set; }
}