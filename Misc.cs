using System;
using System.Linq;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using SharpDX;

namespace PickIt;

public partial class PickIt
{
    private bool CanFitInventory(ItemData groundItem)
    {
        return FindSpotInventory(groundItem) != null;
    }

    /// <summary>
    /// Finds a spot available in the inventory to place the item
    /// </summary>
    private Vector2? FindSpotInventory(ItemData item)
    {
        var inventorySlots = InventorySlots;
        var inventoryItems = _inventoryItems.InventorySlotItems;
        const int width = 12;
        const int height = 5;

        if (inventorySlots == null)
            return null;

        var itemToStackWith = inventoryItems.FirstOrDefault(x => CanItemBeStacked(item, x));
        if (itemToStackWith != null)
        {
            return new Vector2(itemToStackWith.PosX, itemToStackWith.PosY);
        }

        for (var yCol = 0; yCol < height - (item.Height - 1); yCol++)
        for (var xRow = 0; xRow < width - (item.Width - 1); xRow++)
        {
            var obstructed = false;

            for (var xWidth = 0; xWidth < item.Width && !obstructed; xWidth++)
            for (var yHeight = 0; yHeight < item.Height && !obstructed; yHeight++)
            {
                obstructed |= inventorySlots[yCol + yHeight, xRow + xWidth];
            }

            if (!obstructed) return new Vector2(xRow, yCol);
        }

        return null;
    }

    private static bool CanItemBeStacked(ItemData item, ServerInventory.InventSlotItem inventoryItem)
    {
        if (item.Entity.Path != inventoryItem.Item.Path)
            return false;

        if (!item.Entity.HasComponent<Stack>() || !inventoryItem.Item.HasComponent<Stack>())
            return false;

        var itemStackComp = item.Entity.GetComponent<Stack>();
        var inventoryItemStackComp = inventoryItem.Item.GetComponent<Stack>();

        /*
         * Reserved if the itemlevel is ever found as incubators dont have a mods comp?? why.
        if (item.BaseName.EndsWith(" Incubator") && inventoryItem.Item.HasComponent<Mods>())
        {
            return (item.ItemLevel == inventoryItem.Item.GetComponent<Mods>().ItemLevel) && inventoryItemStackComp.Size + itemStackComp.Size <= inventoryItemStackComp.Info.MaxStackSize;
        }
        */

        return inventoryItemStackComp.Size + itemStackComp.Size <= inventoryItemStackComp.Info.MaxStackSize;
    }

    private bool[,] GetContainer2DArray(ServerInventory containerItems)
    {
        var containerCells = new bool[containerItems.Rows, containerItems.Columns];

        try
        {
            foreach (var item in containerItems.InventorySlotItems)
            {
                var itemSizeX = item.SizeX;
                var itemSizeY = item.SizeY;
                var inventPosX = item.PosX;
                var inventPosY = item.PosY;
                var startX = Math.Max(0, inventPosX);
                var startY = Math.Max(0, inventPosY);
                var endX = Math.Min(containerItems.Columns, inventPosX + itemSizeX);
                var endY = Math.Min(containerItems.Rows, inventPosY + itemSizeY);
                for (var y = startY; y < endY; y++)
                for (var x = startX; x < endX; x++)
                    containerCells[y, x] = true;
            }
        }
        catch (Exception e)
        {
            // ignored
            LogMessage(e.ToString(), 5);
        }

        return containerCells;
    }
}