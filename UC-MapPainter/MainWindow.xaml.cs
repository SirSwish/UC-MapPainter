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
        private string loadedFilePath;
        private TextureSelectionWindow textureSelectionWindow;
        private GridModel gridModel = new GridModel();
        private int selectedWorldNumber;
        private ScaleTransform scaleTransform = new ScaleTransform();
        private string selectedTextureType;
        private int selectedTextureNumber;
        private int selectedTextureRotation = 0;
        public bool IsEditMode { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            MainContentGrid.LayoutTransform = scaleTransform;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeTextureSelectionWindow();
            SetEditMode("Textures");
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
            }
        }

        private void TextureSelectionWindow_Closed(object sender, EventArgs e)
        {
            TextureSelectionMenuItem.IsEnabled = true; // Enable the menu item when the window is closed
        }

        private void TextureSelection_Click(object sender, RoutedEventArgs e)
        {
            InitializeTextureSelectionWindow();
        }

        private async void NewMap_Click(object sender, RoutedEventArgs e)
        {
            var worldSelectionWindow = new WorldSelectionWindow();
            if (worldSelectionWindow.ShowDialog() == true)
            {
                loadedFilePath = null; // Reset the loaded file path when creating a new map
                LoadedFilePathLabel.Text = "None"; // Update label
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
                LoadMapAsync(filePath);
            }
        }

        private async void LoadMapAsync(string filePath)
        {
            loadedFilePath = filePath; // Store the loaded file path

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

            // Show Generating Map window and progress bar
            ProgressBar.Visibility = Visibility.Visible;
            var generatingMapWindow = new GeneratingMapWindow();
            generatingMapWindow.Owner = this;
            generatingMapWindow.Show();

            // Load the cells
            await InitializeMapGridFromBytesAsync(fileBytes);

            generatingMapWindow.Close();
            ProgressBar.Visibility = Visibility.Collapsed;

            SaveMenuItem.IsEnabled = true;
            ExportMenuItem.IsEnabled = true; // Enable the Export menu item
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
                    byte heightByte = fileBytes[index + 4];
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

                    // Update gridModel.Cells
                    var cellData = new Cell
                    {
                        Row = row,
                        Column = col,
                        TextureType = textureType,
                        TextureNumber = textureNumber,
                        Rotation = (int)rotation,
                        Height = heightByte // Assign the height from the loaded byte
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
                int row = 127 - Grid.GetRow(cell);
                int col = 127 - Grid.GetColumn(cell);

                var cellData = gridModel.Cells.FirstOrDefault(c => c.Row == row && c.Column == col);
                if (cellData != null)
                {
                    if (IsEditMode)
                    {
                        int increment = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? 10 : 1;
                        cellData.Height = Math.Min(cellData.Height + increment, 127);

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
                    else
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
                        string texturePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Textures", textureFolder, $"tex{cellData.TextureNumber:D3}hi.bmp");

                        string debugMessage = $"Cell Debug Information:\n" +
                                              $"X: {col}\n" +
                                              $"Y: {row}\n" +
                                              $"Texture File Path: {texturePath}\n" +
                                              $"Texture Type: {cellData.TextureType}\n" +
                                              $"Rotation: {cellData.Rotation}°\n" +
                                              $"Height: {cellData.Height}";
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

                // Add debug statement for verification
                string debugMessage = $"Painting cell ({Grid.GetColumn(cell)}, {Grid.GetRow(cell)}):\nTexturePath: {texturePath}\nRotation: {rotation}°";
                // MessageBox.Show(debugMessage, "Debug Information", MessageBoxButton.OK, MessageBoxImage.Information);
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

                // Use the loaded file path as the template if a map was loaded, otherwise use default.iam
                string templateFilePath = loadedFilePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Map", "default.iam");
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
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Textures", textureFolder, $"tex{textureNumber:D3}hi.bmp");
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
            EditTextureButton.IsEnabled = true;
            EditHeightButton.IsEnabled = true;
            EditBuildingsButton.IsEnabled = false;

            switch (mode)
            {
                case "Textures":
                    EditTextureButton.IsEnabled = false;
                    IsEditMode = false;
                    break;
                case "Height":
                    EditHeightButton.IsEnabled = false;
                    IsEditMode = true;
                    break;
                case "Buildings":
                    EditBuildingsButton.IsEnabled = false;
                    IsEditMode = false;
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
    }
}
