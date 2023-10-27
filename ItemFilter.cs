using ExileCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using SharpDX;
using System.Linq;
using PickIt;

namespace PickIt
{
    public class ItemFilterData
    {
        public string Query { get; set; }
        public Func<ItemData, bool> CompiledQuery { get; set; }
        public int LineNumber { get; set; }
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
            string[] queries = File.ReadAllLines(filterFilePath);

            for (int i = 0; i < queries.Length; i++)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(queries[i]) || queries[i].Trim().StartsWith("//"))
                    {
                        continue;
                    }

                    // I should make it so users can import this class and deal with cleaning their own queries
                    // TODO: create a different method that takes in a single line and the users can handle their query filter to allow for alternative storage
                    var processedLine = queries[i].Split(new string[] { "//" }, StringSplitOptions.None)[0].Trim();

                    string sanitizedQuery = SanitizeQuery(processedLine);
                    LambdaExpression lambda = ParseItemDataLambda(sanitizedQuery);
                    var compiledLambda = lambda.Compile();
                    _compiledQueries.Add(new ItemFilterData
                    {
                        Query = processedLine,
                        CompiledQuery = (Func<ItemData, bool>)compiledLambda,
                        LineNumber = i
                    });
                }
                catch (Exception e)
                {
                    DebugWindow.LogError($"[ItemQueryProcessor] Error caching query ({queries[i]}) on Line # {i + 1}: {e.Message}", 30);
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
            string sanitizedQuery = query.Trim().Replace("\n", ""); // Escape double quotes
            return sanitizedQuery;
        }
    }
}
