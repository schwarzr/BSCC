using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codeworx.Battleship.Generator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var data = await File.ReadAllBytesAsync(@"C:\Temp\battleship.data");

            var hits = new Dictionary<int, Shot>();

            ConsoleKeyInfo key;

            var sb = new StringBuilder();

            sb.AppendLine("namespace Codeworx.Battleship.Generator {");
            sb.AppendLine("    public abstract class BattleshipStrategy {");
            sb.AppendLine("        public abstract int Fallback();");
            sb.AppendLine("        public abstract CellState GetState(int cell);");
            sb.AppendLine("");
            sb.AppendLine("        public int Next(){");

            GenerateCode(sb, data);
            sb.AppendLine("            return Fallback();");

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            if (File.Exists("C:\\temp\\sample.cs"))
            {
                File.Delete("c:\\temp\\sample.cs");
            }

            File.WriteAllText("c:\\temp\\sample.cs", sb.ToString());

            return;

            do
            {
                int[] count;
                int totalHits;
                var nextIndex = GetNextIndex(data, hits, new int[] { }, out count, out totalHits);

                Console.WriteLine();

                Console.WriteLine($"TotalHits: {totalHits}");

                Console.WriteLine("|--|--|--|--|--|--|--|--|--|--|");
                for (int y = 0; y < 10; y++)
                {
                    Console.Write("|");
                    for (int x = 0; x < 10; x++)
                    {
                        if (y * 10 + x == nextIndex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("??");
                            Console.ResetColor();
                        }
                        else if (hits.TryGetValue(y * 10 + x, out var shot))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write(shot == Shot.Hit ? "XX" : "  ");
                            Console.ResetColor();
                        }
                        else
                        {
                            var value = (Convert.ToDouble(count[y * 10 + x]) / totalHits) * 100;
                            Console.Write($"{value:00}");
                        }


                        Console.Write("|");
                    }
                    Console.WriteLine();
                }

                Console.WriteLine("|--|--|--|--|--|--|--|--|--|--|");

                key = Console.ReadKey();
                if (key.Key == ConsoleKey.W)
                {
                    hits.Add(nextIndex, Shot.Water);
                }
                else if (key.Key == ConsoleKey.H)
                {
                    hits.Add(nextIndex, Shot.Hit);
                }
                else if (key.Key == ConsoleKey.S)
                {
                    Sink(nextIndex, hits);
                }


            } while (key.Key != ConsoleKey.X);
        }

        private static void GenerateCode(StringBuilder sb, byte[] data, Dictionary<int, Shot> hits = null, int[] resolve = null, int level = 0)
        {
            hits = hits ?? new Dictionary<int, Shot>();

            var space = new string(' ', (hits.Count + 3) * 4);

            resolve = resolve ?? new int[] { };

            var nextIndex = GetNextIndex(data, hits, resolve, out int[] count, out int totalHits);

            if (nextIndex == -1 || level >= 10 || totalHits < 500)
            {
                sb.AppendLine(space + "    return Fallback();");
                return;
            }

            sb.AppendLine(space + $"// samplesize: {totalHits}");
            sb.AppendLine(space + $"switch(GetState({nextIndex})){{");

            sb.AppendLine(space + $"    case CellState.None:");
            sb.AppendLine(space + $"        return {nextIndex};");

            sb.AppendLine(space + $"    case CellState.Water:");
            var hitsw = new Dictionary<int, Shot>(hits);
            hitsw.Add(nextIndex, Shot.Water);
            GenerateCode(sb, data, hitsw, resolve, level + 1);
            sb.AppendLine(space + $"    break;");

            sb.AppendLine(space + $"    case CellState.Hit:");
            var hitsh = new Dictionary<int, Shot>(hits);
            hitsh.Add(nextIndex, Shot.Hit);

            var nextResolve = new int[resolve.Length + 1];
            Array.Copy(resolve, nextResolve, resolve.Length);
            nextResolve[resolve.Length] = nextIndex;

            GenerateCode(sb, data, hitsh, nextResolve, level + 1);
            sb.AppendLine(space + $"    break;");

            sb.AppendLine(space + $"    case CellState.Sunk:");
            var hitss = new Dictionary<int, Shot>(hits);
            Sink(nextIndex, hitss);
            GenerateCode(sb, data, hitss, level: level + 1);
            sb.AppendLine(space + $"    break;");

            sb.AppendLine(space + "}");

        }

        private static int GetNextIndex(byte[] data, Dictionary<int, Shot> hits, int[] resolve, out int[] count, out int totalHits)
        {
            List<int> filter = new List<int>();

            if (resolve.Length > 0)
            {
                var minCell = resolve.Min();
                var maxCell = resolve.Max();

                bool horizontal = false;
                bool vertical = false;

                if (minCell == maxCell)
                {
                    horizontal = true;
                    vertical = true;
                }
                else if (maxCell < minCell + 10)
                {
                    horizontal = true;
                }
                else
                {
                    vertical = true;
                }

                if (horizontal)
                {
                    if (minCell % 10 > 0)
                    {
                        if (!hits.ContainsKey(minCell - 1))
                        {
                            filter.Add(minCell - 1);
                        }
                    }

                    if (maxCell % 10 < 9)
                    {
                        if (!hits.ContainsKey(maxCell + 1))
                        {
                            filter.Add(maxCell + 1);
                        }
                    }
                }

                if (vertical)
                {
                    if (minCell >= 10)
                    {
                        if (!hits.ContainsKey(minCell - 10))
                        {
                            filter.Add(minCell - 10);
                        }
                    }

                    if (maxCell < 90)
                    {
                        if (!hits.ContainsKey(maxCell + 10))
                        {
                            filter.Add(maxCell + 10);
                        }
                    }
                }
            }

            count = GetStatistics(hits, data, filter, out totalHits);
            var max = count.Max();

            var nextIndex = 0;

            if (max == 0)
            {
                if (filter.Count > 0)
                {
                    foreach (var item in filter)
                    {
                        if (!hits.ContainsKey(item))
                        {
                            return item;
                        }
                    }
                }

                return -1;
                //throw new NotSupportedException("this should not happen!!!");
            }

            for (int i = 0; i < 100; i++)
            {
                if (count[i] == max)
                {
                    nextIndex = i;
                    break;
                }
            }

            return nextIndex;
        }

        private static void Sink(int nextIndex, Dictionary<int, Shot> hits)
        {
            hits.Add(nextIndex, Shot.Hit);

            var x = nextIndex % 10;
            var y = nextIndex / 10;

            var leftx = x - 1;
            var rightx = x + 1;
            var topy = y - 1;
            var bottomy = y + 1;

            var horizontal = false;

            if (x > 0 && hits.TryGetValue(y * 10 + leftx, out var shot) && shot == Shot.Hit)
            {
                horizontal = true;
            }
            if (x < 9 && hits.TryGetValue(y * 10 + rightx, out var shot2) && shot2 == Shot.Hit)
            {
                horizontal = true;
            }

            if (horizontal)
            {
                var search = x - 1;
                while (search >= 0)
                {
                    if (hits.TryGetValue(y * 10 + search, out var tmp))
                    {
                        if (tmp == Shot.Water)
                        {
                            break;
                        }
                    }
                    else
                    {
                        hits.Add(y * 10 + search, Shot.Water);
                        break;
                    }
                    search--;
                }
                search = x + 1;
                while (search <= 9)
                {
                    if (hits.TryGetValue(y * 10 + search, out var tmp))
                    {
                        if (tmp == Shot.Water)
                        {
                            break;
                        }
                    }
                    else
                    {
                        hits.Add(y * 10 + search, Shot.Water);
                        break;
                    }
                    search++;
                }
            }
            else
            {
                var search = y - 1;
                while (search >= 0)
                {
                    if (hits.TryGetValue(search * 10 + x, out var tmp))
                    {
                        if (tmp == Shot.Water)
                        {
                            break;
                        }
                    }
                    else
                    {
                        hits.Add(search * 10 + x, Shot.Water);
                        break;
                    }
                    search--;
                }
                search = y + 1;
                while (search <= 9)
                {
                    if (hits.TryGetValue(search * 10 + x, out var tmp))
                    {
                        if (tmp == Shot.Water)
                        {
                            break;
                        }
                    }
                    else
                    {
                        hits.Add(search * 10 + x, Shot.Water);
                        break;
                    }
                    search++;
                }
            }
        }

        private static int[] GetStatistics(Dictionary<int, Shot> hits, byte[] data, List<int> filter, out int totalHits)
        {
            var count = new int[100];
            var gameCount = data.Length / 13;
            totalHits = 0;

            for (int x = 0; x < gameCount; x++)
            {
                var start = x * 13;
                bool next = false;

                foreach (var hit in hits)
                {
                    var buffer = data[start + (hit.Key / 8)];
                    var mask = 0x80 >> (hit.Key % 8);

                    if (hit.Value == Shot.Water)
                    {
                        if ((buffer & mask) != 0)
                        {
                            next = true;
                            break;
                        }
                    }
                    else
                    {
                        if ((buffer & mask) == 0)
                        {
                            next = true;
                            break;
                        }
                    }

                }

                if (next)
                {
                    continue;
                }

                totalHits++;
                for (int i = 0; i < 100; i++)
                {
                    if (filter.Count == 0 || filter.Contains(i))
                    {

                        var buffer = data[start + (i / 8)];
                        var mask = 0x80 >> (i % 8);
                        if ((buffer & mask) != 0)
                        {
                            count[i]++;
                        }
                    }
                }
            }

            foreach (var item in hits.Keys)
            {
                count[item] = 0;
            }

            return count;
        }
    }
}
