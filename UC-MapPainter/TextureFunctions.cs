using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UC_MapPainter
{
    public class TextureFunctions
    {
        private GridModel gridModel;
        private int selectedWorldNumber;
        private MainWindow mainWindow;

        public TextureFunctions(GridModel gridModel, int selectedWorldNumber, MainWindow mainWindow)
        {
            this.gridModel = gridModel;
            this.selectedWorldNumber = selectedWorldNumber;
            this.mainWindow = mainWindow;
        }

        public async Task PaintCell(Border cell, string textureType, int textureNumber, double rotation, int selectedWorldNumber)
        {
            string textureFolder = textureType == "world" ? $"world{selectedWorldNumber}" : textureType;
            string texturePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Textures", textureFolder, $"tex{textureNumber:D3}hi.bmp");

            BitmapImage bitmapImage;
            if (File.Exists(texturePath))
            {
                bitmapImage = new BitmapImage(new Uri(texturePath));

                var rotateTransform = new RotateTransform(rotation, 32, 32);

                cell.Background = new VisualBrush
                {
                    Visual = new Image
                    {
                        Source = bitmapImage,
                        RenderTransform = rotateTransform,
                        RenderTransformOrigin = new Point(0.5, 0.5),
                        Stretch = Stretch.Fill,
                        Width = 64,
                        Height = 64
                    }
                };
            }
            else 
            {
                // Set the background color to white using SolidColorBrush
                cell.Background = new SolidColorBrush(Colors.White);
            }


        }

        public async Task DrawCells(byte[] fileBytes, int selectedWorldNumber)
        {
            mainWindow.MainContentGrid.Children.Clear();
            mainWindow.MainContentGrid.RowDefinitions.Clear();
            mainWindow.MainContentGrid.ColumnDefinitions.Clear();
            gridModel.Cells.Clear();

            for (int i = 0; i < 128; i++)
            {
                mainWindow.MainContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) });
                mainWindow.MainContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            }

            byte[] TexHeightData = Map.ReadTextureData(fileBytes);
            int index = 0;

            mainWindow.ProgressBar.Maximum = 128 * 128;
            mainWindow.ProgressBar.Value = 0;
            mainWindow.ProgressBar.Visibility = System.Windows.Visibility.Visible;

            for (int col = 0; col < 128; col++) // Iterate columns first
            {
                for (int row = 0; row < 128; row++) // Then iterate rows
                {
                    byte textureByte = TexHeightData[index];
                    byte combinedByte = TexHeightData[index + 1];
                    sbyte heightByte = (sbyte)TexHeightData[index + 4]; // Convert to signed byte
                    byte[] tileBytes = new byte[6];
                    Array.Copy(fileBytes, index, tileBytes, 0, 6);
                    index += 6; // Skip to the next 6-byte sequence

                    var cell = new Border
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(0.5),
                        Width = 64,
                        Height = 64
                    };

                    cell.MouseLeftButtonDown += mainWindow.Cell_MouseLeftButtonDown;
                    cell.MouseRightButtonDown += mainWindow.Cell_MouseRightButtonDown;

                    // There must be a cleaner way to achieve this...
                    cell.MouseDown += mainWindow.Cell_MouseMiddleButtonDown;
                    cell.MouseMove += mainWindow.Cell_MouseMove;

                    Grid.SetRow(cell, 127 - row); // Correct mapping for row
                    Grid.SetColumn(cell, 127 - col); // Correct mapping for column

                    mainWindow.MainContentGrid.Children.Add(cell);

                    string textureType;
                    int textureNumber;
                    switch (combinedByte & 0x03)
                    {
                        case 0:
                            textureType = "world";
                            textureNumber = textureByte;
                            break;
                        case 1:
                            textureType = "shared";
                            textureNumber = textureByte + 256;
                            break;
                        case 2:
                            textureType = "shared/prims";
                            textureNumber = (sbyte)textureByte + 64;
                            break;
                        case 3:
                            textureType = "shared/prims";
                            textureNumber = textureByte + 64;
                            break;
                        default:
                            continue;
                    }

                    int rotationIndex = (combinedByte >> 2) % 4;
                    double rotation = rotationIndex switch
                    {
                        0 => 180.0,
                        1 => 90.0,
                        2 => 0.0,
                        3 => 270.0,
                        _ => 0.0
                    };

                    // Update gridModel.Cells
                    var cellData = new Cell(mainWindow)
                    {
                        Row = row,
                        Column = col,
                        TextureType = textureType,
                        TextureNumber = textureNumber,
                        Rotation = (int)rotation,
                        Height = heightByte, // Assign the height from the loaded byte as signed
                        TileSequence = tileBytes // Store the tile bytes
                    };
                    gridModel.Cells.Add(cellData);


                    if (mainWindow.currentEditMode == "Height")
                    {
                        var textBlock = new TextBlock
                        {
                            Text = cellData.Height.ToString(),
                            Foreground = Brushes.Red,
                            FontWeight = FontWeights.Bold,
                            Margin = new Thickness(0, 0, 5, 5),
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Bottom
                        };
                        cell.Child = textBlock;
                    }
                    else
                    {
                        cell.Child = null;
                    }

                    // Use the cell data to set the cell background
                    await PaintCell(cell, textureType, textureNumber, rotation, selectedWorldNumber);

                    // Update progress every 128 cells
                    if ((col * 128 + row) % 128 == 0)
                    {
                        mainWindow.ProgressBar.Value = col * 128 + row;
                        await Task.Delay(1); // Yield to UI thread
                    }
                }
            }

            mainWindow.ProgressBar.Value = 128 * 128; // Ensure progress bar is complete
            mainWindow.ProgressBar.Visibility = System.Windows.Visibility.Collapsed;
        }

        public string GetTexturePath(string textureType, int textureNumber, byte[] modifiedFileBytes)
        {
            int saveType = Map.ReadMapSaveType(modifiedFileBytes);
            int mapWorld = Map.ReadTextureWorld(modifiedFileBytes,saveType);
            string textureFolder = textureType == "world" ? $"world{mapWorld}" : textureType;
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Textures", textureFolder, $"tex{textureNumber:D3}hi.bmp");
        }

        public bool IsValidWorld(int worldNumber)
        {
            var validWorlds = new HashSet<int> { 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 15, 16, 17, 18, 20 };
            return validWorlds.Contains(worldNumber);
        }

        public int FindCellTexOffset(int x, int z)
        {

            return (x * 128 + z) * 6;
        }
    }
}
