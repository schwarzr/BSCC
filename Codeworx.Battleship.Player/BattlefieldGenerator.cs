using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codeworx.Battleship.Generator;
using Codeworx.Battleship.Player.Extensions;
using Codeworx.Battleship.Player.Strategy;
using NBattleshipCodingContest.Logic;

namespace Codeworx.Battleship.Player
{
    public class BattlefieldGenerator
    {

        private readonly Random _random;
        private readonly BoardState _state;
        private readonly int[,] _alreadyHit;
        private readonly int[,] _scoreboard;

        public static byte[] data;// = System.IO.File.ReadAllBytes(@"c:\Temp\battleship.data");
        private const int HISTORYLIMIT = 10;
        private const int MINHITS = 500;
        private int _retryCount;

        public BattlefieldGenerator(BoardState state, int[,] alreadyHit)
        {
            _random = new Random();
            _state = state;
            _alreadyHit = alreadyHit;
            _scoreboard = new int[10, 10];

            ////bool check = true;
            ////while (check)
            ////{
            ////    var next = _random.Next(0, 100);
            ////    var x = next % 10;
            ////    var y = next / 10;

            ////    if (_state.Template[x, y] != FieldState.None)
            ////    {
            ////        check = false;
            ////    }
            ////}
        }

        public void Simulate()
        {
            var strategy = new MlStrategy(_state);

            var strategyOutput = strategy.Next();

            if (strategyOutput != 666)
            //if (_state.Shot < HISTORYLIMIT && SimulateByHistoricData())
            {
                //var next = Next();
                //if (next != $"{_lookup[strategyOutput % 10]}{(strategyOutput / 10) + 1}")
                //{
                //    Console.WriteLine("shit!!!");
                //}

                _scoreboard[strategyOutput % 10, strategyOutput / 10]++;
                // do nothing
            }
            else
            {

                if (_state.HitOptions.Any())
                {
                    SimulateHits();
                    //SimulateHits2();
                }
                else
                {
                    CalculcateDensity();

                    //if (_state.RemainingShips.Length == 2)// || _state.RemainingShips.Length == 3)
                    //{
                    //    SimulateRandom();
                    //}
                    //else
                    //{
                    //    CalculcateDensity();
                    //}
                }
            }
        }

        private bool SimulateByHistoricData()
        {
            var hits = new Dictionary<int, Shot>();


            foreach (var item in _state.SunkenShips)
            {
                var length = FieldStateParser.StateLength[item.Ship];
                for (int i = 0; i < length; i++)
                {
                    if (item.Vertical)
                    {
                        hits.Add((item.Y + i) * 10 + item.X, Shot.Hit);
                    }
                    else
                    {
                        hits.Add(item.Y * 10 + (item.X + i), Shot.Hit);
                    }
                }
            }

            var filter = new List<int>();

            foreach (var item in _state.HitOptions)
            {
                if (item.Vertical || item.Length == 1)
                {
                    if (item.Y > 0)
                    {
                        filter.Add((item.Y - 1) * 10 + item.X);
                    }

                    if (item.Y + item.Length <= 9)
                    {
                        filter.Add((item.Y + item.Length) * 10 + item.X);
                    }
                }

                if (!item.Vertical || item.Length == 1)
                {
                    if (item.X > 0)
                    {
                        filter.Add(item.Y * 10 + (item.X - 1));
                    }

                    if (item.X + item.Length <= 9)
                    {
                        filter.Add(item.Y * 10 + (item.X + item.Length));
                    }
                }

                if (item.Vertical)
                {
                    for (int i = 0; i < item.Length; i++)
                    {
                        hits.Add((item.Y + i) * 10 + item.X, Shot.Hit);
                    }
                }
                else
                {
                    for (int i = 0; i < item.Length; i++)
                    {
                        hits.Add(item.Y * 10 + (item.X + i), Shot.Hit);
                    }
                }
            }

            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    if (hits.ContainsKey(y * 10 + x))
                    {
                        continue;
                    }

                    if (_state.Template[x, y] == FieldState.None)
                    {
                        hits.Add(y * 10 + x, Shot.Water);
                    }
                }
            }


            var count = GetStatistics(hits, data, out var totalHits);

            if (totalHits < MINHITS)
            {
                return false;
            }

            for (int i = 0; i < 100; i++)
            {
                if (filter.Count == 0 || filter.Contains(i))
                {
                    _scoreboard[i % 10, i / 10] = count[i];
                }
            }

            return true;
        }

        private void CalculcateDensity()
        {
            var data = new FieldOption[10, 10];
            var remaining = _state.RemainingShips.Select(p => FieldStateParser.StateLength[p]).ToList();

            //remaining = new List<int>() { remaining.Max() };

            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    if (_state.Template[x, y] != FieldState.None)
                    {
                        data[x, y] = FieldOption.All;
                    }
                }
            }
            CalculcateDensity(data, remaining);
        }

        private void CalculcateDensity(FieldOption[,] data, IEnumerable<int> remaining)
        {

            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    FieldOption result = FieldOption.None;

                    foreach (var item in remaining)
                    {
                        if (x + item <= 10)
                        {
                            var skip = false;
                            for (int i = 0; i < item; i++)
                            {
                                if (data[x + i, y] == FieldOption.None)
                                {
                                    skip = true;
                                    break;
                                }
                            }

                            if (!skip)
                            {
                                for (int i = 0; i < item; i++)
                                {
                                    _scoreboard[x + i, y]++;
                                }
                            }
                        }
                        if (y + item <= 10)
                        {
                            var skip = false;
                            for (int i = 0; i < item; i++)
                            {
                                if (data[x, y + i] == FieldOption.None)
                                {
                                    skip = true;
                                    break;
                                }
                            }

                            if (!skip)
                            {
                                for (int i = 0; i < item; i++)
                                {
                                    _scoreboard[x, y + i]++;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void SimulateRandom()
        {
            var template = new FieldOption[10, 10];
            var remainingSizesTemplate = _state.RemainingShips.Select(p => FieldStateParser.StateLength[p]).Distinct().ToList();

            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    if (_state.Template[x, y] != FieldState.None)
                    {
                        template[x, y] = FieldOption.All;
                    }
                }
            }

            SetFlags(template, remainingSizesTemplate);

            var missingShips = _state.RemainingShips.OrderBy(p => _random.Next()).ToList();

            int runCount;

            switch (missingShips.Count)
            {
                case 4:
                    runCount = 10;
                    break;
                case 3:
                    runCount = 150;
                    break;
                case 2:
                    runCount = 1000;
                    break;
                case 1:
                    runCount = 1000;
                    break;
                default:
                    runCount = 10;
                    break;
            }

            ProcessShip(template, missingShips, runCount);


            //for (int run = 0; run < 1000; run++)
            //{
            //    var board = new string(' ', 100).ToCharArray();

            //    var remaining = _state.RemainingShips.OrderByDescending(p => _random.Next()).ToList();
            //    var remainingSizes = remainingSizesTemplate.ToList();
            //    var currentRun = new FieldOption[10, 10];
            //    var toAdd = new List<(int x, int y)>();

            //    Array.Copy(template, currentRun, 100);

            //    while (remaining.Any())
            //    {
            //        var currentShip = remaining[0];

            //        var available = GetAvailable(currentRun);
            //        var list = available[FieldStateParser.StateLength[currentShip]];
            //        if (list.Count == 0)
            //        {
            //            _retryCount++;

            //            if (_retryCount > 990)
            //            {
            //                //Console.WriteLine("ui");
            //            }
            //            break;
            //        }

            //        var next = list.Pick(_random);

            //        var x = next.x;
            //        var y = next.y;

            //        if (currentRun[x, y] != FieldOption.None)
            //        {
            //            for (int i = 0; i < next.size; i++)
            //            {
            //                int setx, sety;
            //                if (next.vertical)
            //                {
            //                    setx = x;
            //                    sety = y + i;
            //                }
            //                else
            //                {
            //                    setx = x + i;
            //                    sety = y;
            //                }

            //                toAdd.Add((setx, sety));
            //            }

            //            var minX = x > 0 ? x - 1 : 0;
            //            var minY = y > 0 ? y - 1 : 0;
            //            int maxX, maxY;

            //            if (next.vertical)
            //            {
            //                maxX = x < 9 ? x + 1 : 9;
            //                maxY = y + next.size < 9 ? y + next.size : 9;
            //            }
            //            else
            //            {
            //                maxX = x + next.size < 9 ? x + next.size : 9;
            //                maxY = y < 9 ? y + 1 : 9;
            //            }

            //            for (int setX = minX; setX <= maxX; setX++)
            //            {
            //                for (int setY = minY; setY <= maxY; setY++)
            //                {
            //                    currentRun[setX, setY] = FieldOption.None;
            //                }
            //            }

            //            remaining.Remove(currentShip);
            //            if (next.size == 3 && (remaining.Contains(FieldState.Submarine) || remaining.Contains(FieldState.Cruiser)))
            //            {
            //                // do nothing    
            //            }
            //            else
            //            {
            //                remainingSizes.Remove(next.size);
            //            }

            //            if (remaining.Count > 0)
            //            {
            //                SetFlags(currentRun, remainingSizes);
            //            }
            //            else
            //            {
            //                foreach (var item in toAdd)
            //                {
            //                    board[item.y * 10 + item.x] = 'X';

            //                    var count = ++_scoreboard[item.x, item.y];

            //                    if (count > _highscore)
            //                    {
            //                        _highscore = count;
            //                        _nextX = item.x;
            //                        _nextY = item.y;
            //                    }
            //                }

            //                //Console.WriteLine(GetBoard(new String(board)));
            //            }
            //        }
            //    }
            //}

            //if (_retryCount > 0)
            //{
            //    Console.WriteLine($"RetryCount: {_retryCount}");
            //}
        }

        private void ProcessShip(FieldOption[,] template, List<FieldState> missingShips, int runCount)
        {
            var nextShip = missingShips[0];
            var nextRemaining = missingShips.GetRange(1, missingShips.Count - 1);
            var nextSizes = nextRemaining.Select(p => FieldStateParser.StateLength[p]).Distinct().ToList();

            var available = GetAvailable(template);
            var size = FieldStateParser.StateLength[nextShip];
            var options = available[size];

            if (options.Count == 0)
            {
                // TODO shit!!!
                _retryCount++;
                return;
            }

            var collection = options.Pick(_random, runCount).ToList();
            //Parallel.ForEach(collection, next =>
            foreach (var next in collection)
            {
                var x = next.x;
                var y = next.y;
                FieldOption[,] currentRun = new FieldOption[10, 10];
                Array.Copy(template, currentRun, 100);

                for (int i = 0; i < next.size; i++)
                {
                    int setx, sety;
                    if (next.vertical)
                    {
                        setx = x;
                        sety = y + i;
                    }
                    else
                    {
                        setx = x + i;
                        sety = y;
                    }

                    currentRun[setx, sety] = FieldOption.Ship;
                }

                var minX = x > 0 ? x - 1 : 0;
                var minY = y > 0 ? y - 1 : 0;
                int maxX, maxY;

                if (next.vertical)
                {
                    maxX = x < 9 ? x + 1 : 9;
                    maxY = y + next.size < 9 ? y + next.size : 9;
                }
                else
                {
                    maxX = x + next.size < 9 ? x + next.size : 9;
                    maxY = y < 9 ? y + 1 : 9;
                }

                for (int setX = minX; setX <= maxX; setX++)
                {
                    for (int setY = minY; setY <= maxY; setY++)
                    {
                        if (currentRun[setX, setY] != FieldOption.Ship)
                        {
                            currentRun[setX, setY] = FieldOption.None;
                        }
                    }
                }

                if (nextRemaining.Count > 0)
                {
                    SetFlags(currentRun, nextSizes);
                    ProcessShip(currentRun, nextRemaining, runCount);
                }
                else
                {
                    Score(currentRun);
                }
            }//);
        }

        private void Score(FieldOption[,] currentRun)
        {
            //var board = new string(' ', 100).ToCharArray();

            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    if (currentRun[x, y] == FieldOption.Ship)
                    {
                        _scoreboard[x, y]++;
                        //board[y * 10 + x] = 'X';
                    }
                }
            }

            //Console.WriteLine(GetBoard(new string(board)));
        }

        private BoardContent GetBoard(string board)
        {
            var result = new BoardContent();
            for (var i = 0; i < 100; i++)
            {
                result[new BoardIndex(i)] = BoardContentJsonConverter.CharToSquareContent(board[i]);
            }

            return result;

        }

        private Dictionary<int, List<(int x, int y, int size, bool vertical)>> GetAvailable(FieldOption[,] currentRun)
        {
            var result = new Dictionary<int, List<(int x, int y, int size, bool vertical)>>()
            {
                { 2, new List<(int x, int y, int size, bool vertical)>() },
                { 3, new List<(int x, int y, int size, bool vertical)>() },
                { 4, new List<(int x, int y, int size, bool vertical)>() },
                { 5, new List<(int x, int y, int size, bool vertical)>() },
            };

            for (int y = 0; y < 10; y++)
                for (int x = 0; x < 10; x++)
                {
                    {
                        if (currentRun[x, y] != FieldOption.None)
                        {
                            if ((currentRun[x, y] & FieldOption.FiveHorizontal) == FieldOption.FiveHorizontal)
                            {
                                result[5].Add((x, y, 5, false));
                            }

                            if ((currentRun[x, y] & FieldOption.FiveVertical) == FieldOption.FiveVertical)
                            {
                                result[5].Add((x, y, 5, true));
                            }

                            if ((currentRun[x, y] & FieldOption.FourHorizontal) == FieldOption.FourHorizontal)
                            {
                                result[4].Add((x, y, 4, false));
                            }

                            if ((currentRun[x, y] & FieldOption.FourVertical) == FieldOption.FourVertical)
                            {
                                result[4].Add((x, y, 4, true));
                            }

                            if ((currentRun[x, y] & FieldOption.ThreeHorizontal) == FieldOption.ThreeHorizontal)
                            {
                                result[3].Add((x, y, 3, false));
                            }

                            if ((currentRun[x, y] & FieldOption.ThreeVertical) == FieldOption.ThreeVertical)
                            {
                                result[3].Add((x, y, 3, true));
                            }

                            if ((currentRun[x, y] & FieldOption.TwoHorizontal) == FieldOption.TwoHorizontal)
                            {
                                result[2].Add((x, y, 2, false));
                            }

                            if ((currentRun[x, y] & FieldOption.TwoVertical) == FieldOption.TwoVertical)
                            {
                                result[2].Add((x, y, 2, true));
                            }

                        }
                    }
                }

            return result;
        }

        private List<(FieldState state, bool vertical, int size)> GetOptions(FieldOption fieldOption, List<FieldState> remaining)
        {
            var result = new List<(FieldState, bool, int)>();

            foreach (var item in remaining)
            {
                var size = FieldStateParser.StateLength[item];
                switch (size)
                {
                    case 2:
                        if ((fieldOption & FieldOption.TwoHorizontal) == FieldOption.TwoHorizontal)
                        {
                            result.Add((item, false, size));
                        }
                        if ((fieldOption & FieldOption.TwoVertical) == FieldOption.TwoVertical)
                        {
                            result.Add((item, true, size));
                        }
                        break;
                    case 3:
                        if ((fieldOption & FieldOption.ThreeHorizontal) == FieldOption.ThreeHorizontal)
                        {
                            result.Add((item, false, size));
                        }
                        if ((fieldOption & FieldOption.ThreeVertical) == FieldOption.ThreeVertical)
                        {
                            result.Add((item, true, size));
                        }
                        break;
                    case 4:
                        if ((fieldOption & FieldOption.FourHorizontal) == FieldOption.FourHorizontal)
                        {
                            result.Add((item, false, size));
                        }
                        if ((fieldOption & FieldOption.FourVertical) == FieldOption.FourVertical)
                        {
                            result.Add((item, true, size));
                        }
                        break;
                    case 5:
                        if ((fieldOption & FieldOption.FiveHorizontal) == FieldOption.FiveHorizontal)
                        {
                            result.Add((item, false, size));
                        }
                        if ((fieldOption & FieldOption.FiveVertical) == FieldOption.FiveVertical)
                        {
                            result.Add((item, true, size));
                        }
                        break;
                }
                if (result.Any())
                {
                    break;
                }
            }

            return result;
        }

        private static void SetFlags(FieldOption[,] data, IEnumerable<int> remaining)
        {
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    FieldOption result = FieldOption.None;

                    foreach (var item in remaining)
                    {
                        if (x + item <= 10)
                        {
                            var skip = false;
                            for (int i = 0; i < item; i++)
                            {
                                if (data[x + i, y] == FieldOption.None || data[x + i, y] == FieldOption.Ship)
                                {
                                    skip = true;
                                    break;
                                }
                            }

                            if (!skip)
                            {
                                for (int i = 0; i < item; i++)
                                {
                                    data[x + i, y] |= FieldOption.Passive;
                                }

                                switch (item)
                                {
                                    case 2:
                                        result |= FieldOption.TwoHorizontal;
                                        break;
                                    case 3:
                                        result |= FieldOption.ThreeHorizontal;
                                        break;
                                    case 4:
                                        result |= FieldOption.FourHorizontal;
                                        break;
                                    case 5:
                                        result |= FieldOption.FiveHorizontal;
                                        break;
                                }
                            }
                        }
                        if (y + item <= 10)
                        {
                            var skip = false;
                            for (int i = 0; i < item; i++)
                            {
                                if (data[x, y + i] == FieldOption.None || data[x, y + i] == FieldOption.Ship)
                                {
                                    skip = true;
                                    break;
                                }
                            }

                            if (!skip)
                            {
                                for (int i = 0; i < item; i++)
                                {
                                    data[x, y + i] |= FieldOption.Passive;
                                }

                                switch (item)
                                {
                                    case 2:
                                        result |= FieldOption.TwoVertical;
                                        break;
                                    case 3:
                                        result |= FieldOption.ThreeVertical;
                                        break;
                                    case 4:
                                        result |= FieldOption.FourVertical;
                                        break;
                                    case 5:
                                        result |= FieldOption.FiveVertical;
                                        break;
                                }
                            }
                        }
                    }

                    if (result == FieldOption.None)
                    {
                        data[x, y] &= (FieldOption.Passive | FieldOption.Ship);
                    }
                    else
                    {
                        data[x, y] = result;
                    }
                }
            }
        }

        private void SimulateHits2()
        {
            var data = new FieldOption[10, 10];
            var remaining = _state.RemainingShips.Select(p => FieldStateParser.StateLength[p]).Where(p => _state.HitOptions.Any(x => p > x.Length)).ToList();

            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    if (_state.Template[x, y] != FieldState.None)
                    {
                        data[x, y] = FieldOption.All;
                    }
                }
            }

            foreach (var item in _state.HitOptions)
            {
                if (item.Vertical)
                {
                    for (int y = item.Y; y < item.Y + item.Length; y++)
                    {
                        data[item.X, y] = FieldOption.All;
                    }
                }
                else
                {
                    for (int x = item.X; x < item.X + item.Length; x++)
                    {
                        data[x, item.Y] = FieldOption.All;
                    }
                }
            }

            CalculcateDensity(data, remaining);

            var temp = new int[10, 10];

            foreach (var item in _state.HitOptions)
            {
                bool checkVertical = false;
                bool checkHorizontal = false;

                if (item.Length == 1)
                {
                    checkVertical = true;
                    checkHorizontal = true;
                }
                else if (item.Vertical)
                {
                    checkVertical = true;
                }
                else
                {
                    checkHorizontal = true;
                }

                if (checkHorizontal)
                {
                    if (item.X > 0)
                    {
                        temp[item.X - 1, item.Y] = _scoreboard[item.X - 1, item.Y];
                    }
                    if (item.X + item.Length <= 9)
                    {
                        temp[item.X + item.Length, item.Y] = _scoreboard[item.X + item.Length, item.Y];
                    }
                }

                if (checkVertical)
                {
                    if (item.Y > 0)
                    {
                        temp[item.X, item.Y - 1] = _scoreboard[item.X, item.Y - 1];
                    }
                    if (item.Y + item.Length <= 9)
                    {
                        temp[item.X, item.Y + item.Length] = _scoreboard[item.X, item.Y + item.Length];
                    }
                }
            }

            Array.Copy(temp, _scoreboard, 100);
        }

        private void SimulateHits()
        {
            var orderd = _state.HitOptions.Select(p => new { Hit = p, Resolutions = p.GetFlattened() }).OrderBy(p => p.Resolutions.Count).ToList();

            var currentRun = new FieldState[10, 10];
            Array.Copy(_state.Template, currentRun, 100);

            foreach (var item in _state.HitOptions)
            {
                for (int i = 0; i < item.Length; i++)
                {
                    if (item.Vertical)
                    {
                        currentRun[item.X, item.Y + i] = FieldState.Hit;
                    }
                    else
                    {
                        currentRun[item.X + i, item.Y] = FieldState.Hit;
                    }
                }
            }

            foreach (var hit in _state.HitOptions)
            {
                foreach (var resolution in hit.GetFlattened())
                {
                    int startx, starty;

                    if (resolution.Vertical)
                    {
                        startx = hit.X;
                        starty = resolution.Start;
                    }
                    else
                    {
                        startx = resolution.Start;
                        starty = hit.Y;
                    }

                    var length = FieldStateParser.StateLength[resolution.State];

                    for (int i = 0; i < length; i++)
                    {
                        var x = startx;
                        var y = starty;
                        bool doubleScore = false;
                        if (resolution.Vertical)
                        {
                            y += i;
                            doubleScore = y == hit.Y - 1 || y == hit.Y + hit.Length;
                        }
                        else
                        {
                            x += i;
                            doubleScore = x == hit.X - 1 || x == hit.X + hit.Length;
                        }

                        if ((currentRun[x, y] & FieldState.Hit) == FieldState.Hit)
                        {
                            continue;
                        }

                        ++_scoreboard[x, y];
                        if (doubleScore)
                        {
                            ++_scoreboard[x, y];
                        }
                    }
                }
            }
        }

        private static int[] GetStatistics(Dictionary<int, Shot> hits, byte[] data, out int totalHits)
        {
            var count = new int[100];
            var gameCount = data.Length / 13;
            totalHits = 0;

            var size = 0;

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
                    var buffer = data[start + (i / 8)];
                    var mask = 0x80 >> (i % 8);
                    if ((buffer & mask) != 0)
                    {
                        count[i]++;
                    }
                }
            }

            foreach (var item in hits.Keys)
            {
                count[item] = 0;
            }

            return count;
        }


        private static void RemoveShip(FieldState[,] currentRun, FieldState ship)
        {
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    currentRun[x, y] &= ~ship;
                }
            }
        }

        public string Next()
        {
            int highscore = int.MinValue;
            var highScorer = new List<(int x, int y)>();
            int nextX = 0, nextY = 0;
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    if (_scoreboard[x, y] > highscore)
                    {
                        highscore = _scoreboard[x, y];
                    }
                }
            }
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    if (_scoreboard[x, y] == highscore)
                    {
                        highScorer.Add((x, y));
                    }
                }
            }

            ////if (highScorer.Count > 1)
            ////{
            ////    var tempScore = int.MaxValue;

            ////    foreach (var item in highScorer)
            ////    {
            ////        if (_alreadyHit[item.x, item.y] < tempScore)
            ////        {
            ////            tempScore = _alreadyHit[item.x, item.y];
            ////            nextX = item.x;
            ////            nextY = item.y;
            ////        }

            ////        //if ((item.y * 10 + item.x) % 2 == 1)
            ////        //{
            ////        //    return $"{_lookup[item.x]}{item.y + 1}";
            ////        //}
            ////    }

            ////    return $"{_lookup[nextX]}{nextY + 1}";
            ////}

            //if (highScorer.Count > 1)
            //{
            //    foreach (var item in highScorer)
            //    {
            //        if ((item.y * 10 + item.x) % 2 == 0)
            //        {
            //            return $"{_lookup[item.x]}{item.y + 1}";
            //        }
            //    }
            //}

            //var pick = highScorer.Pick(_random);

            var pick = highScorer[0];

            //var pick = highScorer[highScorer.Count - 1];

            return $"{_lookup[pick.x]}{pick.y + 1}";
        }

        private const string _lookup = "ABCDEFGHIJ";
    }
}
