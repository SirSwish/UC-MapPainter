using System.Collections.Generic;

namespace UC_MapPainter
{
    public class GridModel
    {
        public List<Cell> Cells { get; set; } = new List<Cell>();
        public List<ObOb> ObObArray { get; set; } = new List<ObOb>();
        public List<MapWho> MapWhoArray { get; set; } = new List<MapWho>();
    }
}
