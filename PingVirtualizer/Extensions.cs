using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace PingVirtualizer
{
    public static class Extensions
    {
        public static double Deviation(this IEnumerable<decimal?> source)
        {
            var average = source.Average();

            return Math.Sqrt(source.Average(o => Math.Pow(Convert.ToDouble(o - average), 2)));
        }

        public static double Deviation<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
        {
            return source.Select(selector).Deviation();
        }

        public static double ToUTCUnixTimestamp(this DateTime value)
        {
            return (value - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime()).TotalSeconds;
        }

        public static void OrderPositionsBy<TKey>(this SeriesCollection seriesCollection, Func<Series, bool> predicate, Func<Series, TKey> keySelector)
        {
            var ordered = seriesCollection.Where(predicate).OrderByDescending(keySelector).ToList();

            foreach (var item in ordered)
            {
                seriesCollection.Remove(item);
                seriesCollection.Add(item);
            }
        }
    }
}

