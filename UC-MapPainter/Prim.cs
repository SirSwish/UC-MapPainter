namespace UC_MapPainter
{
    public class Prim
    {
        public short Y { get; set; }
        public byte X { get; set; }
        public byte Z { get; set; }
        public byte PrimNumber { get; set; }
        public byte Yaw { get; set; }
        public byte Flags { get; set; }
        public byte InsideIndex { get; set; }
        // New properties to store actual map positions
        public int PixelX { get; set; }
        public int PixelZ { get; set; }
        public int MapWhoIndex { get; set; }

        public string DisplayName => ObjectNames.GetName(PrimNumber);
    }
}
