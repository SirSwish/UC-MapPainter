using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UC_MapPainter
{
    public class TMAReader
    {
        public TMAFile ReadTMAFile(string filePath)
        {
            TMAFile tmaFile = new TMAFile();

            using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                // Read save_type (4 bytes)
                tmaFile.SaveType = reader.ReadUInt32();

                // Read Textures_XY Block Dimensions (first_dim_size, second_dim_size)
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

                // Read Texture_Style_Names Block Dimensions (first_dim_size, second_dim_size)
                ushort nameFirstDimSize = reader.ReadUInt16();
                ushort nameSecondDimSize = reader.ReadUInt16();

                // Read Texture Style Names
                for (int i = 0; i < nameFirstDimSize; i++)
                {
                    byte[] nameBytes = reader.ReadBytes(nameSecondDimSize);
                    string name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');
                    tmaFile.TextureStyles[i].Name = name;
                }

                // Read Textures_Flags Block if present (save_type > 2)
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
}
