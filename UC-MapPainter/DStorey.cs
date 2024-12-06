using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UC_MapPainter
{
    public class DStorey
    {
        // 2-byte unsigned integer
        public ushort Style { get; set; } // Replacement style, potentially reducible to byte
        public ushort Index { get; set; } // Index to painted info

        // 1-byte signed integer
        public sbyte Count { get; set; } // +ve is a style, -ve is something else

        // 1-byte unsigned integer
        public byte BloodyPadding { get; set; }
    }
}
