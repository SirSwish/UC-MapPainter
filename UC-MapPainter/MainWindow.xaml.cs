using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace UC_MapPainter
{
    public partial class MainWindow : Window
    {
        //UI
        private string currentEditMode;
        public bool IsEditMode { get; private set; }
        private ScaleTransform scaleTransform = new ScaleTransform();
        private GridModel gridModel = new GridModel();

        //Map
        private byte[] fileBytes;
        private string loadedFilePath;

        //Textures
        private TextureFunctions textureFunctions;
        private TextureSelectionWindow textureSelectionWindow;
        private int selectedWorldNumber;
        private bool isTextureSelectionLocked = false;
        private string lockedWorld = null;
        private string selectedTextureType;
        private int selectedTextureNumber;
        private int selectedTextureRotation = 0;

        //Prims
        internal PrimSelectionWindow primSelectionWindow;
        private PrimFunctions primFunctions;
        private int selectedPrimNumber = -1;
        private Canvas MapWhoGridCanvas = new Canvas();
        private bool isMapWhoGridVisible = false;
        public int SelectedPrimNumber
        {
            get { return selectedPrimNumber; }
            set { selectedPrimNumber = value; }
        }


        //Windows
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            MainContentGrid.LayoutTransform = scaleTransform;
            MainContentGrid.MouseMove += MainContentGrid_MouseMove;
            MapWhoGridCanvas.IsHitTestVisible = false;
            MainContentGrid.Children.Add(MapWhoGridCanvas);
            primFunctions = new PrimFunctions(this, gridModel, primSelectionWindow);
            textureFunctions = new TextureFunctions(gridModel, selectedWorldNumber, this);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeTextureSelectionWindow();
            SetEditMode("Textures");
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


        ///////////////
        //Click Events
        ///////////////
        private void PrimSelection_Click(object sender, RoutedEventArgs e)
        {
            InitializePrimSelectionWindow();
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
                selectedWorldNumber = textureFunctions.ExtractWorldNumber(selectedWorld);

                ProgressBar.Visibility = Visibility.Visible;

                var generatingMapWindow = new GeneratingMapWindow();
                generatingMapWindow.Owner = this;
                generatingMapWindow.Show();

                // Clear the arrays
                gridModel.PrimArray.Clear();
                gridModel.MapWhoArray.Clear();

                await textureFunctions.InitializeMapGridAsync(selectedWorldNumber);

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

        private void EditPrimsButton_Click(object sender, RoutedEventArgs e)
        {
            SetEditMode("Prims");
            InitializePrimSelectionWindow();
            primFunctions.DrawPrims(OverlayGrid);
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

        private void ShowObObInfo_Click(object sender, RoutedEventArgs e)
        {
            primFunctions.DisplayObjectData();
        }

        private void ShowMapWhoInfo_Click(object sender, RoutedEventArgs e)
        {
            primFunctions.DisplayMapWhoInfo();
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

        ///////////////
        //Events
        ///////////////
        private void PrimSelectionWindow_Closed(object sender, EventArgs e)
        {
            PrimSelectionMenuItem.IsEnabled = true; // Enable the menu item when the window is closed
        }

        private void TextureSelectionWindow_Closed(object sender, EventArgs e)
        {
            TextureSelectionMenuItem.IsEnabled = true; // Enable the menu item when the window is closed

            // Track lock status and selected world
            isTextureSelectionLocked = textureSelectionWindow.IsWorldLocked;
            lockedWorld = textureSelectionWindow.GetSelectedWorld();
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

        //////////////////
        /// Miscealaneous
        /////////////////

        private async void LoadMapAsync(string filePath)
        {
            loadedFilePath = filePath; // Store the loaded file path
            UpdateWindowTitle(Path.GetFileName(filePath));
            fileBytes = File.ReadAllBytes(filePath); // Store file bytes

            // Clear the arrays
            gridModel.PrimArray.Clear();
            gridModel.MapWhoArray.Clear();

            // Determine the world number from the specified offset (0x7D4 from the end of the file)
            int worldNumberOffset = fileBytes.Length - 0x7D4;
            selectedWorldNumber = fileBytes[worldNumberOffset];

            // Validate the world number
            string selectedWorld = $"World {selectedWorldNumber}";
            if (!textureFunctions.IsValidWorld(selectedWorldNumber))
            {
                MessageBox.Show("World not assigned to Map. Please select a world.", "Invalid World", MessageBoxButton.OK, MessageBoxImage.Warning);
                var worldSelectionWindow = new WorldSelectionWindow();
                if (worldSelectionWindow.ShowDialog() == true)
                {
                    selectedWorld = worldSelectionWindow.SelectedWorld;
                    selectedWorldNumber = textureFunctions.ExtractWorldNumber(selectedWorld);
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
            await primFunctions.ReadObjectData(fileBytes, saveType, obSize1);

            // Load the cells
            await textureFunctions.InitializeMapGridFromBytesAsync(fileBytes, selectedWorldNumber);

            generatingMapWindow.Close();
            ProgressBar.Visibility = Visibility.Collapsed;

            SaveMenuItem.IsEnabled = true;
            ExportMenuItem.IsEnabled = true; // Enable the Export menu item
            SetEditMode("Textures");
        }

        public void Cell_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
                else if (currentEditMode == "Height")
                {
                    int row = 127 - Grid.GetRow(cell);
                    int col = 127 - Grid.GetColumn(cell);

                    var cellData = gridModel.Cells.FirstOrDefault(c => c.Row == row && c.Column == col);
                    if (cellData != null)
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
                }
            }
        }

        public void Cell_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
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
                    var texturePath = textureFunctions.GetTexturePath(cell.TextureType, cell.TextureNumber);
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

        private void ApplyRotation()
        {
            var transform = new RotateTransform(selectedTextureRotation)
            {
                CenterX = SelectedTextureImage.Width / 2,
                CenterY = SelectedTextureImage.Height / 2
            };
            SelectedTextureImage.RenderTransform = transform;
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
                    InitializePrimSelectionWindow(); // Open the PrimNumber Selection Window
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

        private void DrawMapWhoGrid()
        {
            primFunctions.DrawMapWhoGrid(MapWhoGridCanvas);
            EnsureMapWhoGridCanvasOnTop();
        }

        private void EnsureMapWhoGridCanvasOnTop()
        {
            MainContentGrid.Children.Remove(MapWhoGridCanvas);
            MainContentGrid.Children.Add(MapWhoGridCanvas);
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

        public void UpdateSelectedPrim(int primNumber)
        {
            primFunctions.UpdateSelectedPrim(primNumber);
        }


    }
}
