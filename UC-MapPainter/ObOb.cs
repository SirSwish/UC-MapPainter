namespace UC_MapPainter
{
    public class ObOb
    {
        public short Y { get; set; }
        public byte X { get; set; }
        public byte Z { get; set; }
        public byte Prim { get; set; }
        public byte Yaw { get; set; }
        public byte Flags { get; set; }
        public byte InsideIndex { get; set; }

        public string DisplayName => ObjectNames.GetName(Prim);
    }
}
