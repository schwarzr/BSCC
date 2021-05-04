using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Codeworx.Battleship.Player
{
    public class FieldStateParser
    {
        private static readonly FieldState[] _allStates = { FieldState.Destroyer, FieldState.Submarine, FieldState.Cruiser, FieldState.Battleship, FieldState.Carrier };

        public static Dictionary<FieldState, int> StateLength => _stateLength;

        private static readonly Dictionary<FieldState, int> _stateLength = new Dictionary<FieldState, int>
        {
            {FieldState.Destroyer, 2},
            {FieldState.Submarine, 3},
            {FieldState.Cruiser, 3},
            {FieldState.Battleship, 4},
            {FieldState.Carrier, 5},
        };

        public static BoardState Parse(string template)
        {
            var sunkShips = new List<SunkShipOption>();

            var remainingShips = _allStates.ToList();

            var result = new FieldState[10, 10];
            for (byte i = 0; i < 10; i++)
            {
                for (byte j = 0; j < 10; j++)
                {
                    result[i, j] = FieldState.All;
                }
            }

            bool vertical;
            int length, x, y;

            int shotCount = 0;

            for (byte i = 0; i < 100; i++)
            {
                switch (template[i])
                {
                    case 'W':
                        result[i % 10, i / 10] = FieldState.None;
                        shotCount++;
                        break;
                    case 'H':

                        if (ParseHit('H', template, result, i, out vertical, out length, out x, out y))
                        {
                            MarkHit(result, x, y, length, vertical);
                        }
                        shotCount++;
                        break;
                    case 'X':

                        if (ParseHit('X', template, result, i, out vertical, out length, out x, out y))
                        {
                            var sunk = Sink(result, x, y, length, vertical);
                            sunkShips.Add(new SunkShipOption(x, y, sunk, vertical));
                            remainingShips.Remove(sunk);
                        }
                        shotCount++;
                        break;
                }
            }

            var options = GetHitResolutionOptions(result, remainingShips);

            return new BoardState(result, options, sunkShips, remainingShips.ToArray(), shotCount);
        }

        private static List<HitOption> GetHitResolutionOptions(FieldState[,] result, IEnumerable<FieldState> ships)
        {
            var options = new List<HitOption>();

            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    if ((result[x, y] & FieldState.Hit) == FieldState.Hit)
                    {
                        var currentHit = new ConcurrentDictionary<FieldState, ConcurrentDictionary<bool, List<int>>>();

                        int hitsize = 0;
                        bool horizontal = false;
                        bool vertical = false;
                        var nextRight = x < 9 ? result[x + 1, y] : FieldState.None;

                        if ((nextRight & FieldState.Hit) == FieldState.Hit)
                        {
                            var xIterator = x;
                            do
                            {
                                hitsize++;
                            } while ((GetNextX(result, ref xIterator, y) & FieldState.Hit) == FieldState.Hit);

                            horizontal = true;
                        }
                        else
                        {
                            var nextBottom = y < 9 ? result[x, y + 1] : FieldState.None;

                            if ((nextBottom & FieldState.Hit) == FieldState.Hit)
                            {
                                var yIterator = y;

                                do
                                {
                                    hitsize++;
                                } while ((GetNextY(result, x, ref yIterator) & FieldState.Hit) == FieldState.Hit);

                                vertical = true;
                            }
                            else
                            {
                                hitsize = 1;
                                vertical = true;
                                horizontal = true;
                            }
                        }


                        foreach (var item in ships)
                        {
                            if ((result[x, y] & item) == item)
                            {
                                var totalSize = _stateLength[item];

                                if (horizontal)
                                {
                                    var startx = x - (totalSize - hitsize);
                                    startx = startx < 0 ? 0 : startx;

                                    while (startx <= x)
                                    {
                                        bool works = true;

                                        for (int i = 0; i < totalSize; i++)
                                        {
                                            if (startx + i > 9 || result[startx + i, y] == FieldState.None)
                                            {
                                                works = false;
                                                break;
                                            }
                                        }

                                        if (works)
                                        {
                                            currentHit
                                                .GetOrAdd(item, p => new ConcurrentDictionary<bool, List<int>>())
                                                .GetOrAdd(false, p => new List<int>())
                                                .Add(startx);
                                        }

                                        startx++;
                                    }
                                }

                                if (vertical)
                                {
                                    var starty = y - (totalSize - hitsize);
                                    starty = starty < 0 ? 0 : starty;

                                    while (starty <= y)
                                    {
                                        bool works = true;

                                        for (int i = 0; i < totalSize; i++)
                                        {
                                            if (starty + i > 9 || result[x, starty + i] == FieldState.None)
                                            {
                                                works = false;
                                                break;
                                            }
                                        }

                                        if (works)
                                        {
                                            currentHit
                                                .GetOrAdd(item, p => new ConcurrentDictionary<bool, List<int>>())
                                                .GetOrAdd(true, p => new List<int>())
                                                .Add(starty);
                                        }

                                        starty++;
                                    }
                                }
                            }
                        }
                        if (currentHit.Count == 0)
                        {
                            throw new NotSupportedException("This should not happen!!!");
                        }

                        for (int i = 0; i < hitsize; i++)
                        {
                            if (horizontal)
                            {
                                result[x + i, y] = FieldState.None;
                            }
                            if (vertical)
                            {
                                result[x, y + i] = FieldState.None;
                            }
                        }


                        options.Add(new HitOption(x, y, hitsize, vertical, currentHit.ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableDictionary(x => x.Key, x => x.Value.ToArray()))));
                    }
                }
            }
            return options;
        }

        private static FieldState GetNextX(FieldState[,] result, ref int x, int y)
        {
            return x < 9 ? result[++x, y] : FieldState.None;
        }

        private static FieldState GetNextY(FieldState[,] result, int x, ref int y)
        {
            return y < 9 ? result[x, ++y] : FieldState.None;
        }

        private static FieldState GetPossibleShips(int size)
        {
            switch (size)
            {
                case 2:
                    return FieldState.Destroyer | FieldState.Submarine | FieldState.Cruiser | FieldState.Battleship | FieldState.Carrier;
                case 3:
                    return FieldState.Submarine | FieldState.Cruiser | FieldState.Battleship | FieldState.Carrier;
                case 4:
                    return FieldState.Battleship | FieldState.Carrier;
                case 5:
                    return FieldState.Carrier;
            }

            throw new NotSupportedException("This should not happen!");
        }

        private static void MarkHit(FieldState[,] result, int x, int y, int currentLength, bool vertical = false)
        {
            var state = GetPossibleShips(currentLength + 1);

            for (byte i = 0; i < currentLength; i++)
            {
                if (vertical)
                {
                    result[x, y + i] &= state;
                    result[x, y + i] |= FieldState.Hit;
                }
                else
                {
                    result[x + i, y] &= state;
                    result[x + i, y] |= FieldState.Hit;
                }
            }

            if (vertical)
            {
                if (x > 0)
                {
                    result[x - 1, y] &= state;
                }

                if (x < 9)
                {
                    result[x + 1, y] &= state;
                }
            }
            else
            {
                if (y > 0)
                {
                    result[x, y - 1] &= state;
                }

                if (y < 9)
                {
                    result[x, y + 1] &= state;
                }
            }
        }

        private static bool ParseHit(char hitType, string template, FieldState[,] result, byte i, out bool vertical, out int currentLength, out int x, out int y)
        {
            vertical = false;
            currentLength = 1;
            x = i % 10;
            y = i / 10;

            if (result[x, y] == FieldState.None || ((result[x, y] & FieldState.Hit) == FieldState.Hit))
            {
                return false;
            }

            if (y > 0)
            {
                if (x > 0)
                {
                    result[x - 1, y - 1] = FieldState.None;
                }

                if (x < 9 && y > 0)
                {
                    result[x + 1, y - 1] = FieldState.None;
                }
            }
            if (y < 9)
            {
                if (x > 0)
                {
                    result[x - 1, y + 1] = FieldState.None;
                }
                if (x < 9)
                {
                    result[x + 1, y + 1] = FieldState.None;
                }
            }

            if (x < 9 && template[i + 1] == hitType)
            {
                currentLength = 2;
                for (byte l = 2; l <= 9 - x; l++)
                {
                    if (template[i + l] == hitType)
                    {
                        currentLength = l + 1;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else if (y < 9 && template[i + 10] == hitType)
            {
                currentLength = 2;
                vertical = true;

                for (byte l = 2; l <= 9 - y; l++)
                {
                    if (template[i + l * 10] == hitType)
                    {
                        currentLength = l + 1;
                    }
                    else
                    {
                        break;
                    }
                }

            }

            return true;
        }

        private static FieldState Sink(FieldState[,] result, int x, int y, int length, bool vertical)
        {
            FieldState shipToSink;
            switch (length)
            {
                case 2:
                    shipToSink = FieldState.Destroyer;
                    break;

                case 3:
                    shipToSink = (result[x, y] & FieldState.Submarine) == FieldState.Submarine ? FieldState.Submarine : FieldState.Cruiser;
                    break;
                case 4:
                    shipToSink = FieldState.Battleship;
                    break;
                case 5:
                    shipToSink = FieldState.Carrier;
                    break;
                default:
                    throw new NotSupportedException("This should not happen!");
            }

            var startx = x > 0 ? x - 1 : x;
            var starty = y > 0 ? y - 1 : y;
            var endx = x;
            var endy = y;

            if (vertical)
            {
                endx = x < 9 ? x + 1 : x;
                endy = y + length < 9 ? y + length : 9;
            }
            else
            {
                endx = x + length < 9 ? x + length : 9;
                endy = y < 9 ? y + 1 : y;
            }

            for (int currentx = startx; currentx <= endx; currentx++)
            {
                for (int currenty = starty; currenty <= endy; currenty++)
                {
                    result[currentx, currenty] = FieldState.None;
                }
            }

            for (int currentx = 0; currentx < 10; currentx++)
            {
                for (int currenty = 0; currenty < 10; currenty++)
                {
                    result[currentx, currenty] &= ~shipToSink;
                }
            }


            return shipToSink;
        }
    }
}
