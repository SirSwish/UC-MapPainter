using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UC_MapPainter
{
    ///////////////////////////////////////////////
    //////         LIGHT PROPERTIES           ///
    //////  [4b - First free light index      ] ///
    //////  [4b - Night mode flags            ] ///
    //////  [4b - Ambient D3D color           ] ///
    //////  [4b - Ambient D3D specular color  ] ///
    //////  [4b - Ambient red component       ] ///
    //////  [4b - Ambient green component     ] ///
    //////  [4b - Ambient blue component      ] ///
    //////  [1b - Lamppost red component      ] ///
    //////  [1b - Lamppost green component    ] ///
    //////  [1b - Lamppost blue component     ] ///
    //////  [1b - Padding for alignment       ] ///
    //////  [4b - Lamppost radius             ] ///
    ///////////////////////////////////////////////
    public struct LightProperties
    {
        public int EdLightFree { get; set; }
        public uint NightFlag { get; set; }
        public uint NightAmbD3DColour { get; set; }
        public uint NightAmbD3DSpecular { get; set; }
        public int NightAmbRed { get; set; }
        public int NightAmbGreen { get; set; }
        public int NightAmbBlue { get; set; }
        public sbyte NightLampostRed { get; set; }
        public sbyte NightLampostGreen { get; set; }
        public sbyte NightLampostBlue { get; set; }
        public byte Padding { get; set; }
        public int NightLampostRadius { get; set; }

        // D3D color components with signed interpretation for RGB
        public byte D3DAlpha => (byte)((NightAmbD3DColour >> 24) & 0xFF);
        public byte D3DRed => (byte)((NightAmbD3DColour >> 16) & 0xFF);
        public byte D3DGreen => (byte)((NightAmbD3DColour >> 8) & 0xFF);
        public byte D3DBlue => (byte)(NightAmbD3DColour & 0xFF);

        // Specular color components with signed interpretation for RGB
        public byte SpecularAlpha => (byte)((NightAmbD3DSpecular >> 24) & 0xFF);
        public byte SpecularRed => (byte)((NightAmbD3DSpecular >> 16) & 0xFF);
        public byte SpecularGreen => (byte)((NightAmbD3DSpecular >> 8) & 0xFF);
        public byte SpecularBlue => (byte)(NightAmbD3DSpecular & 0xFF);
    }
}
