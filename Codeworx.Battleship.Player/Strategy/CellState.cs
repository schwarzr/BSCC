using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Codeworx.Battleship.Generator
{
    public enum CellState
    {
        None = 0x00,
        Water = 0x01,
        Hit = 0x02,
        Sunk = 0x03,
    }
}
