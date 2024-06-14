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

        public string DisplayName => ObjectNames.GetName(PrimNumber);
    }
}
