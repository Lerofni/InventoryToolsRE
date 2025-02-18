﻿using CriticalCommonLib.Models;
using CriticalCommonLib.Sheets;
using InventoryTools.Logic.Columns.Abstract;

namespace InventoryTools.Logic.Columns
{
    public class ItemLevelColumn : IntegerColumn
    {
        public override int? CurrentValue(InventoryItem item)
        {
            return CurrentValue(item.Item);
        }

        public override int? CurrentValue(ItemEx item)
        {
            return item.LevelEquip;
        }

        public override int? CurrentValue(SortingResult item)
        {
            return CurrentValue(item.InventoryItem);
        }

        public override string Name { get; set; } = "Item Level";
        public override float Width { get; set; } = 80.0f;
        public override string HelpText { get; set; } = "Shows the level required to equip the item.";
        public override bool HasFilter { get; set; } = true;
        public override ColumnFilterType FilterType { get; set; } = ColumnFilterType.Text;
    }
}