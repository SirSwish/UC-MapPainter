namespace UC_MapPainter
{
    public class Cell
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public string TextureType { get; set; }
        public int TextureNumber { get; set; }
        public int Rotation { get; set; }
        public byte[] TileSequence { get; set; } = new byte[6];
        public int Height { get; set; } = 0; // Default height is 0


        public void UpdateTileSequence(bool isDefaultTexture)
        {
            byte textureByte = 0;
            byte methodBits = 0;

            // Calculate the texture byte and method bits
            if (TextureType == "world")
            {
                textureByte = (byte)TextureNumber;
                methodBits = 0b00;
            }
            else if (TextureType == "shared")
            {
                textureByte = (byte)(TextureNumber - 256);
                methodBits = 0b01;
            }
            else if (TextureType == "prims")
            {
                if (TextureNumber >= 0 && TextureNumber <= 63)
                {
                    textureByte = (byte)(TextureNumber - 64);
                    methodBits = 0b10;
                }
                else if (TextureNumber >= 64 && TextureNumber <= 319)
                {
                    textureByte = (byte)(TextureNumber - 64);
                    methodBits = 0b11;
                }
            }

            // Calculate the rotation bits
            byte rotationBits = 0;
            switch (Rotation)
            {
                case 0:
                    rotationBits = 0b10;
                    break;
                case 90:
                    rotationBits = 0b01;
                    break;
                case 180:
                    rotationBits = 0b00;
                    break;
                case 270:
                    rotationBits = 0b11;
                    break;
            }

            // Combine rotation and method bits
            byte combinedByte = isDefaultTexture ? (byte)0x00 : (byte)((rotationBits << 2) | methodBits);

            // Store the sequence
            TileSequence[0] = textureByte;
            TileSequence[1] = combinedByte;
            TileSequence[2] = 0x00;
            TileSequence[3] = 0x00;
            TileSequence[4] = (byte)Height; // Store the height in the 5th byte
            TileSequence[5] = 0x00; // This is the 6th byte and can be used for other purposes if needed
        }
    }
}
