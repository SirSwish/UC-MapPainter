namespace UC_MapPainter
{
    public class MapWho
    {
        ///////////////////////////////////////////////
        //////            MAPWHO STRUCTURE          ///
        //////   [Lower 11-bits - Object Index]     ///
        //////   [Higher 5-bits - Number of Objects ///
        //////                                      ///
        ///////////////////////////////////////////////

        //Example: [1,5] Start at index 1, the next 5 objects from position 1 appear in MapWho x
        public int Index { get; set; }
        public int Num { get; set; }
    }
}
