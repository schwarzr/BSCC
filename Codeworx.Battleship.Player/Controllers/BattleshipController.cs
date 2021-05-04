using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using NBattleshipCodingContest.Logic;

namespace Codeworx.Battleship.Player.Controllers
{
    [Route("api/[controller]")]
    public class BattleshipController : Controller
    {
        private static int _callcount;
        private static ConcurrentDictionary<Guid, (int h, int v)> _shipcount = new ConcurrentDictionary<Guid, (int h, int v)>();
        private static List<BoardState> _statistics = new List<BoardState>();
        private static int[,] _alreadyHit = new int[10, 10];

        [HttpGet("getReady")]

        public IActionResult GetReady()
        {
            _shipcount = new ConcurrentDictionary<Guid, (int h, int v)>();
            return Ok();
        }

        [HttpGet("reset-training")]
        public IActionResult ResetTraining()
        {
            System.IO.File.Delete("battleship.data");
            return Ok();
        }

        [HttpGet("training")]
        public IActionResult Training()
        {
            var buffer = System.IO.File.ReadAllBytes("battleship.data");
            return new FileContentResult(buffer, "application/octet-stream");
        }

        [HttpPost("finished")]
        public IActionResult Finished([FromBody] BattleshipRequest[] games)
        {
            _alreadyHit = new int[10, 10];
            int vertical = 0;
            int horizontal = 0;

            ////using (var fs = new FileStream(@"battleship.data", FileMode.OpenOrCreate, FileAccess.Write))
            ////{
            ////    fs.Seek(0, SeekOrigin.End);
            ////    foreach (var game in games)
            ////    {
            ////        var buffer = ConvertGame(game.Board);
            ////        fs.Write(buffer, 0, 13);
            ////    }
            ////}

            //_statistics.AddRange(games.Select(p => FieldStateParser.Parse(p.Board)).ToList());
            //PrintDistribution(_statistics);

            ////vertical = parsed.Sum(p => p.SunkenShips.Count(p => p.Vertical));
            ////horizontal = parsed.Sum(p => p.SunkenShips.Count(p => !p.Vertical));

            ////var uniqueShips = (from p in parsed
            ////                   from s in p.SunkenShips
            ////                   group s by s into tmp
            ////                   select new { Ship = tmp.Key, Count = tmp.Count() }).ToList();

            //foreach (var item in games)
            //{
            //    var parsed = FieldStateParser.Parse(item.Board);
            //    var board = GetBoard(item.Board);
            //    var currentVertical = parsed.SunkenShips.Count(p => p.Vertical);
            //    var currentHorizontal = parsed.SunkenShips.Count(p => !p.Vertical);

            //    vertical += currentVertical;
            //    horizontal += currentHorizontal;
            //}

            //Console.WriteLine($"{vertical} Vertical Ships and {horizontal} Horizontal Ships.");

            return Ok();
        }

        private byte[] ConvertGame(string board)
        {
            var result = new byte[100];
            for (int i = 0; i < 100; i++)
            {
                if (board[i] == 'X')
                {
                    var bit = i % 8;
                    byte flag = (byte)(0x80 >> bit);
                    result[i / 8] |= flag;
                }
            }

            return result;
        }

        private void PrintDistribution(List<BoardState> parsed)
        {
            var data = new int[10, 10];

            foreach (var item in parsed)
            {
                foreach (var ship in item.SunkenShips)
                {
                    var length = FieldStateParser.StateLength[ship.Ship];
                    for (int i = 0; i < length; i++)
                    {
                        if (ship.Vertical)
                        {
                            data[ship.X, ship.Y + i]++;
                        }
                        else
                        {
                            data[ship.X + 1, ship.Y]++;
                        }
                    }
                }
            }

            var count = parsed.Count * 5.0m;

            Console.WriteLine("|--|--|--|--|--|--|--|--|--|--|");
            for (int y = 0; y < 10; y++)
            {
                Console.Write("|");
                for (int x = 0; x < 10; x++)
                {
                    var value = (data[x, y] / count) * 100;

                    Console.Write($"{value:00}|");
                }
                Console.WriteLine();
                Console.WriteLine("|--|--|--|--|--|--|--|--|--|--|");
            }
        }

        [HttpPost("getShots")]
        public async Task<BoardIndex[]> GetShotsAsync([FromBody] BattleshipRequest[] shotRequests)
        {
            //BattlefieldGenerator.data = System.IO.File.ReadAllBytes(@"C:\Temp\battleship.data");

            //Console.WriteLine($"Call: {++_callcount}");

            var second = new string(' ', 100).ToCharArray();
            second[54] = 'W';
            var secondBoard = new string(second);

            // Create a helper variable that will receive our calculated
            // shots for each shot request.
            var shots = new BoardIndex[shotRequests.Length];

            ////foreach (var item in shotRequests)
            ////{
            ////    if (item.LastShot != null)
            ////    {
            ////        BoardIndex index = item.LastShot;
            ////        if (item.Board[index.Row * 10 + index.Column] == 'H' || item.Board[index.Row * 10 + index.Column] == 'X')
            ////        {
            ////            _alreadyHit[index.Column, index.Row]++;
            ////        }
            ////    }
            ////}


            var requests = shotRequests.Select((p, i) => new { Index = i, Request = p })
                                .GroupBy(p => p.Request.Board)
                                .Select(p => new { Board = p.Key, Requests = p.ToList() })
                                //.Select(p => new { Board = p.Request.Board, Requests = new[] { p } })
                                .ToList();

            ////foreach (var item in shotRequests)
            ////{
            ////    var parsed = FieldStateParser.Parse(item.Board);
            ////    var board = GetBoard(item.Board);
            ////    var currentVertical = parsed.SunkenShips.Count(p => p.Vertical);
            ////    var currentHorizontal = parsed.SunkenShips.Count(p => !p.Vertical);

            ////    _shipcount.AddOrUpdate(item.GameId, (currentHorizontal, currentVertical), (p, q) => (currentHorizontal, currentVertical));
            ////}

            ////Console.WriteLine($"{_shipcount.Sum(p => p.Value.v)} Vertical Ships and {_shipcount.Sum(p => p.Value.h)} Horizontal Ships.");

            ////if (requests.Count == 1 && requests[0].Board == new string(' ', 100))
            ////{
            ////    for (int i = 0; i < shots.Length; i++)
            ////    {
            ////        shots[i] = "E6";
            ////    }
            ////    return shots;
            ////}

            Parallel.ForEach(requests, (shotRequest) =>
            {
                //if (shotRequest.Board == secondBoard)
                //{
                //    foreach (var item in shotRequest.Requests)
                //    {
                //        shots[item.Index] = "F5";
                //    }
                //    return;
                //}

                //Console.WriteLine(GetBoard(shotRequest.Board));

                var generator = new BattlefieldGenerator(FieldStateParser.Parse(shotRequest.Board), _alreadyHit);
                generator.Simulate();
                var nextShot = generator.Next();
                foreach (var item in shotRequest.Requests)
                {
                    shots[item.Index] = nextShot;
                }
            });

            return shots;
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
    }
}
