using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace UC_MapPainter
{

    ///////////////////////////////////////////////
    //////            IAM STRUCTURE             ///
    //////  [4b - saveType][4b - Object size]   ///
    //////  [98304b -Texture and Height Data]   ///
    //////  [Variable # bytes- Building Data]   ///
    //////  [Variable # bytes - Objects Data]   ///
    //////  [2048b - MapWho Data][4b - World]   ///
    //////  [2000 bytes - PSX Texture Data  ]   ///
    ///////////////////////////////////////////////

    public static class Map
    {
        public static readonly int HeaderSize = 8;
        public static readonly int TextureDataSize = 98304; // 6 * (128 * 128)
        public static readonly int BuildingDataOffset = 98312; // 8 bytes header + texture data size
        public static readonly int MapWhoSize = 1024; // 32 * 32 entries

        //////////////////////////////
        /// MAP READ FUNCTIONS  //////
        //////////////////////////////

        //Read the save version of the Map. Game maps are typically version 25, but older maps exist also.
        public static int ReadMapSaveType(byte[] fileBytes)
        {
            return BitConverter.ToInt32(fileBytes, 0);
        }

        //Read the size of the 'objects section' (includes Prim Section and MapWho section)
        public static int ReadObjectSize(byte[] fileBytes)
        {
            return BitConverter.ToInt32(fileBytes, 4);
        }

        //Determine the actual size of the objects section 
        public static int ReadObjectSectionSize(byte[] fileBytes, int saveType, int objectBytes)
        {
            int size = fileBytes.Length - 12;

            // Adjust for texture data if saveType >= 25
            if (saveType >= 25)
            {
                size -= 2000;
            }

            // Subtract the original objectSectionSize
            size -= objectBytes;

            return size;
        }
        // Read the 128 * 128 Texture/Height data
        public static byte[] ReadTextureData(byte[] fileBytes)
        {
            byte[] textureData = new byte[TextureDataSize];
            Array.Copy(fileBytes, 8, textureData, 0, TextureDataSize);
            return textureData;
        }

        // Read a single Cell based off an X,Z offset.
        public static byte[] ReadTextureCell(byte[] fileBytes, int offset)
        {
            // Read 6 bytes from the offset of fileBytes from offset + header size (8)
            byte[] cellBytes = new byte[6];
            Array.Copy(fileBytes, offset + HeaderSize, cellBytes, 0, 6);

            return cellBytes;
        }

        //Read Walls/Building data
        public static byte[] ReadBuildingData(byte[] fileBytes, int objectOffset)
        {
            int buildingDataSize = objectOffset - BuildingDataOffset;
            byte[] buildingData = new byte[buildingDataSize];
            Array.Copy(fileBytes, BuildingDataOffset, buildingData, 0, buildingDataSize);
            return buildingData;
        }

        //Read just the number of objects
        public static int ReadNumberPrimObjects(byte[] fileBytes, int objectOffset)
        {
            return BitConverter.ToInt32(fileBytes, objectOffset);
        }

        // Read all the Prim Data
        public static List<Prim> ReadPrims(byte[] fileBytes, int objectOffset)
        {
            List<Prim> primList = new List<Prim>();

            int numObjects = BitConverter.ToInt32(fileBytes, objectOffset);

            // Read Prim Array
            for (int i = 0; i < numObjects; i++)
            {
                int index = objectOffset + 4 + (i * 8);
                var prim = new Prim
                {
                    Y = BitConverter.ToInt16(fileBytes, index),
                    X = fileBytes[index + 2],
                    Z = fileBytes[index + 3],
                    PrimNumber = fileBytes[index + 4],
                    Yaw = fileBytes[index + 5],
                    Flags = fileBytes[index + 6],
                    InsideIndex = fileBytes[index + 7]
                };
                primList.Add(prim);
            }

            return primList;
        }

        // Read the MapWho data
        public static List<MapWho> ReadMapWho(byte[] fileBytes, int objectOffset)
        {
            List<MapWho> mapWhoList = new List<MapWho>(MapWhoSize);

            int numObjects = BitConverter.ToInt32(fileBytes, objectOffset);

            // Calculate the MapWho offset
            int mapWhoOffset = objectOffset + 4 + (numObjects * 8);

            // Read MapWho
            for (int i = 0; i < MapWhoSize; i++)
            {
                int index = mapWhoOffset + (i * 2);
                ushort cell = BitConverter.ToUInt16(fileBytes, index);
                var mapWho = new MapWho
                {
                    Index = cell & 0x07FF,
                    Num = (cell >> 11) & 0x1F
                };
                mapWhoList.Add(mapWho);
            }

            return mapWhoList;
        }

        //Read the texture world number
        public static int ReadTextureWorld(byte[] fileBytes, int saveType)
        {
            int offset = saveType >= 25 ? fileBytes.Length - 2004 : fileBytes.Length - 4;
            return BitConverter.ToInt32(fileBytes, offset);
        }

        //Read the PSX Texture Data
        public static byte[] ReadPSXTextureData(byte[] fileBytes)
        {
            byte[] psxTextureData = new byte[2000];
            Array.Copy(fileBytes, fileBytes.Length - 2000, psxTextureData, 0, 2000);
            return psxTextureData;
        }

        //////////////////////////////
        /// MAP WRITE FUNCTIONS  /////
        //////////////////////////////

        // Write the new save type to buffer
        public static void WriteMapSaveType(byte[] newFileBytes, int newSaveType)
        {
            BitConverter.GetBytes(newSaveType).CopyTo(newFileBytes, 0);
        }

        //Write the new object size to buffer - this will account for all new Prims plus MapWho structure
        public static void WriteObjectSize(byte[] newFileBytes, int newObjectSize)
        {
            BitConverter.GetBytes(newObjectSize).CopyTo(newFileBytes, 4);
        }

        //Write the new textures and height data to buffer
        public static void WriteTextureData(byte[] newFileBytes, byte[] newTextureData, int offset)
        {
            Array.Copy(newTextureData, 0, newFileBytes, HeaderSize + offset, 6);
        }

        public static void WriteHeightData(byte[] newFileBytes, byte height, int offset)
        {
            newFileBytes[8 + offset + 4] = height;
        }

        //Write the new building data to buffer
        public static void WriteBuildingData(byte[] newFileBytes, byte[] newBuildingData, int newObjectOffset)
        {
            Array.Copy(newBuildingData, 0, newFileBytes, BuildingDataOffset, newObjectOffset - BuildingDataOffset);
        }

        //Write new Prim objects to buffer
        public static void WritePrims(byte[] newFileBytes, List<Prim> newPrimArray, int newObjectOffset)
        {
            int numObjects = newPrimArray.Count +1;
            BitConverter.GetBytes(numObjects).CopyTo(newFileBytes, newObjectOffset);

            // Write an initial 8 bytes of 0's before writing the prims
            Array.Fill(newFileBytes, (byte)0, newObjectOffset + 4, 8);

            // Write the prims in the primArray
            for (int i = 0; i < newPrimArray.Count; i++)
            {
                var prim = newPrimArray[i];
                int index = newObjectOffset + 4 + 8 + (i * 8); // Adjusted to start after the initial 8 bytes of 0's
                BitConverter.GetBytes(prim.Y).CopyTo(newFileBytes, index);
                newFileBytes[index + 2] = prim.X;
                newFileBytes[index + 3] = prim.Z;
                newFileBytes[index + 4] = prim.PrimNumber;
                newFileBytes[index + 5] = prim.Yaw;
                newFileBytes[index + 6] = prim.Flags;
                newFileBytes[index + 7] = prim.InsideIndex;
            }
        }

        //Write the updated MapWho structure to buffer
        public static void WriteMapWho(byte[] newFileBytes, List<MapWho> newMapWhoArray, int newMapWhoOffset)
        {
            for (int i = 0; i < newMapWhoArray.Count; i++)
            {
                BitConverter.GetBytes((ushort)((newMapWhoArray[i].Num << 11) | (newMapWhoArray[i].Index & 0x07FF))).CopyTo(newFileBytes, newMapWhoOffset + (i * 2));
            }
        }

        // Write the new texture world number to buffer
        public static void WriteTextureWorld(byte[] newFileBytes, int newTextureWorld, int saveType)
        {
            int offset = saveType >= 25 ? newFileBytes.Length - 2004 : newFileBytes.Length - 4;
            BitConverter.GetBytes(newTextureWorld).CopyTo(newFileBytes, offset);
        }

        // Write the new PSX texture data to buffer
        public static void WritePSXTextureData(byte[] newFileBytes, byte[] newPSXTextureData)
        {
            Array.Copy(newPSXTextureData, 0, newFileBytes, newFileBytes.Length - 2000, 2000);
        }
    }
}
