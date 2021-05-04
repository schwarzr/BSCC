using System.Collections.Generic;
using System.Collections.Immutable;

namespace Codeworx.Battleship.Player
{
    public class BoardState
    {
        public BoardState(FieldState[,] template, IEnumerable<HitOption> hitOptions, IEnumerable<SunkShipOption> sunkenShips, FieldState[] remainingShips, int shot)
        {
            Template = template;
            Shot = shot;
            RemainingShips = remainingShips;
            HitOptions = hitOptions.ToImmutableList();
            SunkenShips = sunkenShips.ToImmutableList();
        }

        public FieldState[,] Template { get; }
        public FieldState[] RemainingShips { get; }
        public ImmutableList<HitOption> HitOptions { get; }

        public ImmutableList<SunkShipOption> SunkenShips { get; }
        public int Shot { get; }
    }
}