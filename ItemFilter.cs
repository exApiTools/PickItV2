using ExileCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using SharpDX;
using System.Linq;
using System.Linq.Dynamic.Core.Exceptions;

namespace PickIt;

public class ItemFilterData
{
    public string Query { get; set; }
    public string RawQuery { get; set; }
    public Func<ItemData, bool> CompiledQuery { get; set; }
    public int InitialLine { get; set; }
}

public class ItemFilter
{
    private readonly List<ItemFilterData> _queries;

    private static readonly ParsingConfig ParsingConfig = new ParsingConfig()
    {
        AllowNewToEvaluateAnyType = true,
        ResolveTypesBySimpleName = true,
        CustomTypeProvider = new CustomDynamicLinqCustomTypeProvider(),
    };

    private ItemFilter(List<ItemFilterData> queries)
    {
        _queries = queries;
    }

    public static ItemFilter Load(string filterFilePath)
    {
        return new ItemFilter(GetQueries(filterFilePath));
    }

    public bool Matches(ItemData item)
    {
        foreach (var cachedQuery in _queries)
        {
            try
            {
                if (cachedQuery.CompiledQuery(item))
                {
                    DebugWindow.LogMsg($"[ItemQueryProcessor] Matches an Item\nLine # {cachedQuery.InitialLine}\nItem({item.BaseName})\n{cachedQuery.Query.Replace("\n", "")}", 10, Color.LawnGreen);
                    return true; // Stop further checks once a match is found
                }
            }
            catch
            {
                // skip error for now, its a huge issue when the amount of catching starts creeping up
                // 4500 lines that procude an error on one item take 50ms per Tick() vs handling the error taking 0.2ms
                //DebugWindow.LogError($"Evaluation Error! Line # {cachedQuery.InitialLine} Entry: '{cachedQuery.Query}' Item {item.BaseName}\n{ex}");
                continue;
            }
        }

        return false;
    }

    private static List<ItemFilterData> GetQueries(string filterFilePath)
    {
        var compiledQueries = new List<ItemFilterData>();
        var rawLines = File.ReadAllLines(filterFilePath);
        var lines = SplitQueries(rawLines);

        foreach (var (query, rawQuery, initialLine) in lines)
        {
            try
            {
                var lambda = ParseItemDataLambda(query);
                var compiledLambda = lambda.Compile();
                compiledQueries.Add(new ItemFilterData
                {
                    Query = query,
                    RawQuery = rawQuery,
                    CompiledQuery = compiledLambda,
                    InitialLine = initialLine
                });
            }
            catch (Exception ex)
            {
                var exMessage = ex is ParseException parseEx
                    ? $"{parseEx.Message} (at index {parseEx.Position})"
                    : ex.ToString();
                DebugWindow.LogError($"[ItemQueryProcessor] Error processing query ({query.Replace("\n", "")}) on Line # {initialLine}: {exMessage}", 15);
            }
        }

        DebugWindow.LogMsg($@"[ItemQueryProcessor] Processed {filterFilePath.Split("\\").LastOrDefault()} with {compiledQueries.Count} queries", 2, Color.Orange);
        return compiledQueries;
    }

    private static List<(string section, string rawSection, int sectionStartLine)> SplitQueries(string[] rawLines)
    {
        string section = null;
        string rawSection = null;
        var sectionStartLine = 0;
        var lines = new List<(string section, string rawSection, int sectionStartLine)>();

        foreach (var (line, index) in rawLines.Append("").Select((value, i) => (value, i)))
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                var lineWithoutComment = line.IndexOf("//", StringComparison.Ordinal) is var commentIndex and not -1
                    ? line[..commentIndex]
                    : line;
                if (section == null)
                {
                    sectionStartLine = index + 1; // Set at the start of each section
                }

                section += $"{lineWithoutComment}\n";
                rawSection += $"{line}\n";
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(section))
                {
                    lines.Add((section, rawSection.TrimEnd('\n'), sectionStartLine));
                }

                section = null;
                rawSection = null;
            }
        }

        return lines;
    }

    private static Expression<Func<ItemData, bool>> ParseItemDataLambda(string expression)
    {
        return DynamicExpressionParser.ParseLambda<ItemData, bool>(ParsingConfig, false, expression);
    }
}