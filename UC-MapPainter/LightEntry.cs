using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UC_MapPainter
{
    //////////////////////////////////////////////////////////
    //////            LIGHT STRUCTURE                      ///
    //////  [1b - Range (0-255)][3b RGB ( -127 - 127) ]    ///
    //////  [1b - Next (Index of Next Free Light)     ]    ///
    //////  [1b - Used (Flag for if slot is used)     ]    ///
    //////  [1b - Flag (Bitflag properties)           ]    ///
    //////  [1b - Padding Used for memory alignment   ]    ///
    //////  [1b - BitFlag - Various Props             ]    ///
    //////  [4b - X-Pos  ][4b - Y-Pos  ][4b - Z-Poz   ]    ///
    //////////////////////////////////////////////////////////
    public class LightEntry
    {
        public byte Range { get; set; } // UBYTE (1 byte)
        public sbyte Red { get; set; } // SBYTE (1 byte)
        public sbyte Green { get; set; } // SBYTE (1 byte)
        public sbyte Blue { get; set; } // SBYTE (1 byte)
        public byte Next { get; set; } // UBYTE (1 byte)
        public byte Used { get; set; } // UBYTE (1 byte)
        public byte Flags { get; set; } // UBYTE (1 byte)
        public byte Padding { get; set; } // UBYTE (1 byte)
        public int X { get; set; } // SLONG (4 bytes)
        public int Y { get; set; } // SLONG (4 bytes)
        public int Z { get; set; } // SLONG (4 bytes)
    }
}
