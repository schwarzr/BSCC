using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Codeworx.Battleship.Generator;

namespace Codeworx.Battleship.Player.Strategy
{
    public class MlStrategy : BattleshipStrategy
    {
        private readonly BoardState _state;

        private Dictionary<int, int> _shipLength;
        private Dictionary<int, int> _shipCurrentLength;

        private Dictionary<int, int> _sunk;

        private List<int> _hit;

        public MlStrategy(BoardState state)
        {
            _sunk = new Dictionary<int, int>();
            _hit = new List<int>();
            _shipLength = new Dictionary<int, int>();
            _shipCurrentLength = new Dictionary<int, int>();
            _state = state;
            int shipId = 0;
            foreach (var item in _state.SunkenShips)
            {
                int x, y;
                var length = FieldStateParser.StateLength[item.Ship];
                _shipLength.Add(shipId, length);
                _shipCurrentLength.Add(shipId, 0);

                for (int i = 0; i < length; i++)
                {
                    if (item.Vertical)
                    {
                        x = item.X;
                        y = item.Y + i;
                    }
                    else
                    {
                        x = item.X + i;
                        y = item.Y;
                    }
                    _sunk.Add(y * 10 + x, shipId);
                }

                shipId++;
            }

            foreach (var item in _state.HitOptions)
            {
                int x, y;
                for (int i = 0; i < item.Length; i++)
                {
                    if (item.Vertical)
                    {
                        x = item.X;
                        y = item.Y + i;
                    }
                    else
                    {
                        x = item.X + i;
                        y = item.Y;
                    }
                    _hit.Add(y * 10 + x);
                }
            }
        }

        public override int Fallback()
        {
            return 666;
        }

        public override CellState GetState(int cell)
        {
            if (_sunk.TryGetValue(cell, out var shipId))
            {
                if (++_shipCurrentLength[shipId] == _shipLength[shipId])
                {
                    return CellState.Sunk;
                }
                else
                {
                    return CellState.Hit;
                }
            }
            else if (_hit.Contains(cell))
            {
                return CellState.Hit;
            }
            else if (_state.Template[cell % 10, cell / 10] == FieldState.None)
            {
                return CellState.Water;
            }

            return CellState.None;
        }
    }
}
