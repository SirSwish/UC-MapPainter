using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Windows.Input;

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
        public static readonly int BuildingHeaderSize = 48; //Size of the header section in the building data
        public static readonly int BuildingPaddingSize = 14; //Size of the padding
        public static readonly int WallPaddingSize = 14; //Size of the padding

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
        public static BuildingHeader ReadBuildingHeader(byte[] fileBytes, int offset)
        {
            return BuildingHeader.ReadFromBytes(fileBytes, offset);
        }

        public static List<Building> ReadBuildings(byte[] fileBytes, int offset, int totalBuildings)
        {
            int buildingDataOffset = offset + BuildingHeaderSize + BuildingPaddingSize;
            return Building.ReadBuildings(fileBytes, buildingDataOffset, totalBuildings);
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
            int numObjects = newPrimArray.Count + 1;
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
        //public static void WriteBuildingsMock(byte[] newFileBytes, int offset, List<DBuilding> buildings, List<DFacet> facets, List<DStorey> storeys)
        //{
        //    ushort next_dbuilding = (ushort)(buildings.Count + 1); // buildings.count() + 1
        //    int offsetCursor = offset;
        //    BitConverter.GetBytes(next_dbuilding).CopyTo(newFileBytes, offsetCursor);
        //    offsetCursor += 2;

        //    ushort next_dfacet = (ushort)(facets.Count + 1); ; // facets.count() + 1
        //    BitConverter.GetBytes(next_dfacet).CopyTo(newFileBytes, offsetCursor);
        //    offsetCursor += 2;

        //    // FIXME
        //    ushort next_dstyle = (ushort)(facets.Count + 1); ; // styles.count() + 1
        //    BitConverter.GetBytes(next_dstyle).CopyTo(newFileBytes, offsetCursor);
        //    offsetCursor += 2;

        //    ushort next_paint_mem = 1;
        //    BitConverter.GetBytes(next_paint_mem).CopyTo(newFileBytes, offsetCursor);
        //    offsetCursor += 2;

        //    ushort next_dstorey = 1;
        //    BitConverter.GetBytes(next_dstorey).CopyTo(newFileBytes, offsetCursor);
        //    offsetCursor += 2;

        //    DBuilding building = new DBuilding();

        //    WriteSingleBuilding(newFileBytes, offsetCursor, building);
        //    offsetCursor += 24;

        //    for (int i = 0; i < buildings.Count; i++)
        //    {
        //        WriteSingleBuilding(newFileBytes, offsetCursor, buildings[i]);
        //        offsetCursor += 24;
        //    }

        //    DFacet facet = new DFacet(true);
        //    WriteSingleFacet(newFileBytes, offsetCursor, facet);
        //    offsetCursor += 26;

        //    for (int i = 0; i < facets.Count; i++)
        //    {
        //        WriteSingleFacet(newFileBytes, offsetCursor, facets[i]);
        //        offsetCursor += 26;
        //    }


        //    // FIXME


        //    ushort dstyles = 0;
        //    BitConverter.GetBytes(dstyles).CopyTo(newFileBytes, offsetCursor);
        //    offsetCursor += 1;
        //    dstyles = 3;
        //    for (int i = 0; i < facets.Count; i++)
        //    {
        //        BitConverter.GetBytes(dstyles).CopyTo(newFileBytes, offsetCursor);
        //        offsetCursor += 1;
        //    }

        //    List<byte> paint_mem = new List<byte>
        //    {
        //        0
        //    };
        //    for (int i = 0; i < paint_mem.Count; i++)
        //    {
        //        newFileBytes[offsetCursor] = paint_mem[i];
        //        offsetCursor += 1;
        //    }

        //    DStorey storey = new DStorey();
        //    WriteSingleStorey(newFileBytes, offsetCursor, storey);
        //    offsetCursor += 6;

        //}

        //public static void WriteSingleBuilding(byte[] newFileBytes, int offset, DBuilding building)
        //{
        //    int cursor = offset;

        //    // Write X, Y, Z (4 bytes each)
        //    BitConverter.GetBytes(building.X).CopyTo(newFileBytes, cursor);
        //    cursor += 4;

        //    BitConverter.GetBytes(building.Y).CopyTo(newFileBytes, cursor);
        //    cursor += 4;

        //    BitConverter.GetBytes(building.Z).CopyTo(newFileBytes, cursor);
        //    cursor += 4;

        //    // Write StartFacet and EndFacet (2 bytes each)
        //    BitConverter.GetBytes(building.StartFacet).CopyTo(newFileBytes, cursor);
        //    cursor += 2;

        //    BitConverter.GetBytes(building.EndFacet).CopyTo(newFileBytes, cursor);
        //    cursor += 2;

        //    // Write Walkable (2 bytes)
        //    BitConverter.GetBytes(building.Walkable).CopyTo(newFileBytes, cursor);
        //    cursor += 2;

        //    // Write Counter (2 bytes, directly as they are already bytes)
        //    newFileBytes[cursor] = building.Counter[0];
        //    cursor += 1;

        //    newFileBytes[cursor] = building.Counter[1];
        //    cursor += 1;

        //    // Write Padding (2 bytes)
        //    BitConverter.GetBytes(building.Padding).CopyTo(newFileBytes, cursor);
        //    cursor += 2;

        //    // Write Ware and Type (1 byte each)
        //    newFileBytes[cursor] = building.Ware;
        //    cursor += 1;

        //    newFileBytes[cursor] = building.Type;
        //    cursor += 1;

        //    //BitConverter.GetBytes(building.X).CopyTo(newFileBytes, offset);
        //    //BitConverter.GetBytes(building.Y).CopyTo(newFileBytes, offset+4);
        //    //BitConverter.GetBytes(building.Z).CopyTo(newFileBytes, offset+8);
        //    //BitConverter.GetBytes(building.StartFacet).CopyTo(newFileBytes, offset+12);
        //    //BitConverter.GetBytes(building.EndFacet).CopyTo(newFileBytes, offset+14);
        //    //BitConverter.GetBytes(building.Walkable).CopyTo(newFileBytes, offset+16);
        //    //BitConverter.GetBytes(building.Counter[0]).CopyTo(newFileBytes, offset+18);
        //    //BitConverter.GetBytes(building.Counter[1]).CopyTo(newFileBytes, offset+19);
        //}

        //public static void WriteSingleFacet(byte[] newFileBytes, int offset, DFacet facet)
        //{
        //    int cursor = offset;

        //    // Write FacetType (1 byte)
        //    newFileBytes[cursor] = facet.FacetType;
        //    cursor += 1;

        //    // Write Height (1 byte)
        //    newFileBytes[cursor] = facet.Height;
        //    cursor += 1;

        //    // Write x[2] (2 bytes, directly as it's already byte[])
        //    newFileBytes[cursor] = facet.X[0];
        //    cursor += 1;

        //    newFileBytes[cursor] = facet.X[1];
        //    cursor += 1;

        //    // Write Y[2] (4 bytes, using BitConverter for 2 SWORD values)
        //    BitConverter.GetBytes(facet.Y[0]).CopyTo(newFileBytes, cursor);
        //    cursor += 2;

        //    BitConverter.GetBytes(facet.Y[1]).CopyTo(newFileBytes, cursor);
        //    cursor += 2;

        //    // Write z[2] (2 bytes, directly as it's already byte[])
        //    newFileBytes[cursor] = facet.Z[0];
        //    cursor += 1;

        //    newFileBytes[cursor] = facet.Z[1];
        //    cursor += 1;

        //    // Write FacetFlags (2 bytes)
        //    BitConverter.GetBytes(facet.FacetFlags).CopyTo(newFileBytes, cursor);
        //    cursor += 2;

        //    // Write StyleIndex (2 bytes)
        //    BitConverter.GetBytes(facet.StyleIndex).CopyTo(newFileBytes, cursor);
        //    cursor += 2;

        //    // Write Building (2 bytes)
        //    BitConverter.GetBytes(facet.Building).CopyTo(newFileBytes, cursor);
        //    cursor += 2;

        //    // Write DStorey (2 bytes)
        //    BitConverter.GetBytes(facet.DStorey).CopyTo(newFileBytes, cursor);
        //    cursor += 2;

        //    // Write FHeight (1 byte)
        //    newFileBytes[cursor] = facet.FHeight;
        //    cursor += 1;

        //    // Write BlockHeight (1 byte)
        //    newFileBytes[cursor] = facet.BlockHeight;
        //    cursor += 1;

        //    // Write Open (1 byte)
        //    newFileBytes[cursor] = facet.Open;
        //    cursor += 1;

        //    // Write Dfcache (1 byte)
        //    newFileBytes[cursor] = facet.Dfcache;
        //    cursor += 1;

        //    // Write Shake (1 byte)
        //    newFileBytes[cursor] = facet.Shake;
        //    cursor += 1;

        //    // Write CutHole (1 byte)
        //    newFileBytes[cursor] = facet.CutHole;
        //    cursor += 1;

        //    // Write Counter[2] (2 bytes, directly as it's already byte[])
        //    newFileBytes[cursor] = facet.Counter[0];
        //    cursor += 1;

        //    newFileBytes[cursor] = facet.Counter[1];
        //    cursor += 1;
        //}

        //public static void WriteSingleStorey(byte[] newFileBytes, int offset, DStorey storey)
        //{
        //    int cursor = offset;

        //    BitConverter.GetBytes(storey.Style).CopyTo(newFileBytes, cursor);
        //    cursor+= 2;
        //    BitConverter.GetBytes(storey.Index).CopyTo(newFileBytes, cursor);
        //    cursor += 2;
        //    BitConverter.GetBytes(storey.Count).CopyTo(newFileBytes, cursor);
        //    cursor += 1;
        //    newFileBytes[cursor] = storey.BloodyPadding;
        //    cursor += 1;

        //}

        /// SECOND APPROACH
        //public static byte[] PrepareBuildingsData(List<DBuilding> buildings, List<DFacet> facets, List<DStorey> storeys)
        //{
        //    List<byte> buildingByteBuffer = new List<byte>();

        //    ushort next_dbuilding = (ushort)(buildings.Count + 1);
        //    buildingByteBuffer.Append(BitConverter.GetBytes(next_dbuilding));


        //    int initialOffset = 0;

        //    // Estimate the required size based on input data (adjust as needed)
        //    int estimatedSize = initialOffset +
        //                        (24 * (buildings.Count + 1)) + // Size of each building (including one new building)
        //                        (26 * (facets.Count + 1)) +   // Size of each facet (including one new facet)
        //                        (6 * (storeys.Count + 1)) +   // Size of each storey (including one new storey)
        //                        10; // Buffer for other fields and metadata

        //    estimatedSize = 0;

        //    // Create a dynamic buffer using List<byte>
        //    List<byte> byteBuffer = new List<byte>(new byte[estimatedSize]);

        //    // Cursor to track the offset
        //    int offsetCursor = initialOffset;

        //    // Write building metadata
        //    ushort next_dbuilding = (ushort)(buildings.Count + 1);
        //    byteBuffer.InsertRange(offsetCursor, BitConverter.GetBytes(next_dbuilding));
        //    offsetCursor += 2;

        //    ushort next_dfacet = (ushort)(facets.Count + 1);
        //    byteBuffer.InsertRange(offsetCursor, BitConverter.GetBytes(next_dfacet));
        //    offsetCursor += 2;

        //    ushort next_dstyle = (ushort)(facets.Count + 1); // Assuming facets.Count represents styles for now
        //    byteBuffer.InsertRange(offsetCursor, BitConverter.GetBytes(next_dstyle));
        //    offsetCursor += 2;

        //    ushort next_paint_mem = 1;
        //    byteBuffer.InsertRange(offsetCursor, BitConverter.GetBytes(next_paint_mem));
        //    offsetCursor += 2;

        //    ushort next_dstorey = 1;
        //    byteBuffer.InsertRange(offsetCursor, BitConverter.GetBytes(next_dstorey));
        //    offsetCursor += 2;

        //    // Write a new default building
        //    DBuilding building = new DBuilding();
        //    byteBuffer.InsertRange(offsetCursor, WriteSingleBuildingToBytes(building));
        //    offsetCursor += 24;

        //    // Write existing buildings
        //    foreach (var b in buildings)
        //    {
        //        byteBuffer.InsertRange(offsetCursor, WriteSingleBuildingToBytes(b));
        //        offsetCursor += 24;
        //    }

        //    // Write a new default facet
        //    DFacet facet = new DFacet(true);
        //    byteBuffer.InsertRange(offsetCursor, WriteSingleFacetToBytes(facet));
        //    offsetCursor += 26;

        //    // Write existing facets
        //    foreach (var f in facets)
        //    {
        //        byteBuffer.InsertRange(offsetCursor, WriteSingleFacetToBytes(f));
        //        offsetCursor += 26;
        //    }

        //    // Example for styles (assumed 1 byte per style)
        //    ushort dstyles = 0;
        //    byteBuffer.Add((byte)dstyles);
        //    for (int i = 0; i < facets.Count; i++)
        //    {
        //        byteBuffer.Add(3); // Example value for style
        //    }

        //    // Example for paint memory
        //    List<byte> paintMem = new List<byte> { 0 };
        //    byteBuffer.AddRange(paintMem);

        //    // Write a new default storey
        //    DStorey storey = new DStorey();
        //    byteBuffer.InsertRange(offsetCursor, WriteSingleStoreyToBytes(storey));
        //    offsetCursor += 6;

        //    // Return the new byte array
        //    return byteBuffer.ToArray();
        //}

        //// Helper method for building serialization
        //private static byte[] WriteSingleBuildingToBytes(DBuilding building)
        //{
        //    byte[] bytes = new byte[24];
        //    int offset = 0;

        //    BitConverter.GetBytes(building.X).CopyTo(bytes, offset);
        //    offset += 4;

        //    BitConverter.GetBytes(building.Y).CopyTo(bytes, offset);
        //    offset += 4;

        //    BitConverter.GetBytes(building.Z).CopyTo(bytes, offset);
        //    offset += 4;

        //    BitConverter.GetBytes(building.StartFacet).CopyTo(bytes, offset);
        //    offset += 2;

        //    BitConverter.GetBytes(building.EndFacet).CopyTo(bytes, offset);
        //    offset += 2;

        //    BitConverter.GetBytes(building.Walkable).CopyTo(bytes, offset);
        //    offset += 2;

        //    building.Counter.CopyTo(bytes, offset);
        //    offset += 2;

        //    BitConverter.GetBytes(building.Padding).CopyTo(bytes, offset);
        //    offset += 2;

        //    bytes[offset++] = building.Ware;
        //    bytes[offset++] = building.Type;

        //    return bytes;
        //}

        //// Helper method for facet serialization
        //private static byte[] WriteSingleFacetToBytes(DFacet facet)
        //{
        //    byte[] bytes = new byte[26];
        //    int offset = 0;

        //    bytes[offset++] = facet.FacetType;
        //    bytes[offset++] = facet.Height;

        //    facet.X.CopyTo(bytes, offset);
        //    offset += 2;

        //    foreach (var y in facet.Y)
        //    {
        //        BitConverter.GetBytes(y).CopyTo(bytes, offset);
        //        offset += 2;
        //    }

        //    facet.Z.CopyTo(bytes, offset);
        //    offset += 2;

        //    BitConverter.GetBytes(facet.FacetFlags).CopyTo(bytes, offset);
        //    offset += 2;

        //    BitConverter.GetBytes(facet.StyleIndex).CopyTo(bytes, offset);
        //    offset += 2;

        //    BitConverter.GetBytes(facet.Building).CopyTo(bytes, offset);
        //    offset += 2;

        //    BitConverter.GetBytes(facet.DStorey).CopyTo(bytes, offset);
        //    offset += 2;

        //    bytes[offset++] = facet.FHeight;
        //    bytes[offset++] = facet.BlockHeight;
        //    bytes[offset++] = facet.Open;
        //    bytes[offset++] = facet.Dfcache;
        //    bytes[offset++] = facet.Shake;
        //    bytes[offset++] = facet.CutHole;

        //    facet.Counter.CopyTo(bytes, offset);
        //    offset += 2;

        //    return bytes;
        //}

        //// Helper method for storey serialization
        //private static byte[] WriteSingleStoreyToBytes(DStorey storey)
        //{
        //    byte[] bytes = new byte[6];
        //    int offset = 0;

        //    BitConverter.GetBytes(storey.Style).CopyTo(bytes, offset);
        //    offset += 2;

        //    BitConverter.GetBytes(storey.Index).CopyTo(bytes, offset);
        //    offset += 2;

        //    bytes[offset++] = (byte)storey.Count;
        //    bytes[offset++] = storey.BloodyPadding;

        //    return bytes;
        //}

        public static List<byte> PrepareBuildingsMock(List<DBuilding> buildings, List<DFacet> facets, List<DStorey> storeys)
        {
            // Initialize a dynamic buffer
            List<byte> byteBuffer = new List<byte>();

            // Write building metadata
            ushort next_dbuilding = (ushort)(buildings.Count + 1);
            byteBuffer.AddRange(BitConverter.GetBytes(next_dbuilding));

            ushort next_dfacet = (ushort)(facets.Count + 1);
            byteBuffer.AddRange(BitConverter.GetBytes(next_dfacet));

            ushort next_dstyle = (ushort)(facets.Count + 1); // Assuming facets.Count represents styles for now
            byteBuffer.AddRange(BitConverter.GetBytes(next_dstyle));

            ushort next_paint_mem = 1;
            byteBuffer.AddRange(BitConverter.GetBytes(next_paint_mem));

            ushort next_dstorey = 1;
            byteBuffer.AddRange(BitConverter.GetBytes(next_dstorey));

            // Write a new default building
            DBuilding defaultBuilding = new DBuilding();
            byteBuffer.AddRange(WriteSingleBuildingToBytes(defaultBuilding));

            // Write existing buildings
            foreach (var b in buildings)
            {
                byteBuffer.AddRange(WriteSingleBuildingToBytes(b));
            }

            // Write a new default facet
            DFacet defaultFacet = new DFacet(true);
            byteBuffer.AddRange(WriteSingleFacetToBytes(defaultFacet));

            // Write existing facets
            foreach (var f in facets)
            {
                byteBuffer.AddRange(WriteSingleFacetToBytes(f));
            }

            // Example for styles (assumed 1 byte per style)
            byteBuffer.Add(0); // Start of styles
            for (int i = 0; i < facets.Count; i++)
            {
                byteBuffer.Add(3); // Example value for style
            }

            // Example for paint memory
            List<byte> paintMem = new List<byte> { 0 };
            byteBuffer.AddRange(paintMem);

            // Write a new default storey
            DStorey defaultStorey = new DStorey();
            byteBuffer.AddRange(WriteSingleStoreyToBytes(defaultStorey));


            // UNCHANGED FOR NOW
            //ushort next_inside_storey = 1;
            //byteBuffer.AddRange(BitConverter.GetBytes(next_inside_storey));

            //ushort next_inside_stair = 1;
            //byteBuffer.AddRange(BitConverter.GetBytes(next_inside_stair));

            //ushort next_block = 1;
            //byteBuffer.AddRange(BitConverter.GetBytes(next_block));

            ////FileWrite(handle, &inside_storeys[0], sizeof(struct InsideStorey)*next_inside_storey);
            ////FileWrite(handle,&inside_stairs[0],sizeof(struct Staircase)*next_inside_stair);
            ////FileWrite(handle,&inside_block[0],sizeof(UBYTE)* next_inside_block);

            //// sizeof(struct InsideStorey) - 22  bytes
            //// sizeof(struct Staircase) - 10 bytes

            //for (int i = 0; i < 33; i++)
            //{
            //    byteBuffer.Add(0); // Ignore InsideStorey Staircase data for now
            //}

            // sizeof(struct DWalkable) - 22 bytes
            // sizeof(struct RoofFace4) - 10 bytes

            return byteBuffer; // Return the prepared byte list
        }

        // Helper method for building serialization
        private static byte[] WriteSingleBuildingToBytes(DBuilding building)
        {
            byte[] bytes = new byte[24];
            int offset = 0;

            BitConverter.GetBytes(building.X).CopyTo(bytes, offset);
            offset += 4;

            BitConverter.GetBytes(building.Y).CopyTo(bytes, offset);
            offset += 4;

            BitConverter.GetBytes(building.Z).CopyTo(bytes, offset);
            offset += 4;

            BitConverter.GetBytes(building.StartFacet).CopyTo(bytes, offset);
            offset += 2;

            BitConverter.GetBytes(building.EndFacet).CopyTo(bytes, offset);
            offset += 2;

            BitConverter.GetBytes(building.Walkable).CopyTo(bytes, offset);
            offset += 2;

            building.Counter.CopyTo(bytes, offset);
            offset += 2;

            BitConverter.GetBytes(building.Padding).CopyTo(bytes, offset);
            offset += 2;

            bytes[offset++] = building.Ware;
            bytes[offset++] = building.Type;

            return bytes;
        }

        // Helper method for facet serialization
        private static byte[] WriteSingleFacetToBytes(DFacet facet)
        {
            byte[] bytes = new byte[26];
            int offset = 0;

            bytes[offset++] = facet.FacetType;
            bytes[offset++] = facet.Height;

            facet.X.CopyTo(bytes, offset);
            offset += 2;

            foreach (var y in facet.Y)
            {
                BitConverter.GetBytes(y).CopyTo(bytes, offset);
                offset += 2;
            }

            facet.Z.CopyTo(bytes, offset);
            offset += 2;

            BitConverter.GetBytes(facet.FacetFlags).CopyTo(bytes, offset);
            offset += 2;

            BitConverter.GetBytes(facet.StyleIndex).CopyTo(bytes, offset);
            offset += 2;

            BitConverter.GetBytes(facet.Building).CopyTo(bytes, offset);
            offset += 2;

            BitConverter.GetBytes(facet.DStorey).CopyTo(bytes, offset);
            offset += 2;

            bytes[offset++] = facet.FHeight;
            bytes[offset++] = facet.BlockHeight;
            bytes[offset++] = facet.Open;
            bytes[offset++] = facet.Dfcache;
            bytes[offset++] = facet.Shake;
            bytes[offset++] = facet.CutHole;

            facet.Counter.CopyTo(bytes, offset);
            offset += 2;

            return bytes;
        }

        // Helper method for storey serialization
        private static byte[] WriteSingleStoreyToBytes(DStorey storey)
        {
            byte[] bytes = new byte[6];
            int offset = 0;

            BitConverter.GetBytes(storey.Style).CopyTo(bytes, offset);
            offset += 2;

            BitConverter.GetBytes(storey.Index).CopyTo(bytes, offset);
            offset += 2;

            bytes[offset++] = (byte)storey.Count;
            bytes[offset++] = storey.BloodyPadding;

            return bytes;
        }

    }
}
