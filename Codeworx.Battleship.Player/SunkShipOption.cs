namespace Codeworx.Battleship.Player
{
    public class SunkShipOption
    {
        public SunkShipOption(int x, int y, FieldState ship, bool vertical)
        {
            X = x;
            Y = y;
            Ship = ship;
            Vertical = vertical;
        }

        public int X { get; }
        public int Y { get; }
        public FieldState Ship { get; }
        public bool Vertical { get; }

        public override int GetHashCode()
        {
            return X ^ Y ^ (int)Ship ^ Vertical.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var compare = (SunkShipOption)obj;

            return compare.X.Equals(X) &&
                compare.Y.Equals(Y) &&
                compare.Ship.Equals(Ship) &&
                compare.Vertical.Equals(Vertical);
        }
    }
}