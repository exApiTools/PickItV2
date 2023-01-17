using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.Shared.Enums;
using FilterCore;
using FilterCore.Entry;
using FilterCore.Line;
using FilterDomain.LineStrategy;

namespace PickIt;

public class ItemFilterProcessor
{
    private static readonly IReadOnlyDictionary<string, Func<CustomItem, double?>> NumericValueExtractors = new Dictionary<string, Func<CustomItem, double?>>
    {
        ["ItemLevel"] = i => i.ItemLevel,
        ["Width"] = i => i.Width,
        ["Height"] = i => i.Height,
        ["Quality"] = i => i.Quality,
        ["MapTier"] = i => i.MapTier,
        ["StackSize"] = i => i.StackInfo?.Count,
        ["LinkedSockets"] = i => i.SocketInfo?.LargestLinkSize,
        ["Sockets"] = i => i.SocketInfo?.SocketNumber,
        ["GemLevel"] = i => i.GemInfo?.Level,
        ["Rarity"] = i => (int)i.Rarity is var ii
            ? ii switch
            {
                >=(int)ItemRarity.Normal and <= (int)ItemRarity.Unique => ii,
                _ => null
            }
            : null,
    };

    private static readonly IReadOnlyDictionary<string, Func<CustomItem, string>> StringValueExtractors = new Dictionary<string, Func<CustomItem, string>>
    {
        ["BaseType"] = i => i.BaseName,
        ["Class"] = i => i.ClassName,
        ["GemQualityType"] = i => i.GemInfo?.QualityType.ToString()
    };


    public Filter Filter { get; }

    public ItemFilterProcessor(Filter filter)
    {
        Filter = filter;
    }

    public bool ShowItem(CustomItem item)
    {
        return Filter.FilterEntries.Where(x => x.Header.Type == FilterGenerationConfig.FilterEntryType.Content).FirstOrDefault(x => IsApplicable(item, x))?.Header
            .HeaderValue == "Show";
    }

    private bool IsApplicable(CustomItem item, IFilterEntry filterEntry)
    {
        return filterEntry.Content.Content.All(lineGroup => lineGroup.Value.All(filterLine => IsApplicable(item, filterLine)));
    }

    private bool IsApplicable(CustomItem item, IFilterLine line)
    {
        if (NumericValueExtractors.TryGetValue(line.Ident, out var extractor))
        {
            if (line.Value is not NumericValueContainer nvc)
            {
                throw new FilterProcessorException(line, item, "Unsupported value container");
            }

            var testedValue = extractor(item);
            if (testedValue == null)
            {
                return false;
            }

            return NumericFits(nvc, testedValue.Value);
        }

        if (StringValueExtractors.TryGetValue(line.Ident, out var sExtractor))
        {
            if (line.Value is not EnumValueContainer evc)
            {
                throw new FilterProcessorException(line, item, "Unsupported value container");
            }

            var testedValue = sExtractor(item);
            if (testedValue == null)
            {
                return false;
            }

            return evc.Value.Select(x => x.value).Any(testedValue.Contains);
        }

        switch (line.Ident)
        {
            case "HasInfluence":
            {
                if (line.Value is not EnumValueContainer evc)
                {
                    throw new FilterProcessorException(line, item, "Unsupported value container");
                }

                return (item.InfluenceFlags ?? Influence.None) == Influence.None && evc.Value.Any(x => x.value == "None") ||
                       item.InfluenceFlags != null && item.InfluenceFlags.Value.GetFlags().Select(x => x.ToString())
                           .Intersect(evc.Value.Select(x => x.value))
                           .Any();
            }
            //actions do not filter
            case "SetBorderColor":
            case "SetTextColor":
            case "SetBackgroundColor":
            case "SetFontSize":
            case "PlayAlertSound":
            case "PlayAlertSoundPositional":
            case "DisableDropSound":
            case "EnableDropSound":
            case "CustomAlertSound":
            case "MinimapIcon":
            case "PlayEffect":
                return true;
            default:
                throw new FilterProcessorException(line, item, "Unsupported condition");
        }
    }

    private bool NumericFits(NumericValueContainer valueSpec, double value)
    {
        var specValue = double.TryParse(valueSpec.Value, out var doubleValue)
            ? doubleValue
            : Enum.TryParse<ItemRarity>(valueSpec.Value, out var rarityValue)
                ? (int)rarityValue
                : throw new Exception("Unable to parse");
        switch (valueSpec.Operator)
        {
            case ">=":
                return value >= specValue;
            case ">":
                return value > specValue;
            case "=":
            case "==":
            case null:
                return value == specValue;
            case "<":
                return value < specValue;
            case "<=":
                return value <= specValue;
            default:
                throw new Exception("Unknown operator");
        }
    }
}