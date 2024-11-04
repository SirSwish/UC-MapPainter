using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UC_MapPainter
{
    public class BuildingHeader
    {
        public int TotalBuildings { get; set; }
        public int TotalWalls { get; set; }

        public static BuildingHeader ReadFromBytes(byte[] fileBytes, int offset)
        {
            BuildingHeader header = new BuildingHeader
            {
                TotalBuildings = BitConverter.ToUInt16(fileBytes, offset + 2) - 1,
                TotalWalls = BitConverter.ToUInt16(fileBytes, offset + 4) - 1
            };
            return header;
        }
    }
}
