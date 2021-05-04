using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Codeworx.Battleship.Player
{
    [Flags]
    public enum FieldOption : ushort
    {
        None = 0x0000,
        TwoHorizontal = 0x0001,
        TwoVertical = 0x0002,
        ThreeHorizontal = 0x0004,
        ThreeVertical = 0x0008,
        FourHorizontal = 0x0010,
        FourVertical = 0x0020,
        FiveHorizontal = 0x0040,
        FiveVertical = 0x0080,
        Passive = 0x0100,
        Ship = 0x0200,
        All = TwoHorizontal | TwoVertical | ThreeHorizontal | ThreeVertical | FourHorizontal | FourVertical | FiveHorizontal | FiveVertical,
    }
}
