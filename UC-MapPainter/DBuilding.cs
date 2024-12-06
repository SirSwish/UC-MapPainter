using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UC_MapPainter
{
    public class DBuilding
    {
        // 4-byte signed integer
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        // 2-byte unsigned integer
        public ushort StartFacet { get; set; }
        public ushort EndFacet { get; set; }
        public ushort Walkable { get; set; }

        // Array of 2 1-byte unsigned integers
        public byte[] Counter { get; set; } = new byte[2];

        // 2-byte unsigned integer
        public ushort Padding { get; set; }

        // 1-byte unsigned integer
        public byte Ware { get; set; }
        public byte Type { get; set; }
    }
}
