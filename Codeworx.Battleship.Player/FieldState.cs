using System;

namespace Codeworx.Battleship.Player
{
    [Flags]
    public enum FieldState : byte
    {
        None = 0x00,
        Destroyer = 0x01,
        Submarine = 0x02,
        Cruiser = 0x04,
        Battleship = 0x08,
        Carrier = 0x10,
        Hit = 0x20,
        All = Destroyer | Submarine | Cruiser | Battleship | Carrier
    }
}