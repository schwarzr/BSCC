using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Codeworx.Battleship.Player.Extensions
{
    public static class RandomExtesions
    {

        public static TElement Pick<TElement>(this IList<TElement> items, Random random)
        {
            if (items.Count == 1)
            {
                return items[0];
            }

            return items[random.Next(items.Count)];
        }

        public static IEnumerable<TElement> Pick<TElement>(this IList<TElement> items, Random random, int count)
        {
            if (items.Count <= count)
            {
                foreach (var item in items)
                {
                    yield return item;
                }
            }
            else
            {
                var used = new List<int>();

                for (int i = 0; i < count; i++)
                {
                    int next;
                    do
                    {
                        next = random.Next(items.Count);
                    } while (used.Contains(next));

                    used.Add(next);
                    yield return items[next];
                }
            }
        }
    }
}
