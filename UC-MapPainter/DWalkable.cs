using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UC_MapPainter
{
    public class DWalkable
    {
        // 2-byte unsigned integers
        public ushort StartPoint { get; set; } // Unused nowadays
        public ushort EndPoint { get; set; }   // Unused nowadays
        public ushort StartFace3 { get; set; } // Unused nowadays
        public ushort EndFace3 { get; set; }   // Unused nowadays

        public ushort StartFace4 { get; set; } // Indices into the roof faces
        public ushort EndFace4 { get; set; }

        // 1-byte unsigned integers
        public byte X1 { get; set; }
        public byte Z1 { get; set; }
        public byte X2 { get; set; }
        public byte Z2 { get; set; }
        public byte Y { get; set; }
        public byte StoreyY { get; set; }

        // 2-byte unsigned integers
        public ushort Next { get; set; }
        public ushort Building { get; set; }
    }
}
