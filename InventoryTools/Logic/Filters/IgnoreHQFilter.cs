using CriticalCommonLib.Models;
using CriticalCommonLib.Sheets;
using InventoryTools.Logic.Filters.Abstract;

namespace InventoryTools.Logic.Filters;

public class IgnoreHQFilter : BooleanFilter
{
    public override string Key { get; set; } = "IgnoreHQFilter";
    public override string Name { get; set; } = "Ignore HQ Filter?";

    public override string HelpText { get; set; } =
        "When sorting should the filter consider HQ and NQ items to be the same when attempting to stack them? This primary use for this filter is to find items that can have their quality lowered.";

    public override FilterCategory FilterCategory { get; set; } = FilterCategory.Advanced;
    public override FilterType AvailableIn { get; set; } = FilterType.SortingFilter | FilterType.CraftFilter;

    public override void UpdateFilterConfiguration(FilterConfiguration configuration, bool? newValue)
    {
        configuration.IgnoreHQWhenSorting = newValue;
    }

    public override bool? CurrentValue(FilterConfiguration configuration)
    {
        return configuration.IgnoreHQWhenSorting;
    }

    public override bool? FilterItem(FilterConfiguration configuration, InventoryItem item)
    {
        return true;
    }

    public override bool? FilterItem(FilterConfiguration configuration, ItemEx item)
    {
        return true;
    }
}