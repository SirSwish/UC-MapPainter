using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace UC_MapPainter
{
    public class Building
    {
        // Properties for the 24-byte structure (e.g., position, dimensions, etc.)
        public int StartingWallIndex { get; set; }   // Start wall index (2 bytes)
        public int EndingWallIndex { get; set; }     // End wall index (2 bytes)
        public string Roof { get; set; }           // Roof type (1 byte)
        public string Type { get; set; }       // Building type (1 byte at offset 11)
        public byte[] RawData { get; set; } // Store the raw 24 bytes

        public string RawDataHex
        {
            get
            {
                return BitConverter.ToString(RawData).Replace("-", " ");
            }
        }
        public static List<Building> ReadBuildings(byte[] fileBytes, int offset, int count)
        {
            List<Building> buildings = new List<Building>();
            for (int i = 0; i < count; i++)
            {
                int index = offset + (i * 24);
                Building building = new Building
                {
                    // Initialize building properties by reading from the fileBytes.
                };
                buildings.Add(building);
            }
            return buildings;
        }

        // Method to read a Building object from file bytes
        public static Building ReadBuilding(byte[] fileBytes, int offset)
        {
            Building building = new Building
            {
                StartingWallIndex = BitConverter.ToUInt16(fileBytes, offset),
                EndingWallIndex = BitConverter.ToUInt16(fileBytes, offset + 2),
                Roof = ParseRoofType(fileBytes[offset + 4]),
                Type = ParseBuildingType(fileBytes[offset + 11]),
                RawData = fileBytes.Skip(offset).Take(24).ToArray()
            };

            return building;
        }

        // Enum for Roof Types (This property is definitely wrong, but keeping anyway)
        private static string ParseRoofType(byte roofTypeByte)
        {
            switch (roofTypeByte)
            {
                case 0x00: return "Flat Roof";
                case 0x01: return "No Roof";
                default: return $"Unknown (0x{roofTypeByte:X2})";
            }
        }

        private static string ParseBuildingType(byte buildingTypeByte)
        {
            string[] buildingTypes = { "House", "Warehouse", "Office", "Apartment", "Crate" };
            return buildingTypeByte < buildingTypes.Length ? buildingTypes[buildingTypeByte] : "Unknown";
        }
    }
}
