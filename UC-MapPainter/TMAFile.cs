using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UC_MapPainter
{
    ///////////////////////////////////////////////////////////
    //////                TMA FILE STRUCTURE               ////
    //////  [4b  - save_type (Save Type Version)       ]   ////
    //////                                                 ////
    //////  TEXTURES_XY BLOCK:                             ////
    //////    [2b  - first_dim_size (Rows)             ]   ////
    //////    [2b  - second_dim_size (Columns)         ]   ////
    //////    [4b  - Texture Entry per Style           ]   ////
    //////      [1b - Page (Texture Page Index)        ]   ////
    //////      [1b - Tx (X-Coordinate)                ]   ////
    //////      [1b - Ty (Y-Coordinate)                ]   ////
    //////      [1b - Flip (Flip Flag)                 ]   ////
    //////                                                 ////
    //////  TEXTURE_STYLE_NAMES BLOCK:                     ////
    //////    [2b  - first_dim_size (Number of Styles) ]   ////
    //////    [2b  - second_dim_size (Name Length)     ]   ////
    //////    [21b - Style Name (ASCII, Null-padded)   ]   ////
    //////                                                 ////
    //////  TEXTURES_FLAGS BLOCK (Optional):               ////
    //////    [2b  - first_dim_size (Number of Styles) ]   ////
    //////    [2b  - second_dim_size (Flags per Style) ]   ////
    //////    [1b  - Flag Byte (Bitfield)             ]   ////
    ///////////////////////////////////////////////////////////

    public class TMAFile
    {
        public uint SaveType { get; set; }
        public List<TextureStyle> TextureStyles { get; set; }

        public static TMAFile ReadTMAFile(string filePath)
        {
            TMAFile tmaFile = new TMAFile();
            using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                // Read SaveType
                tmaFile.SaveType = reader.ReadUInt32();

                // Read Textures_XY Block Dimensions
                ushort firstDimSize = reader.ReadUInt16();
                ushort secondDimSize = reader.ReadUInt16();

                // Read Textures_XY Entries
                tmaFile.TextureStyles = new List<TextureStyle>(firstDimSize);
                for (int i = 0; i < firstDimSize; i++)
                {
                    TextureStyle style = new TextureStyle();
                    style.Entries = new List<TextureEntry>(secondDimSize);

                    for (int j = 0; j < secondDimSize; j++)
                    {
                        TextureEntry entry = new TextureEntry
                        {
                            Page = reader.ReadByte(),
                            Tx = reader.ReadByte(),
                            Ty = reader.ReadByte(),
                            Flip = reader.ReadByte()
                        };
                        style.Entries.Add(entry);
                    }
                    tmaFile.TextureStyles.Add(style);
                }

                // Read Texture_Style_Names Block Dimensions
                ushort nameFirstDimSize = reader.ReadUInt16();
                ushort nameSecondDimSize = reader.ReadUInt16();

                // Read Texture Style Names
                for (int i = 0; i < nameFirstDimSize; i++)
                {
                    byte[] nameBytes = reader.ReadBytes(nameSecondDimSize);
                    string name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');
                    tmaFile.TextureStyles[i].Name = name;
                }

                // Read Textures_Flags Block if present
                if (tmaFile.SaveType > 2)
                {
                    ushort flagsFirstDimSize = reader.ReadUInt16();
                    ushort flagsSecondDimSize = reader.ReadUInt16();

                    for (int i = 0; i < flagsFirstDimSize; i++)
                    {
                        List<TextureFlag> flags = new List<TextureFlag>(flagsSecondDimSize);
                        for (int j = 0; j < flagsSecondDimSize; j++)
                        {
                            byte flagByte = reader.ReadByte();
                            flags.Add((TextureFlag)flagByte);
                        }
                        tmaFile.TextureStyles[i].Flags = flags;
                    }
                }
            }
            return tmaFile;
        }
    }

    public class TextureStyle
    {
        public string Name { get; set; }
        public List<TextureEntry> Entries { get; set; }
        public List<TextureFlag> Flags { get; set; }
    }

    public class TextureEntry
    {
        public byte Page { get; set; }
        public byte Tx { get; set; }
        public byte Ty { get; set; }
        public byte Flip { get; set; }
    }

    [Flags]
    public enum TextureFlag : byte
    {
        Gouraud = 0x01,
        Textured = 0x02,
        Masked = 0x04,
        Transparent = 0x08,
        Alpha = 0x10,
        Tiled = 0x20,
        TwoSided = 0x40,
        // Bit 7 is unused
    }
}
