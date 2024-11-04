using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UC_MapPainter
{
    public class Wall
    {
        public byte WallTypeByte { get; set; }
        public string WallTypeDescription { get; set; }
        public byte WallHeightByte { get; set; }
        public int WallHeightStoreys { get; set; }
        public byte X1 { get; set; }
        public byte X2 { get; set; }
        public byte Z1 { get; set; }
        public byte Z2 { get; set; }
        public byte StartStorey { get; set; }
        public bool IsClimbable { get; set; }
        public short WallNumber { get; set; } // 13th and 14th bytes
        public string Scale { get; set; }
        public byte[] RawData { get; set; }
        public string RawDataHex => BitConverter.ToString(RawData).Replace("-", " ");

        // Wall Types as per your understanding
        private static readonly Dictionary<byte, string> wallTypes = new Dictionary<byte, string>
        {
            { 0x0A, "Barbed Wire Fence" },
            { 0x01, "Normal" },
            { 0x0C, "Ladder" },
            { 0x0B, "Chain Fence" },
            { 0x0D, "Jumpable Chain Fence" },
            { 0x12, "Door" },
            { 0x09, "Cable" },
            { 0x15, "Unclimbable Bar Fence" }
        };

        public static Wall ReadWall(byte[] fileBytes, int offset)
        {
            byte[] wallData = new byte[26];
            Array.Copy(fileBytes, offset, wallData, 0, 26);

            Wall wall = new Wall
            {
                WallTypeByte = wallData[0],
                WallTypeDescription = wallTypes.ContainsKey(wallData[0]) ? wallTypes[wallData[0]] : "Unknown",
                WallHeightByte = wallData[1],
                WallHeightStoreys = wallData[1] / 4,
                X1 = wallData[2],
                X2 = wallData[3],
                Z1 = wallData[8],
                Z2 = wallData[9],
                StartStorey = wallData[5],
                IsClimbable = wallData[11] != 1, // Climbable is "No" if byte is 1, otherwise "Yes"
                WallNumber = BitConverter.ToInt16(wallData, 12), // 13th and 14th bytes
                Scale = wallData[19] == 0x10 ? "Normal" : (wallData[19] == 0x08 ? "Half" : (wallData[19] == 0x04 ? "Quarter" : "Unknown")),
                RawData = wallData
            };

            return wall;
        }
    }
}