using ExileCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using SharpDX;
using System.Linq;
using PickIt;
using MoreLinq;

namespace PickIt
{
    public class ItemFilterData
    {
        public string Query { get; set; }
        public Func<ItemData, bool> CompiledQuery { get; set; }
        public int InitialLine { get; set; }
    }
    public class ItemFilter
    {
        public static List<ItemFilterData> Load(string filterFilePath)
        {
            return CacheQueries(filterFilePath);
        }

        private static List<ItemFilterData> CacheQueries(string filterFilePath)
        {
            var _compiledQueries = new List<ItemFilterData>();
            var lines = File.ReadAllLines(filterFilePath);
            var sections = new List<string>();
            var section = string.Empty;
            var sectionLineNumber = 0;
            var sanitizedLineNumber = 0;

            foreach (var (line, index) in lines.Select((value, i) => (value, i)))
            {
                if (!string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("//"))
                {
                    if (string.IsNullOrEmpty(section))
                    {
                        sectionLineNumber = index + 1;
                        sanitizedLineNumber = sectionLineNumber; // Set sanitizedLineNumber at the start of each section
                    }
                    section += line + "\n";
                }
                else if (!string.IsNullOrEmpty(section))
                {
                    try
                    {
                        string[] parts = section.Split(new[] { "//" }, StringSplitOptions.None);
                        string sanitizedQuery = SanitizeQuery(parts[0].Trim());
                        LambdaExpression lambda = ParseItemDataLambda(sanitizedQuery);
                        var compiledLambda = lambda.Compile();
                        _compiledQueries.Add(new ItemFilterData
                        {
                            Query = section,
                            CompiledQuery = (Func<ItemData, bool>)compiledLambda,
                            InitialLine = sanitizedLineNumber
                        });
                    }
                    catch (Exception e)
                    {
                        DebugWindow.LogError($"[ItemQueryProcessor] Error caching query ({section}) on Line # {sanitizedLineNumber}: {e.Message}", 30);
                    }
                    sections.Add(section.Trim());
                    section = string.Empty;
                }
            }

            DebugWindow.LogMsg($@"[ItemQueryProcessor] Processed {filterFilePath.Split("\\").LastOrDefault()} with {_compiledQueries.Count} queries", 15, Color.Orange);
            return _compiledQueries;
        }



        private static LambdaExpression ParseItemDataLambda(string expression)
        {
            ParameterExpression itemParameter = Expression.Parameter(typeof(ItemData), "item");
            return DynamicExpressionParser.ParseLambda(
                new[] { itemParameter },
                typeof(bool),
                expression);
        }

        private static string SanitizeQuery(string query)
        {
            // for later if there is more to do.
            return query;
        }
    }
}
