using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UC_MapPainter
{
    ///////////////////////////////////////////////
    //////            LIGHT HEADER             ///
    //////  [4b - Size of ED_Light struct     ] ///
    //////  [4b - Maximum number of lights    ] ///
    //////  [4b - Size of NIGHT_Colour struct ] ///
    ///////////////////////////////////////////////
    ///
    public class LightHeader
    {
        public int SizeOfEdLight { get; set; } // SLONG (4 bytes)
        public int EdMaxLights { get; set; } // SLONG (4 bytes)
        public int SizeOfNightColour { get; set; } // SLONG (4 bytes)

        public ushort SizeOfEdLightLower => (ushort)(SizeOfEdLight & 0xFFFF); // Lower 16 bits
        public ushort Version => (ushort)((SizeOfEdLight >> 16) & 0xFFFF); // Upper 16 bits
    }
}
