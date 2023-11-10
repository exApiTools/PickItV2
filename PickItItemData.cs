using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Elements;
using ItemFilterLibrary;

namespace PickIt;

public class PickItItemData : ItemData
{
    public PickItItemData(LabelOnGround queriedItem, FilesContainer fs, AreaController area) : base(queriedItem, fs)
    {
        AreaInfo = new AreaData(area.CurrentArea.RealLevel, area.CurrentArea.Name, area.CurrentArea.Act, area.CurrentArea.Act > 10);
    }

    public int AttemptedPickups { get; set; }
}