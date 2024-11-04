using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UC_MapPainter
{
    ///////////////////////////////////////////////
    //////          NIGHT COLOUR STRUCT         ///
    //////  [1b - Sky red component            ] ///
    //////  [1b - Sky green component          ] ///
    //////  [1b - Sky blue component           ] ///
    ///////////////////////////////////////////////
    public struct LightNightColour
    {
        public byte Red { get; set; }      // UBYTE (1 byte) - Sky red component
        public byte Green { get; set; }    // UBYTE (1 byte) - Sky green component
        public byte Blue { get; set; }     // UBYTE (1 byte) - Sky blue component
    }
}
