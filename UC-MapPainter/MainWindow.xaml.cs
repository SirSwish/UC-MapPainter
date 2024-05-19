using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace UC_MapPainter
{
    public partial class MainWindow : Window
    {
        private SelectedTextureWindow selectedTextureWindow;
        private TextureSelectionWindow textureSelectionWindow;
        private GridModel gridModel = new GridModel();
        private int selectedWorldNumber;
        private ScaleTransform scaleTransform = new ScaleTransform();

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            MainContentGrid.LayoutTransform = scaleTransform;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeSelectedTextureWindow();
            InitializeTextureSelectionWindow();
        }

        private void InitializeSelectedTextureWindow()
        {
            selectedTextureWindow = new SelectedTextureWindow();
            selectedTextureWindow.Left = 10;
            selectedTextureWindow.Top = 50;
            selectedTextureWindow.Show();
            selectedTextureWindow.Owner = this; // Set the owner after showing the window
        }

        private void InitializeTextureSelectionWindow()
        {
            textureSelectionWindow = new TextureSelectionWindow();
            textureSelectionWindow.SetSelectedTextureWindow(selectedTextureWindow);
            textureSelectionWindow.Left = this.Left + this.Width - textureSelectionWindow.Width - 10;
            textureSelectionWindow.Top = 50;
            textureSelectionWindow.Show();
            textureSelectionWindow.Owner = this; // Set the owner after showing the window
        }

        private async void NewMap_Click(object sender, RoutedEventArgs e)
        {
            var worldSelectionWindow = new WorldSelectionWindow();
            if (worldSelectionWindow.ShowDialog() == true)
            {
                string selectedWorld = worldSelectionWindow.SelectedWorld;
                selectedWorldNumber = ExtractWorldNumber(selectedWorld);

                ProgressBar.Visibility = Visibility.Visible;

                var generatingMapWindow = new GeneratingMapWindow();
                generatingMapWindow.Owner = this;
                generatingMapWindow.Show();

                await InitializeMapGridAsync(selectedWorld);

                generatingMapWindow.Close();
                ProgressBar.Visibility = Visibility.Collapsed;
                textureSelectionWindow.SetSelectedWorld(selectedWorld); // Set the selected world in the drop-down
                textureSelectionWindow.LockWorld();
                textureSelectionWindow.LoadWorldTextures(selectedWorld);
                SaveMenuItem.IsEnabled = true;
                ExportMenuItem.IsEnabled = true; // Enable the Export menu item
            }
        }

        private void LoadMap_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "IAM files (*.iam)|*.iam",
                Title = "Load Map"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                LoadMap(filePath);
            }
        }

        private void LoadMap(string filePath)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);

            // Determine the world number from the specified offset (0x7D4 from the end of the file)
            int worldNumberOffset = fileBytes.Length - 0x7D4;
            selectedWorldNumber = fileBytes[worldNumberOffset];

            // Validate the world number
            string selectedWorld = $"World {selectedWorldNumber}";
            if (!IsValidWorld(selectedWorldNumber))
            {
                MessageBox.Show("World not assigned to Map. Please select a world.", "Invalid World", MessageBoxButton.OK, MessageBoxImage.Warning);
                var worldSelectionWindow = new WorldSelectionWindow();
                if (worldSelectionWindow.ShowDialog() == true)
                {
                    selectedWorld = worldSelectionWindow.SelectedWorld;
                    selectedWorldNumber = ExtractWorldNumber(selectedWorld);
                }
                else
                {
                    return;
                }
            }

            textureSelectionWindow.SetSelectedWorld(selectedWorld);
            textureSelectionWindow.LockWorld();
            textureSelectionWindow.LoadWorldTextures(selectedWorld);

            // Load the cells
            ProgressBar.Visibility = Visibility.Visible;
            InitializeMapGridFromBytes(fileBytes);
            ProgressBar.Visibility = Visibility.Collapsed;

            SaveMenuItem.IsEnabled = true;
            ExportMenuItem.IsEnabled = true; // Enable the Export menu item
        }

        private async Task InitializeMapGridFromBytes(byte[] fileBytes)
        {
            MainContentGrid.Children.Clear();
            MainContentGrid.RowDefinitions.Clear();
            MainContentGrid.ColumnDefinitions.Clear();
            gridModel.Cells.Clear();

            for (int i = 0; i < 128; i++)
            {
                MainContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) });
                MainContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            }

            int index = 8; // Starting after the 8-byte header

            ProgressBar.Maximum = 128 * 128;
            ProgressBar.Value = 0;
            ProgressBar.Visibility = Visibility.Visible;

            for (int col = 0; col < 128; col++)
            {
                for (int row = 0; row < 128; row++)
                {
                    byte textureByte = fileBytes[index];
                    byte combinedByte = fileBytes[index + 1];
                    index += 6; // Skip to the next 6-byte sequence

                    var cell = new Border
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(0.5),
                        Width = 64,
                        Height = 64
                    };
                    cell.MouseLeftButtonDown += Cell_MouseLeftButtonDown;
                    Grid.SetRow(cell, 127 - row);
                    Grid.SetColumn(cell, 127 - col);
                    MainContentGrid.Children.Add(cell);

                    string textureType;
                    int textureNumber;
                    switch (GetMethod(combinedByte & 0x03))
                    {
                        case "A":
                            textureType = "world";
                            textureNumber = textureByte;
                            break;
                        case "B":
                            textureType = "shared";
                            textureNumber = textureByte + 256;
                            break;
                        case "C":
                            textureType = "prims";
                            textureNumber = (sbyte)textureByte + 64;
                            break;
                        case "D":
                            textureType = "prims";
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

                    gridModel.Cells.Add(new Cell
                    {
                        Row = 127 - row,
                        Column = 127 - col,
                        TextureType = textureType,
                        TextureNumber = textureNumber,
                        Rotation = (int)rotation
                    });

                    await PaintCell(cell, textureType, textureNumber, rotation);
                }
            }

            ProgressBar.Value = 128 * 128; // Ensure progress bar is complete
            ProgressBar.Visibility = Visibility.Collapsed;
        }

        private async Task InitializeMapGridAsync(string selectedWorld)
        {
            MainContentGrid.Children.Clear();
            MainContentGrid.RowDefinitions.Clear();
            MainContentGrid.ColumnDefinitions.Clear();
            gridModel.Cells.Clear();

            for (int i = 0; i < 128; i++)
            {
                MainContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) });
                MainContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            }

            string appBasePath = AppDomain.CurrentDomain.BaseDirectory;
            string initialTexturePath = Path.Combine(appBasePath, "Textures", $"world{selectedWorldNumber}", "tex000hi.bmp");

            var initialTexture = new ImageBrush
            {
                ImageSource = new BitmapImage(new Uri(initialTexturePath)),
                Stretch = Stretch.Fill
            };

            ProgressBar.Maximum = 128 * 128;
            ProgressBar.Value = 0;
            ProgressBar.Visibility = Visibility.Visible;

            for (int row = 0; row < 128; row++)
            {
                for (int col = 0; col < 128; col++)
                {
                    var cell = new Border
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(0.5),
                        Background = initialTexture,
                        Width = 64,
                        Height = 64
                    };
                    cell.MouseLeftButtonDown += Cell_MouseLeftButtonDown;
                    Grid.SetRow(cell, 127 - row);
                    Grid.SetColumn(cell, 127 - col);
                    MainContentGrid.Children.Add(cell);

                    gridModel.Cells.Add(new Cell
                    {
                        Row = 127 - row,
                        Column = 127 - col,
                        TextureType = "world",
                        TextureNumber = 0,
                        Rotation = 0
                    });

                    // Update progress every 100 cells
                    if ((row * 128 + col) % 100 == 0)
                    {
                        ProgressBar.Value = row * 128 + col;
                        await Task.Delay(1); // Yield to UI thread
                    }
                }
            }

            ProgressBar.Value = 128 * 128; // Ensure progress bar is complete
            ProgressBar.Visibility = Visibility.Collapsed;
        }

        private void Cell_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border cell && selectedTextureWindow != null)
            {
                var selectedTexture = selectedTextureWindow.SelectedTextureImage.Source;
                if (selectedTexture != null)
                {
                    var imageBrush = new ImageBrush
                    {
                        ImageSource = selectedTexture,
                        Stretch = Stretch.None,
                        AlignmentX = AlignmentX.Center,
                        AlignmentY = AlignmentY.Center
                    };

                    var rotateTransform = new RotateTransform(selectedTextureWindow.SelectedTextureRotation, 32, 32);

                    cell.Background = new VisualBrush
                    {
                        Visual = new Image
                        {
                            Source = selectedTexture,
                            RenderTransform = rotateTransform,
                            RenderTransformOrigin = new Point(0.5, 0.5),
                            Stretch = Stretch.Fill,
                            Width = 64,
                            Height = 64
                        }
                    };

                    int row = 127 - Grid.GetRow(cell);
                    int col = 127 - Grid.GetColumn(cell);

                    var cellData = gridModel.Cells.FirstOrDefault(c => c.Row == row && c.Column == col);
                    if (cellData != null)
                    {
                        bool isDefaultTexture = selectedTextureWindow.SelectedTextureNumber == 0 && selectedTextureWindow.SelectedTextureType == "world";
                        cellData.TextureType = selectedTextureWindow.SelectedTextureType;
                        cellData.TextureNumber = selectedTextureWindow.SelectedTextureNumber;
                        cellData.Rotation = selectedTextureWindow.SelectedTextureRotation;
                        cellData.UpdateTileSequence(isDefaultTexture);
                    }
                }
            }
        }

        private void MainContentGrid_MouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(MainContentGrid);

            int row = 127 - (int)(position.Y / 64);
            int col = 127 - (int)(position.X / 64);

            if (row >= 0 && row < 128 && col >= 0 && col < 128)
            {
                MousePositionLabel.Content = $"X: {col}, Y: {row}";
            }
        }

        private int ExtractWorldNumber(string world)
        {
            return int.Parse(new string(world.Where(char.IsDigit).ToArray()));
        }

        private bool IsValidWorld(int worldNumber)
        {
            var validWorlds = new HashSet<int> { 1, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 15, 16, 17, 18, 20 };
            return validWorlds.Contains(worldNumber);
        }

        private string GetMethod(int index)
        {
            return index switch
            {
                0 => "A",
                1 => "B",
                2 => "C",
                3 => "D",
                _ => null
            };
        }

        private async Task PaintCell(Border cell, string textureType, int textureNumber, double rotation)
        {
            string textureFolder = textureType == "world" ? $"world{selectedWorldNumber}" : textureType;
            string texturePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Textures", textureFolder, $"tex{textureNumber:D3}hi.bmp");
            if (File.Exists(texturePath))
            {
                var imageBrush = new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri(texturePath)),
                    Stretch = Stretch.None,
                    AlignmentX = AlignmentX.Center,
                    AlignmentY = AlignmentY.Center
                };

                var rotateTransform = new RotateTransform(rotation, 32, 32);

                cell.Background = new VisualBrush
                {
                    Visual = new Image
                    {
                        Source = new BitmapImage(new Uri(texturePath)),
                        RenderTransform = rotateTransform,
                        RenderTransformOrigin = new Point(0.5, 0.5),
                        Stretch = Stretch.Fill,
                        Width = 64,
                        Height = 64
                    }
                };
            }
        }

        private void SaveMap_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "IAM files (*.iam)|*.iam",
                Title = "Save Map As",
                FileName = "ExportedMap.iam"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string userFilePath = saveFileDialog.FileName;
                if (userFilePath.Length > 96)
                {
                    var result = MessageBox.Show(
                        "The file path exceeds the maximum length of 96 characters. Using the load map method of debug mode, the game cannot parse paths longer than 96 characters. Do you still want to save here?",
                        "File Path Too Long",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                string defaultFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Map", "default.iam");
                byte[] fileBytes = File.ReadAllBytes(defaultFilePath);

                // Overwrite the textureByte and combinedByte for each cell in column-major order
                int index = 8; // Starting after the 8-byte header
                foreach (var col in Enumerable.Range(0, 128))
                {
                    foreach (var row in Enumerable.Range(0, 128))
                    {
                        var cell = gridModel.Cells.FirstOrDefault(c => c.Row == row && c.Column == col);
                        if (cell != null)
                        {
                            bool isDefaultTexture = cell.TextureNumber == 0 && cell.TextureType == "world";
                            cell.UpdateTileSequence(isDefaultTexture);
                            fileBytes[index] = cell.TileSequence[0]; // textureByte
                            fileBytes[index + 1] = cell.TileSequence[1]; // combinedByte
                            // Skipping the remaining 4 bytes of the 6-byte sequence
                            index += 6;
                        }
                    }
                }

                // Save the world number at the specified offset (0x7D4 from the end of the file)
                int worldNumberOffset = fileBytes.Length - 0x7D4;
                fileBytes[worldNumberOffset] = (byte)selectedWorldNumber;

                // Save the modified file to the user-specified location
                File.WriteAllBytes(userFilePath, fileBytes);

                // Save the modified file to the "Exported" folder
                string exportFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exported");
                Directory.CreateDirectory(exportFolder);
                string exportFilePath = Path.Combine(exportFolder, Path.GetFileName(userFilePath));
                File.WriteAllBytes(exportFilePath, fileBytes);

                MessageBox.Show($"Map saved to {userFilePath} and {exportFilePath}", "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            scaleTransform.ScaleX *= 1.1;
            scaleTransform.ScaleY *= 1.1;
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            scaleTransform.ScaleX /= 1.1;
            scaleTransform.ScaleY /= 1.1;
        }

        private void ExportMapToBmp_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "BMP files (*.bmp)|*.bmp",
                Title = "Export Map to BMP",
                FileName = "ExportedMap.bmp"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                ExportMapToBmp(filePath);
            }
        }

        private void ExportMapToBmp(string filePath)
        {
            const int cellSize = 64;
            const int gridSize = 128;
            const int imageSize = cellSize * gridSize;

            var renderTargetBitmap = new RenderTargetBitmap(imageSize, imageSize, 96, 96, PixelFormats.Pbgra32);
            var drawingVisual = new DrawingVisual();

            using (var drawingContext = drawingVisual.RenderOpen())
            {
                foreach (var cell in gridModel.Cells)
                {
                    var texturePath = GetTexturePath(cell.TextureType, cell.TextureNumber);
                    if (File.Exists(texturePath))
                    {
                        var imageSource = new BitmapImage(new Uri(texturePath));
                        var rect = new Rect(cell.Column * cellSize, cell.Row * cellSize, cellSize, cellSize);

                        drawingContext.PushTransform(new RotateTransform(cell.Rotation, rect.X + cellSize / 2, rect.Y + cellSize / 2));
                        drawingContext.DrawImage(imageSource, rect);
                        drawingContext.Pop();
                    }
                }
            }

            renderTargetBitmap.Render(drawingVisual);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var bitmapEncoder = new BmpBitmapEncoder();
                bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
                bitmapEncoder.Save(fileStream);
            }

            MessageBox.Show($"Map exported to {filePath}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GetTexturePath(string textureType, int textureNumber)
        {
            string textureFolder = textureType == "world" ? $"world{selectedWorldNumber}" : textureType;
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Textures", textureFolder, $"tex{textureNumber:D3}hi.bmp");
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

    }
}
