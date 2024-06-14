using System.Collections.Generic;

namespace UC_MapPainter
{
    public class GridModel
    {
        public List<Cell> Cells { get; set; } = new List<Cell>();
        public List<Prim> PrimArray { get; set; } = new List<Prim>();
        public List<MapWho> MapWhoArray { get; set; } = new List<MapWho>();
    }
}
