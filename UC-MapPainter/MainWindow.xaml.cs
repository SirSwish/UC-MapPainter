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
using System.Text;
using Microsoft.Win32;
using System.Windows.Shapes;
using System.Reflection.Metadata;
using Path = System.IO.Path;

namespace UC_MapPainter
{
    public partial class MainWindow : Window
    {
        private string loadedFilePath;
        private TextureSelectionWindow textureSelectionWindow;
        private GridModel gridModel = new GridModel();
        private int selectedWorldNumber;
        private ScaleTransform scaleTransform = new ScaleTransform();
        private string selectedTextureType;
        private int selectedTextureNumber;
        private int selectedTextureRotation = 0;
        public bool IsEditMode { get; private set; }
        private List<ObOb> obObList; // Add this line to define obObList
        private byte[] fileBytes; // Add this line
        private bool isTextureSelectionLocked = false;
        private string lockedWorld = null;
        private Canvas MapWhoGridCanvas = new Canvas();
        private bool isMapWhoGridVisible = false;
        private PrimSelectionWindow primSelectionWindow;
        private int selectedPrimNumber = -1;
        private string currentEditMode;





        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            MainContentGrid.LayoutTransform = scaleTransform;
            MainContentGrid.MouseMove += MainContentGrid_MouseMove;
            MapWhoGridCanvas.IsHitTestVisible = false;
            MainContentGrid.Children.Add(MapWhoGridCanvas);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeTextureSelectionWindow();
            SetEditMode("Textures");
        }

        private void PrimSelection_Click(object sender, RoutedEventArgs e)
        {
            InitializePrimSelectionWindow();
        }

        private void InitializePrimSelectionWindow()
        {
            if (primSelectionWindow == null || !primSelectionWindow.IsLoaded)
            {
                primSelectionWindow = new PrimSelectionWindow();
                primSelectionWindow.SetMainWindow(this);
                primSelectionWindow.Left = this.Left + this.Width - primSelectionWindow.Width - 10;
                primSelectionWindow.Top = 50;
                primSelectionWindow.Closed += PrimSelectionWindow_Closed;
                primSelectionWindow.Show();
                primSelectionWindow.Owner = this; // Set the owner after showing the window
                PrimSelectionMenuItem.IsEnabled = false; // Disable the menu item
            }
        }

        private void PrimSelectionWindow_Closed(object sender, EventArgs e)
        {
            PrimSelectionMenuItem.IsEnabled = true; // Enable the menu item when the window is closed
        }

        private void InitializeTextureSelectionWindow()
        {
            if (textureSelectionWindow == null || !textureSelectionWindow.IsLoaded)
            {
                textureSelectionWindow = new TextureSelectionWindow();
                textureSelectionWindow.SetMainWindow(this);
                textureSelectionWindow.Left = this.Left + this.Width - textureSelectionWindow.Width - 10;
                textureSelectionWindow.Top = 50;
                textureSelectionWindow.Closed += TextureSelectionWindow_Closed;
                textureSelectionWindow.Show();
                textureSelectionWindow.Owner = this; // Set the owner after showing the window
                TextureSelectionMenuItem.IsEnabled = false; // Disable the menu item

                // Restore lock status and selected world
                if (isTextureSelectionLocked && lockedWorld != null)
                {
                    textureSelectionWindow.SetSelectedWorld(lockedWorld);
                    textureSelectionWindow.LockWorld();
                    textureSelectionWindow.LoadWorldTextures(lockedWorld);
                }
            }
        }

        private void TextureSelectionWindow_Closed(object sender, EventArgs e)
        {
            TextureSelectionMenuItem.IsEnabled = true; // Enable the menu item when the window is closed

            // Track lock status and selected world
            isTextureSelectionLocked = textureSelectionWindow.IsWorldLocked;
            lockedWorld = textureSelectionWindow.GetSelectedWorld();
        }

        private void TextureSelection_Click(object sender, RoutedEventArgs e)
        {
            InitializeTextureSelectionWindow();
        }

        private async void NewMap_Click(object sender, RoutedEventArgs e)
        {
            SetEditMode("Textures");
            var worldSelectionWindow = new WorldSelectionWindow();
            if (worldSelectionWindow.ShowDialog() == true)
            {
                loadedFilePath = null; // Reset the loaded file path when creating a new map
                UpdateWindowTitle("Untitled.iam"); // Update title to "New Map.iam"
                string selectedWorld = worldSelectionWindow.SelectedWorld;
                selectedWorldNumber = ExtractWorldNumber(selectedWorld);

                ProgressBar.Visibility = Visibility.Visible;

                var generatingMapWindow = new GeneratingMapWindow();
                generatingMapWindow.Owner = this;
                generatingMapWindow.Show();

                // Clear the arrays
                gridModel.ObObArray.Clear();
                gridModel.MapWhoArray.Clear();

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
                LoadMapAsync(filePath);
            }
        }

        private async void LoadMapAsync(string filePath)
        {
            loadedFilePath = filePath; // Store the loaded file path
            UpdateWindowTitle(Path.GetFileName(filePath));
            fileBytes = File.ReadAllBytes(filePath); // Store file bytes

            // Clear the arrays
            gridModel.ObObArray.Clear();
            gridModel.MapWhoArray.Clear();


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

            // Show Generating Map window and progress bar
            ProgressBar.Visibility = Visibility.Visible;
            var generatingMapWindow = new GeneratingMapWindow();
            generatingMapWindow.Owner = this;
            generatingMapWindow.Show();

            // Extract OB_Ob and MapWho arrays
            int saveType = BitConverter.ToInt32(fileBytes, 0);
            int obSize1 = BitConverter.ToInt32(fileBytes, 4);
            await InitializeObObAndMapWhoAsync(fileBytes, saveType, obSize1);

            // Load the cells
            await InitializeMapGridFromBytesAsync(fileBytes);

            generatingMapWindow.Close();
            ProgressBar.Visibility = Visibility.Collapsed;

            SaveMenuItem.IsEnabled = true;
            ExportMenuItem.IsEnabled = true; // Enable the Export menu item
            SetEditMode("Textures");

        }





        private async Task InitializeMapGridFromBytesAsync(byte[] fileBytes)
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

            for (int col = 0; col < 128; col++) // Iterate columns first
            {
                for (int row = 0; row < 128; row++) // Then iterate rows
                {
                    byte textureByte = fileBytes[index];
                    byte combinedByte = fileBytes[index + 1];
                    sbyte heightByte = (sbyte)fileBytes[index + 4]; // Convert to signed byte
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
                    cell.MouseLeftButtonDown += Cell_MouseLeftButtonDown;
                    cell.MouseRightButtonDown += Cell_MouseRightButtonDown; // Add right-click event handler
                    Grid.SetRow(cell, 127 - row); // Correct mapping for row
                    Grid.SetColumn(cell, 127 - col); // Correct mapping for column
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
                            textureType = "shared/prims";
                            textureNumber = (sbyte)textureByte + 64;
                            break;
                        case "D":
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
                    var cellData = new Cell
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

                    // Use the cell data to set the cell background
                    await PaintCell(cell, textureType, textureNumber, rotation);

                    // Update progress every 128 cells
                    if ((col * 128 + row) % 128 == 0)
                    {
                        ProgressBar.Value = col * 128 + row;
                        await Task.Delay(1); // Yield to UI thread
                    }
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
            string initialTexturePath = System.IO.Path.Combine(appBasePath, "Textures", $"world{selectedWorldNumber}", "tex000hi.bmp");

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
                    cell.MouseRightButtonDown += Cell_MouseRightButtonDown; // Add right-click event handler
                    Grid.SetRow(cell, 127 - row);
                    Grid.SetColumn(cell, 127 - col);
                    MainContentGrid.Children.Add(cell);

                    gridModel.Cells.Add(new Cell
                    {
                        Row = 127 - row,
                        Column = 127 - col,
                        TextureType = "world",
                        TextureNumber = 0,
                        Rotation = 0,
                        Height = 0 // Initialize height to 0 for new maps
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
            if (sender is Border cell)
            {
                var position = e.GetPosition(MainContentGrid);
                int pixelX = (int)position.X;
                int pixelZ = (int)position.Y;

                if (currentEditMode == "Prims" && selectedPrimNumber != -1)
                {
                    var ellipse = new Ellipse
                    {
                        Width = 15,
                        Height = 15,
                        Fill = Brushes.Blue,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };

                    Canvas.SetLeft(ellipse, pixelX - ellipse.Width / 2);
                    Canvas.SetTop(ellipse, pixelZ - ellipse.Height / 2);

                    OverlayGrid.Children.Add(ellipse);

                    // Reset the selected prim
                    selectedPrimNumber = -1;
                    if (primSelectionWindow != null && primSelectionWindow.IsLoaded)
                    {
                        primSelectionWindow.UpdateSelectedPrimImage(-1); // Clear the selected prim image
                    }
                }
                else if (currentEditMode == "Textures")
                {
                    int row = 127 - Grid.GetRow(cell);
                    int col = 127 - Grid.GetColumn(cell);

                    var cellData = gridModel.Cells.FirstOrDefault(c => c.Row == row && c.Column == col);
                    if (cellData != null)
                    {
                        if (SelectedTextureImage.Source != null)
                        {
                            var imageBrush = new ImageBrush
                            {
                                ImageSource = SelectedTextureImage.Source,
                                Stretch = Stretch.None,
                                AlignmentX = AlignmentX.Center,
                                AlignmentY = AlignmentY.Center
                            };

                            var rotateTransform = new RotateTransform(selectedTextureRotation, 32, 32);

                            cell.Background = new VisualBrush
                            {
                                Visual = new Image
                                {
                                    Source = SelectedTextureImage.Source,
                                    RenderTransform = rotateTransform,
                                    RenderTransformOrigin = new Point(0.5, 0.5),
                                    Stretch = Stretch.Fill,
                                    Width = 64,
                                    Height = 64
                                }
                            };

                            bool isDefaultTexture = selectedTextureNumber == 0 && selectedTextureType == "world";
                            cellData.TextureType = selectedTextureType;
                            cellData.TextureNumber = selectedTextureNumber;
                            cellData.Rotation = selectedTextureRotation;
                            cellData.UpdateTileSequence(isDefaultTexture);
                        }
                    }
                }
            }
        }



        private void Cell_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed && sender is Border cell)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    int row = 127 - Grid.GetRow(cell);
                    int col = 127 - Grid.GetColumn(cell);

                    var cellData = gridModel.Cells.FirstOrDefault(c => c.Row == row && c.Column == col);
                    if (cellData != null)
                    {
                        string textureFolder = cellData.TextureType == "world" ? $"world{selectedWorldNumber}" : cellData.TextureType;
                        string texturePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Textures", textureFolder, $"tex{cellData.TextureNumber:D3}hi.bmp");

                        string debugMessage = $"Cell Debug Information:\n" +
                                              $"X: {col}\n" +
                                              $"Y: {row}\n" +
                                              $"Texture File Path: {texturePath}\n" +
                                              $"Texture Type: {cellData.TextureType}\n" +
                                              $"Rotation: {cellData.Rotation}°\n" +
                                              $"Height: {cellData.Height}\n" +
                                              $"Tile Bytes: {BitConverter.ToString(cellData.TileSequence)}"; // Use the stored tile bytes
                        MessageBox.Show(debugMessage, "Cell Debug Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else if (IsEditMode)
                {
                    int row = 127 - Grid.GetRow(cell);
                    int col = 127 - Grid.GetColumn(cell);

                    var cellData = gridModel.Cells.FirstOrDefault(c => c.Row == row && c.Column == col);
                    if (cellData != null)
                    {
                        int decrement = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? 10 : 1;
                        cellData.Height = Math.Max(cellData.Height - decrement, -127);
                        if (cell.Child is TextBlock textBlock)
                        {
                            textBlock.Text = cellData.Height.ToString();
                        }
                        else
                        {
                            cell.Child = new TextBlock
                            {
                                Text = cellData.Height.ToString(),
                                Foreground = Brushes.Red,
                                FontWeight = FontWeights.Bold,
                                Margin = new Thickness(0, 0, 5, 5),
                                HorizontalAlignment = HorizontalAlignment.Right,
                                VerticalAlignment = VerticalAlignment.Bottom
                            };
                        }
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
                MousePositionLabel.Content = $"X: {col}, Z: {row}";
            }

            double pixelX = 8192 - position.X;
            double pixelZ = 8192 - position.Y;
            PixelPositionLabel.Content = $"Pixel: ({pixelX:F0}, {pixelZ:F0})";
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
                3 => "D"
            };
        }

        private async Task PaintCell(Border cell, string textureType, int textureNumber, double rotation)
        {
            string textureFolder = textureType == "world" ? $"world{selectedWorldNumber}" : textureType;
            string texturePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Textures", textureFolder, $"tex{textureNumber:D3}hi.bmp");

            BitmapImage bitmapImage;
            if (File.Exists(texturePath))
            {
                bitmapImage = new BitmapImage(new Uri(texturePath));
            }
            else
            {
                // Load the resource image
                Uri resourceUri = new Uri("pack://application:,,,/Images/noTile.bmp", UriKind.Absolute);
                bitmapImage = new BitmapImage(resourceUri);
            }

            var imageBrush = new ImageBrush
            {
                ImageSource = bitmapImage,
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };

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

                // Use the loaded file path as the template if a map was loaded, otherwise use default.iam
                string templateFilePath = loadedFilePath ?? System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Map", "default.iam");
                byte[] fileBytes = File.ReadAllBytes(templateFilePath);

                // Determine the save order based on whether the default template or a loaded template is used
                if (loadedFilePath == null)
                {
                    // Saving using the default template
                    int index = 8; // Starting after the 8-byte header
                    for (int col = 0; col < 128; col++) // Left to right columns
                    {
                        for (int row = 0; row < 128; row++) // Bottom to top rows
                        {
                            var cell = gridModel.Cells.FirstOrDefault(c => c.Row == row && c.Column == col);
                            if (cell != null)
                            {
                                bool isDefaultTexture = cell.TextureNumber == 0 && cell.TextureType == "world";
                                cell.UpdateTileSequence(isDefaultTexture);
                                fileBytes[index] = cell.TileSequence[0]; // textureByte
                                fileBytes[index + 1] = cell.TileSequence[1]; // combinedByte
                                fileBytes[index + 4] = (byte)cell.Height; // height

                                // Add debug statement for cell 0,0
                                if (row == 0 && col == 0)
                                {
                                    string debugMessage = $"Saving cell (0,0):\nTextureByte: {cell.TileSequence[0]:X2}\nCombinedByte: {cell.TileSequence[1]:X2}\nHeight: {cell.Height}";
                                    MessageBox.Show(debugMessage, "Debug Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                }

                                // Skipping the remaining 4 bytes of the 6-byte sequence
                                index += 6;
                            }
                        }
                    }
                }
                else
                {
                    // Saving using a loaded template (reverse order)
                    int index = 8; // Starting after the 8-byte header
                    for (int col = 0; col < 128; col++) // Left to right columns
                    {
                        for (int row = 0; row < 128; row++) // Bottom to top rows
                        {
                            var cell = gridModel.Cells.FirstOrDefault(c => c.Row == row && c.Column == col);
                            if (cell != null)
                            {
                                bool isDefaultTexture = cell.TextureNumber == 0 && cell.TextureType == "world";
                                cell.UpdateTileSequence(isDefaultTexture);
                                fileBytes[index] = cell.TileSequence[0]; // textureByte
                                fileBytes[index + 1] = cell.TileSequence[1]; // combinedByte
                                fileBytes[index + 4] = (byte)cell.Height; // height

                                // Skipping the remaining 4 bytes of the 6-byte sequence
                                index += 6;
                            }
                        }
                    }
                }

                // Save the world number at the specified offset (0x7D4 from the end of the file)
                int worldNumberOffset = fileBytes.Length - 0x7D4;
                fileBytes[worldNumberOffset] = (byte)selectedWorldNumber;

                // Save the modified file to the user-specified location
                File.WriteAllBytes(userFilePath, fileBytes);

                // Save the modified file to the "Exported" folder
                string exportFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exported");
                Directory.CreateDirectory(exportFolder);
                string exportFilePath = System.IO.Path.Combine(exportFolder, System.IO.Path.GetFileName(userFilePath));
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

                        drawingContext.PushTransform(new RotateTransform(cell.Rotation + 180, rect.X + cellSize / 2, rect.Y + cellSize / 2));
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

        public void UpdateSelectedTexture(ImageSource newTexture, string type, int number)
        {
            SelectedTextureImage.Source = newTexture;
            selectedTextureType = type;
            selectedTextureNumber = number;
            selectedTextureRotation = 0; // Reset rotation when a new texture is selected

            // Apply rotation
            ApplyRotation();
        }

        private void RotateLeft_Click(object sender, RoutedEventArgs e)
        {
            selectedTextureRotation = (selectedTextureRotation - 90) % 360;
            if (selectedTextureRotation < 0)
            {
                selectedTextureRotation += 360;
            }
            ApplyRotation();
        }

        private void RotateRight_Click(object sender, RoutedEventArgs e)
        {
            selectedTextureRotation = (selectedTextureRotation + 90) % 360;
            ApplyRotation();
        }

        private void ApplyRotation()
        {
            var transform = new RotateTransform(selectedTextureRotation)
            {
                CenterX = SelectedTextureImage.Width / 2,
                CenterY = SelectedTextureImage.Height / 2
            };
            SelectedTextureImage.RenderTransform = transform;
        }

        private string GetTexturePath(string textureType, int textureNumber)
        {
            string textureFolder = textureType == "world" ? $"world{selectedWorldNumber}" : textureType;
            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Textures", textureFolder, $"tex{textureNumber:D3}hi.bmp");
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void EditTextureButton_Click(object sender, RoutedEventArgs e)
        {
            SetEditMode("Textures");
        }

        private void EditHeightButton_Click(object sender, RoutedEventArgs e)
        {
            SetEditMode("Height");
        }

        private void SetEditMode(string mode)
        {
            currentEditMode = mode;

            EditTextureButton.IsEnabled = true;
            EditHeightButton.IsEnabled = true;
            EditBuildingsButton.IsEnabled = true;
            EditPrimsButton.IsEnabled = true;

            switch (mode)
            {
                case "Textures":
                    EditTextureButton.IsEnabled = false;
                    IsEditMode = false;
                    OverlayGrid.Visibility = Visibility.Collapsed;
                    break;
                case "Height":
                    EditHeightButton.IsEnabled = false;
                    IsEditMode = true;
                    OverlayGrid.Visibility = Visibility.Collapsed;
                    ClearSelectedTexture(); // Clear the selected texture
                    break;
                case "Buildings":
                    EditBuildingsButton.IsEnabled = false;
                    IsEditMode = false;
                    OverlayGrid.Visibility = Visibility.Collapsed;
                    ClearSelectedTexture(); // Clear the selected texture
                    break;
                case "Prims":
                    EditPrimsButton.IsEnabled = false;
                    IsEditMode = false;
                    OverlayGrid.Visibility = Visibility.Visible;
                    ClearSelectedTexture(); // Clear the selected texture
                    InitializePrimSelectionWindow(); // Open the Prim Selection Window
                    break;
                default:
                    break;
            }

            UpdateCellDisplayAsync();
        }



        private async void UpdateCellDisplayAsync()
        {
            var generatingMapWindow = new GeneratingMapWindow();
            generatingMapWindow.Owner = this;
            generatingMapWindow.Show();

            ProgressBar.Visibility = Visibility.Visible;

            ProgressBar.Maximum = 128 * 128;
            ProgressBar.Value = 0;

            int cellCount = 0;

            foreach (var cell in MainContentGrid.Children.OfType<Border>())
            {
                var cellData = gridModel.Cells.FirstOrDefault(c => Grid.GetRow(cell) == 127 - c.Row && Grid.GetColumn(cell) == 127 - c.Column);
                if (cellData != null)
                {
                    if (IsEditMode)
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
                }

                cellCount++;

                // Update progress every 128 cells
                if (cellCount % 128 == 0)
                {
                    ProgressBar.Value = cellCount;
                    await Task.Delay(1); // Yield to UI thread
                }
            }

            ProgressBar.Value = 128 * 128; // Ensure progress bar is complete
            ProgressBar.Visibility = Visibility.Collapsed;

            generatingMapWindow.Close();
        }
        private void ShowScrollablePopup(string title, string content)
        {
            var textBox = new TextBox
            {
                Text = content,
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.Wrap
            };

            var scrollViewer = new ScrollViewer
            {
                Content = textBox,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Width = 400,
                Height = 300
            };

            var window = new Window
            {
                Title = title,
                Content = scrollViewer,
                Width = 450,
                Height = 350
            };

            window.ShowDialog();
        }

        private void ShowObObInfo()
        {
            var sb = new StringBuilder();
            foreach (var ob in gridModel.ObObArray)
            {
                sb.AppendLine($"Y: {ob.Y}, X: {ob.X}, Z: {ob.Z}, Prim: {ob.Prim} ({ob.DisplayName}), Yaw: {ob.Yaw}, Flags: {ob.Flags}, InsideIndex: {ob.InsideIndex}");
            }

            ShowScrollablePopup("Ob_Ob Information", sb.ToString());
        }

        private void ShowMapWhoInfo()
        {
            var sb = new StringBuilder();
            foreach (var mw in gridModel.MapWhoArray)
            {
                sb.AppendLine($"Index: {mw.Index}, Num: {mw.Num}");
            }

            ShowScrollablePopup("MapWho Information", sb.ToString());
        }


        private async Task InitializeObObAndMapWhoAsync(byte[] fileBytes, int saveType, int obSize1)
        {
            gridModel.ObObArray.Clear();
            gridModel.MapWhoArray.Clear();

            int fileSize = fileBytes.Length;
            int size = fileSize - 12;

            if (saveType >= 25)
            {
                size -= 2000;  // Adjust for texture data
            }

            size -= obSize1;  // Subtract the object size
            int obObUptoOffset = size + 8;
            int obObUpto = BitConverter.ToInt32(fileBytes, obObUptoOffset);

            // Read OB_Ob array
            for (int i = 0; i < obObUpto; i++)
            {
                int index = obObUptoOffset + 4 + (i * 8);
                var obOb = new ObOb
                {
                    Y = BitConverter.ToInt16(fileBytes, index),
                    X = fileBytes[index + 2],
                    Z = fileBytes[index + 3],
                    Prim = fileBytes[index + 4],
                    Yaw = fileBytes[index + 5],
                    Flags = fileBytes[index + 6],
                    InsideIndex = fileBytes[index + 7]
                };
                gridModel.ObObArray.Add(obOb);
            }

            // Read OB_Mapwho array
            int mapWhoOffset = obObUptoOffset + 4 + (obObUpto * 8);
            for (int i = 0; i < 32 * 32; i++)
            {
                int index = mapWhoOffset + (i * 2);
                ushort cell = BitConverter.ToUInt16(fileBytes, index);
                var mapWho = new MapWho
                {
                    Index = cell & 0x07FF,
                    Num = (cell >> 11) & 0x1F
                };
                gridModel.MapWhoArray.Add(mapWho);
            }
        }

        private void ShowObObInfo_Click(object sender, RoutedEventArgs e)
        {
            ShowObObInfo();
        }

        private void ShowMapWhoInfo_Click(object sender, RoutedEventArgs e)
        {
            ShowMapWhoInfo();
        }

        private void EditPrimsButton_Click(object sender, RoutedEventArgs e)
        {
            SetEditMode("Prims");
            InitializePrimSelectionWindow();
            DrawPrims();
        }

        private void DrawPrims()
        {
            OverlayGrid.Children.Clear(); // Clear existing objects
            string appBasePath = AppDomain.CurrentDomain.BaseDirectory;
            string topPrimsFolder = Path.Combine(appBasePath, "Prims", "TopPrims");

            for (int mapWhoIndex = 0; mapWhoIndex < gridModel.MapWhoArray.Count; mapWhoIndex++)
            {
                var mapWho = gridModel.MapWhoArray[mapWhoIndex];
                int mapWhoRow = mapWhoIndex / 32;
                int mapWhoCol = mapWhoIndex % 32;

                // Iterate through the number of objects in the MapWho cell
                for (int i = 0; i < mapWho.Num; i++)
                {
                    var ob = gridModel.ObObArray[mapWho.Index + i];

                    // Calculate the object's position within the MapWho cell
                    int relativeX = ob.X & 0xFF;
                    int relativeZ = ob.Z & 0xFF;

                    // Calculate the start pixel coordinates of the MapWho cell within the 8192x8192 grid
                    int startX = 8192 - (mapWhoRow * 256);
                    int startZ = 8192 - (mapWhoCol * 256);

                    // Calculate the overall pixel coordinates within the 8192x8192 grid
                    int pixelX = startX - relativeX;
                    int pixelZ = startZ - relativeZ;

                    // Calculate the global tile coordinates within the 128x128 grid
                    int globalTileX = mapWhoCol * 4 + (relativeX / 64);
                    int globalTileZ = mapWhoRow * 4 + (relativeZ / 64);

                    // Calculate the correct orientation
                    int finalPixelX = pixelX;
                    int finalPixelZ = pixelZ;

                    // Load the corresponding prim image from the TopPrims folder
                    string primImagePath = Path.Combine(topPrimsFolder, $"{ob.Prim}.png");
                    if (File.Exists(primImagePath))
                    {
                        var primImage = new Image
                        {
                            Source = new BitmapImage(new Uri(primImagePath))
                        };

                        // Apply rotation based on the Yaw property
                        double rotationAngle = -((ob.Yaw / 255.0) * 360);
                        var rotateTransform = new RotateTransform(rotationAngle);
                        primImage.RenderTransform = rotateTransform;
                        primImage.RenderTransformOrigin = new Point(0.5, 0.5); // Center the rotation

                        // Ensure the image is measured and arranged before positioning
                        primImage.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        primImage.Arrange(new Rect(0, 0, primImage.DesiredSize.Width, primImage.DesiredSize.Height));

                        // Set the position of the image within the 8192x8192 grid, centered at the specified point
                        Canvas.SetLeft(primImage, finalPixelX - primImage.DesiredSize.Width / 2);
                        Canvas.SetTop(primImage, finalPixelZ - primImage.DesiredSize.Height / 2);
                        OverlayGrid.Children.Add(primImage);
                        Canvas.SetZIndex(primImage, 0); // Ensure image is below ellipses
                    }

                    var ellipse = new Ellipse
                    {
                        Width = 15,
                        Height = 15,
                        Fill = Brushes.Red,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };

                    // Set the position of the ellipse within the 8192x8192 grid
                    Canvas.SetLeft(ellipse, finalPixelX - ellipse.Width / 2);
                    Canvas.SetTop(ellipse, finalPixelZ - ellipse.Height / 2);

                    // Ensure the ellipse is on top
                    Canvas.SetZIndex(ellipse, 1);

                    // Add click event handler for the ellipse
                    ellipse.MouseLeftButtonDown += (s, e) => ShowPrimInfo(ob, mapWhoIndex, mapWhoRow, mapWhoCol, relativeX, relativeZ, globalTileX, globalTileZ, finalPixelX, finalPixelZ);

                    // Ensure the ellipse is added after the image, making it on top
                    OverlayGrid.Children.Add(ellipse);
                }
            }
        }


        private void ShowPrimInfo(ObOb ob, int mapWhoIndex, int mapWhoRow, int mapWhoCol, int relativeX, int relativeZ, int globalTileX, int globalTileZ, int pixelX, int pixelZ)
        {
            string objectInfo = $"MapWho Index: {mapWhoIndex}\n" +
                                $"MapWho Cell: [{mapWhoRow}, {mapWhoCol}]\n" +
                                $"X Position in MapWho: {relativeX}\n" +
                                $"Z Position in MapWho: {relativeZ}\n" +
                                $"Y: {ob.Y}\n" +
                                $"Prim: {ob.Prim} ({ob.DisplayName})\n" +
                                $"Yaw: {ob.Yaw}\n" +
                                $"Flags: {ob.Flags}\n" +
                                $"InsideIndex: {ob.InsideIndex}\n" +
                                $"Calculated Tile Position: TileX = {globalTileX}, TileZ = {globalTileZ}\n" +
                                $"Calculated Pixel Position: PixelX = {8192 - pixelX}, PixelZ = {8192 - pixelZ}";
            MessageBox.Show(objectInfo, "Prim Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void ClearSelectedTexture()
        {
            SelectedTextureImage.Source = null;
            selectedTextureType = string.Empty;
            selectedTextureNumber = -1;
            selectedTextureRotation = 0;
        }

        private void UpdateWindowTitle(string fileName)
        {
            this.Title = $"Urban Chaos Map Editor - {fileName}";
        }

        private void DrawMapWhoGrid()
        {
            MapWhoGridCanvas.Children.Clear();

            for (int i = 0; i <= 128; i += 4)
            {
                // Vertical lines
                var verticalLine = new Line
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 3,
                    X1 = i * 64,
                    Y1 = 0,
                    X2 = i * 64,
                    Y2 = 128 * 64
                };
                MapWhoGridCanvas.Children.Add(verticalLine);

                // Horizontal lines
                var horizontalLine = new Line
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 1,
                    X1 = 0,
                    Y1 = i * 64,
                    X2 = 128 * 64,
                    Y2 = i * 64
                };
                MapWhoGridCanvas.Children.Add(horizontalLine);
            }

            // Add labels
            for (int row = 0; row < 32; row++)
            {
                for (int col = 0; col < 32; col++)
                {
                    int index = (31 - row) * 32 + (31 - col); // Reverse the index calculation
                    var label = new TextBlock
                    {
                        Text = $"Index: {index}\nRow: {31 - row}\nCol: {31 - col}",
                        Foreground = Brushes.Red,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Background = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)) // Semi-transparent background
                    };
                    Canvas.SetLeft(label, col * 4 * 64 + 2); // Adjust position slightly to avoid overlap with the grid lines
                    Canvas.SetTop(label, row * 4 * 64 + 2);
                    MapWhoGridCanvas.Children.Add(label);
                }
            }

            EnsureMapWhoGridCanvasOnTop();
        }

        private void ToggleMapWhoGridMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (isMapWhoGridVisible)
            {
                MapWhoGridCanvas.Children.Clear();
                ToggleMapWhoGridMenuItem.Header = "Draw MapWho Grid";
            }
            else
            {
                DrawMapWhoGrid();
                ToggleMapWhoGridMenuItem.Header = "Hide MapWho Grid";
            }
            isMapWhoGridVisible = !isMapWhoGridVisible;
        }
        private void EnsureMapWhoGridCanvasOnTop()
        {
            MainContentGrid.Children.Remove(MapWhoGridCanvas);
            MainContentGrid.Children.Add(MapWhoGridCanvas);
        }
        public void UpdateSelectedPrim(int primNumber)
        {
            selectedPrimNumber = primNumber;
            // If there is a SelectedPrimImage control in PrimSelectionWindow, update it
            if (primSelectionWindow != null && primSelectionWindow.IsLoaded)
            {
                primSelectionWindow.UpdateSelectedPrimImage(primNumber);
            }
        }

        public int SelectedPrimNumber
        {
            get { return selectedPrimNumber; }
            set { selectedPrimNumber = value; }
        }

    }
}
