using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UC_MapPainter
{
    public class DFacet
    {
        // 1-byte unsigned integer
        public byte FacetType { get; set; } = 1; // Default: 1
        public byte Height { get; set; } = 4;    // Default: 4

        // 2-element array of 1-byte unsigned integers
        public byte[] X { get; set; } = new byte[2];

        // 2-element array of 2-byte signed integers
        public short[] Y { get; set; } = new short[2];

        // 2-element array of 1-byte unsigned integers
        public byte[] Z { get; set; } = new byte[2];

        // 2-byte unsigned integers
        public ushort FacetFlags { get; set; } = 256; // Default: 256
        public ushort StyleIndex { get; set; } = 1;   // Default: 1
        public ushort Building { get; set; } = 1;
        public ushort DStorey { get; set; }

        // 1-byte unsigned integers
        public byte FHeight { get; set; }
        public byte BlockHeight { get; set; } = 16; // Default: 16
        public byte Open { get; set; }
        public byte Dfcache { get; set; }
        public byte Shake { get; set; }
        public byte CutHole { get; set; }

        // 2-element array of 1-byte unsigned integers
        public byte[] Counter { get; set; } = new byte[2];


        // Default constructor with preset defaults
        public DFacet() { }

        // Constructor for zero-initialized object
        public DFacet(bool initializeToZero)
        {
            if (initializeToZero)
            {
                FacetType = 0;
                Height = 0;
                X = new byte[2] { 0, 0 };
                Y = new short[2] { 0, 0 };
                Z = new byte[2] { 0, 0 };
                FacetFlags = 0;
                StyleIndex = 0;
                Building = 0;
                DStorey = 0;
                FHeight = 0;
                BlockHeight = 0;
                Open = 0;
                Dfcache = 0;
                Shake = 0;
                CutHole = 0;
                Counter = new byte[2] { 0, 0 };
            }
        }

        public void setClimbable()
        {
            FacetFlags |= (1 << 6);
        }
    }
}
