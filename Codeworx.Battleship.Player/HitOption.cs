using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Codeworx.Battleship.Player
{
    public class HitOption
    {

        public HitOption(int x, int y, int length, bool vertical, ImmutableDictionary<FieldState, ImmutableDictionary<bool, int[]>> possibleResolutions)
        {
            X = x;
            Y = y;
            Vertical = vertical;
            Length = length;
            PossibleResolutions = possibleResolutions;
        }

        public int X { get; }

        public int Y { get; }

        public bool Vertical { get; }

        public int Length { get; }

        public ImmutableDictionary<FieldState, ImmutableDictionary<bool, int[]>> PossibleResolutions { get; }

        public ImmutableList<FlattenedResolution> GetFlattened()
        {
            var query = from p in PossibleResolutions
                        from d in p.Value
                        from s in d.Value
                        select new FlattenedResolution(p.Key, d.Key, s);

            return query.ToImmutableList();
        }

        public class FlattenedResolution
        {
            public FlattenedResolution(FieldState state, bool vertical, int start)
            {
                State = state;
                Vertical = vertical;
                Start = start;
            }

            public FieldState State { get; }
            public bool Vertical { get; }
            public int Start { get; }
        }
    }
}