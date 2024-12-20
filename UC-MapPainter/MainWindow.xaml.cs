﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Collections.Generic;
using Path = System.IO.Path;
using System.Windows.Data;
using System.Windows.Shapes;

namespace UC_MapPainter
{
    public partial class MainWindow : Window
    {
        //UI
        public string currentEditMode;
        private ScaleTransform scaleTransform = new ScaleTransform();
        
        [Flags]
        public enum ButtonFlags
        {
            None = 0,
            Textures = 1 << 0,
            Height = 1 << 1,
            Buildings = 1 << 2,
            Prims = 1 << 3,
            Lights = 1 << 4,
            Walls = 1 << 5,
            All = Textures | Height | Buildings | Prims | Lights | Walls
        }

        //Map
        public byte[] loadedFileBytes;
        private string loadedFilePath;
        public byte[] modifiedFileBytes;
        public byte[] LoadedFileBytes
        {
            get { return loadedFileBytes; }
            set { loadedFileBytes = value; }
        }
        public string LoadedFilePath
        {
            get { return loadedFilePath; }
            set { loadedFilePath = value; }
        }
        public byte[] ModifiedFileBytes
        {
            get { return modifiedFileBytes; }
            set { modifiedFileBytes = value; }
        }
        public bool isNewFile = true;

        //Textures
        private TextureFunctions textureFunctions;
        private TextureSelectionWindow textureSelectionWindow;
        private int selectedWorldNumber;
        private bool isTextureSelectionLocked = false;
        private int lockedWorld = -1;
        private string selectedTextureType;
        private int selectedTextureNumber;
        private int selectedTextureRotation = 0;

        //Prims
        internal PrimSelectionWindow primSelectionWindow;
        private PrimFunctions primFunctions;
        private int selectedPrimNumber = -1;
        private Canvas MapWhoGridCanvas = new Canvas();
        private bool isMapWhoGridVisible = false;
        private GridModel gridModel = new GridModel();

        public int SelectedPrimNumber
        {
            get { return selectedPrimNumber; }
            set { selectedPrimNumber = value; }
        }
        public bool graphicsEnabled = true;

        //Walls
        internal WallSelectionWindow wallSelectionWindow;

        //Buildings
        private BuildingFunctions buildingFunctions;

        // FIXME:

        private Point? currentStartPoint = null;
        private List<Point> currentBuildingPoints = new List<Point>();
        //private List<List<Point>> buildings = new List<List<Point>>(); // Stores completed buildings

        private List<DFacet> facets = new(); // List of all facets created
        private List<short> dstyles = new();
        private List<DBuilding> buildings = new(); // List of all buildings created
        private List<DStorey> storeys = new(); // List of all buildings created
        private List<Line> wallCanvasLines = new();


        private int currentlySelectedCanvasWallLine = 0;
        private int currentFacetIndex = 1; // Index of the next facet to be created
        private Line previewLine = null;
        
        private Line selectedWall = null;
        private DFacet selectedFacet = null;

        private Dictionary<Line, DFacet> wallLines = new Dictionary<Line, DFacet>();

        //private List<Point> mWallPoints = new List<Point>();

        //Lights
        internal LightSelectionWindow lightSelectionWindow;
        public LightFunctions lightFunctions;
        public string loadedLightFilePath = string.Empty;
        public byte[] modifiedLightFileBytes;
        public byte[] loadedLightFileBytes;
        public List<LightEntry> lightEntries = new List<LightEntry>();
        public int EdLightFree = 0; // Tracks the first free LightEntry index
        public LightHeader lightHeader = new LightHeader();
        public LightProperties lightProperties = new LightProperties();
        public LightNightColour lightNightColor = new LightNightColour();
        public byte[] LoadedLightFileBytes
        {
            get { return loadedFileBytes; }
            set { loadedFileBytes = value; }
        }
        public string LoadedLightFilePath
        {
            get { return loadedFilePath; }
            set { loadedFilePath = value; }
        }
        public byte[] ModifiedLightFileBytes
        {
            get { return modifiedFileBytes; }
            set { modifiedFileBytes = value; }
        }
        public uint NightFlag { get; set; }
        public byte D3DAlpha { get; set; }
        public byte D3DRed { get; set; }
        public byte D3DGreen { get; set; }
        public byte D3DBlue { get; set; }
        public byte SpecularAlpha { get; set; }
        public byte SpecularRed { get; set; }
        public byte SpecularGreen { get; set; }
        public byte SpecularBlue { get; set; }
        public int NightAmbRed { get; set; }
        public int NightAmbGreen { get; set; }
        public int NightAmbBlue { get; set; }
        public sbyte NightLampostRed { get; set; }
        public sbyte NightLampostGreen { get; set; }
        public sbyte NightLampostBlue { get; set; }
        public int NightLampostRadius { get; set; }
        public byte NightSkyRed { get; set; }
        public byte NightSkyGreen { get; set; }
        public byte NightSkyBlue { get; set; }

        //Windows
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            MainContentGrid.LayoutTransform = scaleTransform;
            OverlayGrid.LayoutTransform = scaleTransform;
            MainContentGrid.MouseMove += MainContentGrid_MouseMove;
            MapWhoGridCanvas.IsHitTestVisible = false;
            MainContentGrid.Children.Add(MapWhoGridCanvas);
            primFunctions = new PrimFunctions(this, gridModel, primSelectionWindow);
            lightFunctions = new LightFunctions(this, lightSelectionWindow);
            textureFunctions = new TextureFunctions(gridModel, selectedWorldNumber, this);
            buildingFunctions = new BuildingFunctions(primFunctions, this); // Initialize buildingFunctions

            // Bind Ctrl+G to the navigation command
            InputBindings.Add(new KeyBinding(
                NavigationCommands.GoToPage, 
                new KeyGesture(Key.G, ModifierKeys.Control)
            ));
            CommandBindings.Add(new CommandBinding(
                NavigationCommands.GoToPage, 
                NavigateToPositionCommand_Executed
            ));

            // Bind Ctrl+N
            InputBindings.Add(new KeyBinding(
                NavigationCommands.BrowseForward,
                //new KeyGesture(Key.Left, ModifierKeys.Control)
                new KeyGesture(Key.N, ModifierKeys.Control)
            ));
            CommandBindings.Add(new CommandBinding(
                NavigationCommands.BrowseForward,
                NavigateNextWallCommand_Executed
            ));

            this.Closing += MainWindow_Closing; // Add this line
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Set the initial state of the GraphicsEnabledCheckBox
            GraphicsEnabledCheckBox.IsChecked = graphicsEnabled;
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Check if there are unsaved changes
            CheckForUnsavedChanges();
        }

        private void InitializeWallsSelectionWindow()
        {
            wallSelectionWindow = new WallSelectionWindow();
            //wallSelectionWindow.SetMainWindow(this);
            wallSelectionWindow.Left = this.Left + this.Width - wallSelectionWindow.Width - 10;
            wallSelectionWindow.Top = 50;
            //wallSelectionWindow.Closed += PrimSelectionWindow_Closed;
            wallSelectionWindow.Show();
            wallSelectionWindow.Owner = this; // Set the owner after showing the window
            PrimSelectionMenuItem.IsEnabled = false; // Disable the menu item
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

        private void InitializeLightSelectionWindow()
        {
            if (lightSelectionWindow == null || !lightSelectionWindow.IsLoaded)
            {
                lightSelectionWindow = new LightSelectionWindow();
                lightSelectionWindow.SetMainWindow(this);
                lightSelectionWindow.Left = this.Left + this.Width - lightSelectionWindow.Width - 10;
                lightSelectionWindow.Top = 50;
                lightSelectionWindow.Closed += LightSelectionWindow_Closed;
                lightSelectionWindow.Show();
                lightSelectionWindow.Owner = this; // Set the owner after showing the window
                LightSelectionMenuItem.IsEnabled = false; // Disable the menu item
                lightFunctions = new LightFunctions(this, lightSelectionWindow);
            }

            if (!lightSelectionWindow.IsVisible)
            {
                lightSelectionWindow.Show();
            }
            else
            {
                lightSelectionWindow.Activate();
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
        ///
        //private void BuildingSelection_Click(object sender, RoutedEventArgs e)
        //{
        //    InitializeWallsSelectionWindow();
        //}

        private void PrimSelection_Click(object sender, RoutedEventArgs e)
        {
            InitializePrimSelectionWindow();
        }
        private void TextureSelection_Click(object sender, RoutedEventArgs e)
        {
            InitializeTextureSelectionWindow();
        }

        private void LightSelection_Click(object sender, RoutedEventArgs e)
        {
            InitializeLightSelectionWindow();
        }

        private async void NewMap_Click(object sender, RoutedEventArgs e)
        {
            string newFile = "";
            isNewFile = true;
            LoadAsync(newFile);
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
                isNewFile = false;
                LoadAsync(filePath);
            }
        }

        private void SaveMap_Click(object sender, RoutedEventArgs e)
        {
            if (!isNewFile)
            {
                File.WriteAllBytes(loadedFilePath, modifiedFileBytes);
            }
            else 
            {
                SaveAsMap();
            }
        }
        private void SaveAsMap_Click(object sender, RoutedEventArgs e)
        {
            SaveAsMap();
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
            if (modifiedFileBytes == null)
            {
                MessageBox.Show("No map file loaded. Please load a map file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SetEditMode("Textures");
        }

        private void EditHeightButton_Click(object sender, RoutedEventArgs e)
        {
            if (modifiedFileBytes == null)
            {
                MessageBox.Show("No map file loaded. Please load a map file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SetEditMode("Height");
        }

        private void EditBuildingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (modifiedFileBytes == null)
            {
                MessageBox.Show("No map file loaded. Please load a map file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            OverlayGrid.Visibility = Visibility.Visible;
            // Call the DrawBuildings method to visualize the buildings
            SetEditMode("Buildings");
            InitializeWallsSelectionWindow();
            
            //buildingFunctions.DrawBuildings(modifiedFileBytes, OverlayGrid);
            
        }

        private void EditPrimsButton_Click(object sender, RoutedEventArgs e)
        {
            SetEditMode("Prims");
            InitializePrimSelectionWindow();
            primFunctions.DrawPrims(OverlayGrid);
        }
        private void EditLightsButton_Click(object sender, RoutedEventArgs e)
        {
            // Load the embedded resource Light/blank.lgt
            try
            {
                // Get the executing assembly
                var assembly = Assembly.GetExecutingAssembly();

                // Build the resource name
                // Adjust the namespace and path according to your project's structure
                string resourceName = "UC_MapPainter.Light.blank.lgt";

                // Get the resource stream
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        MessageBox.Show($"Could not find embedded resource '{resourceName}'.", "Resource Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        loadedLightFileBytes = memoryStream.ToArray();
                    }
                }

                // Clone the loaded bytes for modification
                modifiedLightFileBytes = (byte[])loadedLightFileBytes.Clone();

                // Initialize the LightSelectionWindow
                InitializeLightSelectionWindow();

                // Set the edit mode to "Lights"
                SetEditMode("Lights");

                // Read light data from the loaded file
                lightHeader = lightFunctions.ReadLightHeader(modifiedLightFileBytes);
                lightEntries = lightFunctions.ReadLightEntries(modifiedLightFileBytes);
                lightProperties = lightFunctions.ReadLightProperties(modifiedLightFileBytes);
                lightNightColor = lightFunctions.ReadLightNightColour(modifiedLightFileBytes);

                // Pass extracted data to the LightSelectionWindow
                lightSelectionWindow.SetLightProperties(lightProperties);
                lightSelectionWindow.SetLightNightColour(lightNightColor);
                lightSelectionWindow.SetLightEntries(lightEntries);

                // Draw lights on the map
                lightFunctions.DrawLights(OverlayGrid, lightEntries);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while loading the embedded light file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditWallsButton_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("Hura.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void ShowPrimInfo_Click(object sender, RoutedEventArgs e)
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
        private void ToggleGraphicsEnabled_Click(object sender, RoutedEventArgs e)
        {
            graphicsEnabled = !graphicsEnabled;
            GraphicsEnabledCheckBox.IsChecked = graphicsEnabled;
            OverlayGrid.Children.Clear();
            SetEditMode("Prims");
            primFunctions.DrawPrims(OverlayGrid);
        }
        
        // Event handler for viewing Building Header information
        private void ViewBuildingHeader_Click(object sender, RoutedEventArgs e)
        {
            if (modifiedFileBytes == null)
            {
                MessageBox.Show("No map file loaded. Please load a map file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Read Building Header
            var buildingHeader = BuildingHeader.ReadFromBytes(modifiedFileBytes, Map.BuildingDataOffset);

            // Show header details in a simple message box for now
            MessageBox.Show(
                $"Total Buildings: {buildingHeader.TotalBuildings}\n" +
                $"Total Walls: {buildingHeader.TotalWalls}",
                "Building Header Information",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        // Stub for viewing Buildings
        private void ViewBuildings_Click(object sender, RoutedEventArgs e)
        {
            if (modifiedFileBytes == null)
            {
                MessageBox.Show("No map file loaded. Please load a map file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Extract Building Header
                int buildingHeaderOffset = Map.BuildingDataOffset;
                byte[] buildingHeader = new byte[48];
                Array.Copy(modifiedFileBytes, buildingHeaderOffset, buildingHeader, 0, 48);

                // Read total number of buildings from the header (subtracting 1 as per your understanding)
                int totalBuildings = BitConverter.ToUInt16(buildingHeader, 2) - 1;

                // Read the buildings data
                int buildingDataOffset = buildingHeaderOffset + 48; // Header size + Padding
                List<Building> buildings = new List<Building>();

                for (int i = 0; i < totalBuildings; i++)
                {
                    Building building = Building.ReadBuilding(modifiedFileBytes, buildingDataOffset + i * 24);
                    buildings.Add(building);
                }

                // Create a new window to display the buildings
                Window buildingWindow = new Window
                {
                    Title = "Buildings",
                    Width = 600,
                    Height = 400,
                    Content = CreateBuildingsDataGrid(buildings)
                };
                buildingWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load building data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Stub for viewing Walls
        private void ViewWalls_Click(object sender, RoutedEventArgs e)
        {
            if (modifiedFileBytes == null)
            {
                MessageBox.Show("No map file loaded. Please load a map file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Extract Building Header
                int buildingHeaderOffset = Map.BuildingDataOffset;
                byte[] buildingHeader = new byte[48];
                Array.Copy(modifiedFileBytes, buildingHeaderOffset, buildingHeader, 0, 48);

                // Read total number of buildings from the header (subtracting 1 as per your understanding)
                int totalBuildings = BitConverter.ToUInt16(buildingHeader, 2) - 1;

                // Calculate the walls section offset
                int wallDataOffset = buildingHeaderOffset + 48 + (totalBuildings * 24) + 14;

                // Read total number of walls from the header (subtracting 1 as per your understanding)
                int totalWalls = BitConverter.ToUInt16(buildingHeader, 4) - 1;

                // Read the walls data
                List<Wall> walls = new List<Wall>();

                for (int i = 0; i < totalWalls; i++)
                {
                    Wall wall = Wall.ReadWall(modifiedFileBytes, wallDataOffset + i * 26);
                    walls.Add(wall);
                }

                // Create a new window to display the walls
                Window wallWindow = new Window
                {
                    Title = "Walls",
                    Width = 800,
                    Height = 600,
                    Content = CreateWallsDataGrid(walls)
                };
                wallWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load wall data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        List<(int X, int Z)> GetPolygonFromFacets(IEnumerable<DFacet> facets)
        {
            var polygon = new List<(int X, int Z)>();

            // Assumes facets are ordered; adjust if needed
            foreach (var facet in facets)
            {
                polygon.Add((facet.X[0], facet.Z[0]));
                polygon.Add((facet.X[1], facet.Z[1]));
            }

            // Optionally remove duplicates if facets share vertices
            return polygon.Distinct().ToList();
        }

        private void NavigateNextWallCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            currentlySelectedCanvasWallLine++;
            if(currentlySelectedCanvasWallLine >= wallCanvasLines.Count)
            {
                currentlySelectedCanvasWallLine = 0;
            }
            SelectWall(wallCanvasLines[currentlySelectedCanvasWallLine]);
        }

        private void NavigateToPositionCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // Prompt the user for the position
            string input = Microsoft.VisualBasic.Interaction.InputBox("Enter coordinates (e.g., 64,64):",
                                                                      "Navigate to Position",
                                                                      "64,64");
            if (!string.IsNullOrEmpty(input) && input.Contains(","))
            {
                var parts = input.Split(',');
                if (int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                {
                    NavigateToPosition(x, y);
                }
                else
                {
                    MessageBox.Show("Invalid input. Please enter coordinates in the format X,Y.", "Error");
                }
            }
        }

        private void NavigateToPosition(int x, int y)
        {
            // Ensure coordinates are within bounds
            if (x < 0 || x >= 128 || y < 0 || y >= 128)
            {
                MessageBox.Show("Coordinates out of bounds.", "Error");
                return;
            }

            // Assuming your grid is wrapped in a ScrollViewer
            ScrollViewer scrollViewer = this.MainScrollViewer;

            // Calculate the offset to center the cell
            double cellSize = 64; // Each cell is 64x64
            double offsetX = x * cellSize - (scrollViewer.ViewportWidth / 2) + (cellSize / 2);
            double offsetY = y * cellSize - (scrollViewer.ViewportHeight / 2) + (cellSize / 2);

            // Scroll to the calculated offsets
            scrollViewer.ScrollToHorizontalOffset(Math.Max(0, offsetX));
            scrollViewer.ScrollToVerticalOffset(Math.Max(0, offsetY));
        }

        (int minX, int maxX, int minZ, int maxZ) GetBoundingBox(List<(int X, int Z)> polygon)
        {
            int minX = polygon.Min(point => point.X);
            int maxX = polygon.Max(point => point.X);
            int minZ = polygon.Min(point => point.Z);
            int maxZ = polygon.Max(point => point.Z);

            return (minX, maxX, minZ, maxZ);
        }

        bool IsPointInPolygon(int x, int z, List<(int X, int Z)> polygon)
        {
            bool isInside = false;
            int n = polygon.Count;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if ((polygon[i].Z > z) != (polygon[j].Z > z) &&
                    (x < (polygon[j].X - polygon[i].X) * (z - polygon[i].Z) / (polygon[j].Z - polygon[i].Z) + polygon[i].X))
                {
                    isInside = !isInside;
                }
            }

            return isInside;
        }
        private int CalculateBuildingsOffset(int next_dbuildingOffset)
        {
            int currentOffset = next_dbuildingOffset;


            Int16 next_dbuilding = BitConverter.ToInt16(modifiedFileBytes, currentOffset);
            currentOffset += 2;
            Int16 next_dfacet = BitConverter.ToInt16(modifiedFileBytes, currentOffset);
            currentOffset += 2;
            Int16 next_dstyle = BitConverter.ToInt16(modifiedFileBytes, currentOffset);
            currentOffset += 2;
            Int16 next_paint_mem = BitConverter.ToInt16(modifiedFileBytes, currentOffset);
            currentOffset += 2;
            Int16 next_dstorey = BitConverter.ToInt16(modifiedFileBytes, currentOffset);
            currentOffset += 2;

            // move by dbuilding offset
            for (int i = 0; i < next_dbuilding; ++i)
            {
                currentOffset += 24;
            }
            // move by dfacet offset
            for (int i = 0; i < next_dfacet; ++i)
            {
                currentOffset += 26;
            }
            // move by dstyle offset
            for (int i = 0; i < next_dstyle; ++i)
            {
                currentOffset += 2;
            }
            // move by paint_mem offset
            for (int i = 0; i < next_paint_mem; ++i)
            {
                currentOffset += 1;
            }
            // move by dstorey offset
            for (int i = 0; i < next_paint_mem; ++i)
            {
                currentOffset += 6;
            }

            Int16 next_inside_storey = BitConverter.ToInt16(modifiedFileBytes, currentOffset);
            currentOffset += 2;
            Int16 next_inside_stair = BitConverter.ToInt16(modifiedFileBytes, currentOffset);
            currentOffset += 2;
            Int32 next_inside_block = BitConverter.ToInt32(modifiedFileBytes, currentOffset);
            currentOffset += 4;

            // move by inside_storey offset
            for (int i = 0; i < next_inside_storey; ++i)
            {
                currentOffset += 22;
            }
            // move by inside_stairs offset
            for (int i = 0; i < next_inside_stair; ++i)
            {
                currentOffset += 10;
            }
            // move by inside_block offset
            for (int i = 0; i < next_inside_block; ++i)
            {
                currentOffset += 1;
            }

            Int16 next_dwalkable_offset = BitConverter.ToInt16(modifiedFileBytes, currentOffset);
            currentOffset += 2;
            Int16 next_roof_face4_offset = BitConverter.ToInt16(modifiedFileBytes, currentOffset);
            currentOffset += 2;


            // move by dwalkables offset
            for (int i = 0; i < next_dwalkable_offset; ++i)
            {
                currentOffset += 22;
            }
            // move by roof_faces4 offset
            for (int i = 0; i < next_roof_face4_offset; ++i)
            {
                currentOffset += 10;
            }


            //int inside_storeys_offset = 22;
            //int inside_stairs_offset = 10;
            //int inside_block_offset = 1;
            //int next_dwalkable_offset = 2;
            //int next_roof_face4_offset = 2;
            //int dwalkables_offset = 22;
            //int roof_face4_offset = 10;




            return currentOffset;
        }
        private void SaveBuildingData_Click(object sender, RoutedEventArgs e)
        {
            //int iamBuildingsOffset = 98314;^
            //Map.WriteBuildingsMock(modifiedFileBytes, iamBuildingsOffset);

            // UPDATE PAP 

            // THIS IS JUST FOR TESTS AS BUILDINGS AREN'T ALWAYS SQUARED
            // Find the lowest and biggest X and Z values
            //byte minX = facets.Min(facet => Math.Min(facet.X[0], facet.X[1]));
            //byte maxX = facets.Max(facet => Math.Max(facet.X[0], facet.X[1]));

            //byte minZ = facets.Min(facet => Math.Min(facet.Z[0], facet.Z[1]));
            //byte maxZ = facets.Max(facet => Math.Max(facet.Z[0], facet.Z[1]));

            //for (int i = minX; i < maxX; i++)
            //{
            //    for( int j = minZ; j < maxZ; j++)
            //    {

            //        int cellOffset = textureFunctions.FindCellTexOffset(i, j);
            //        ushort hardcodedRoofFlags = 592;

            //        Map.WritePapFlagsData(modifiedFileBytes, hardcodedRoofFlags, cellOffset);
            //        Map.WriteAltData(modifiedFileBytes, 4, cellOffset);
            //    }
            //}


            foreach (var building in buildings)
            {
                // Get facets for the current building
                var buildingFacets = facets.Skip(building.StartFacet - 1).Take(building.EndFacet - building.StartFacet);

                // Extract all Y values
                var yValues = buildingFacets.Select(facet => facet.Y).ToList();


                var polygon = GetPolygonFromFacets(buildingFacets);
                var (minX, maxX, minZ, maxZ) = GetBoundingBox(polygon);

                for (int x = minX; x <= maxX; x++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        // Check if the tile center is inside the polygon
                        if (IsPointInPolygon(x, z, polygon))
                        {
                            int cellOffset = textureFunctions.FindCellTexOffset(x, z);
                            ushort hardcodedRoofFlags = 592;

                            Map.WritePapFlagsData(modifiedFileBytes, hardcodedRoofFlags, cellOffset);
                            
                            short firstYValue = (short)(yValues[0].FirstOrDefault());

                            byte height = (byte) ((firstYValue / 256) * 4 + 4);
                            Map.WriteAltData(modifiedFileBytes, height, cellOffset);
                        }
                    }
                }
            }



            List<byte> buildingsData = Map.PrepareBuildingsMock(buildings, facets, storeys, dstyles);

            // FIXME:
            // THIS NEEDS TO BE CALCULATED AS IT'S NOT ALWAYS ON A FIXED POSITION
            int startEmptyIamBuildingsOffset = 98314;

            // THIS WORKS FOR EMPTY MAP?
            //// next_dbuilding+next_dfacet+next_dstyle+next_paint_mem+next_dstorey+sizeof(struct DBuilding)+ sizeof(struct DFacet) + sizeof(UWORD) + sizeof(UBYTE) + sizeof(next_dstorey)
            ////int next_dfacet_offset = 2;

            //int next_dbuildings_offset = 2;
            //int next_dfacet_offset = 2;
            //int next_dstyle_offset = 2;
            //int next_paint_mem_offset = 2;
            //int next_dstorey_offset = 2;
            //int dbuilding_offset = 24;
            //int dfacet_offset = 26;
            //int dstyle_offset = 2;
            //int paint_mem_offset = 1;
            //int dstorey_offset = 6;
            //int next_inside_storey_offset = 2;
            //int next_inside_stair_offset = 2;
            //int next_inside_block_offset = 4;
            //int inside_storeys_offset = 22;
            //int inside_stairs_offset = 10;
            //int inside_block_offset = 1;
            //int next_dwalkable_offset = 2;
            //int next_roof_face4_offset = 2;
            //int dwalkables_offset = 22;
            //int roof_face4_offset = 10;


            //int buildingsOffset = next_dbuildings_offset + next_dfacet_offset + next_dstyle_offset + next_paint_mem_offset + next_dstorey_offset + dbuilding_offset +
            //dfacet_offset + dstyle_offset + paint_mem_offset + dstorey_offset + next_inside_storey_offset + next_inside_stair_offset + next_inside_block_offset +
            //inside_storeys_offset + inside_stairs_offset + inside_block_offset + next_dwalkable_offset + next_roof_face4_offset + dwalkables_offset + roof_face4_offset;

            ////int walkableOffsets = 3 + 33 + 2 + 2 + 22 + 10;
            ////int endEmptyIamBuildingsOffset = startEmptyIamBuildingsOffset + 2+2+2+2+2+ 24+26+2+1+6 + walkableOffsets;

            //int endEmptyIamBuildingsOffset = startEmptyIamBuildingsOffset + buildingsOffset;


            int endEmptyIamBuildingsOffset = CalculateBuildingsOffset(startEmptyIamBuildingsOffset);

            // Create a dynamic list for the new byte array
            List<byte> resultBytes = new List<byte>();

            // Step 1: Grab the first 98314 bytes
            resultBytes.AddRange(modifiedFileBytes.Take(98314));

            // Step 2: Insert the buildings bytes
            resultBytes.AddRange(buildingsData);

            // Step 3: Append the bytes from modifiedFileBytes starting at endEmptyIamBuildingsOffset
            resultBytes.AddRange(modifiedFileBytes.Skip(endEmptyIamBuildingsOffset));

            modifiedFileBytes = resultBytes.ToArray();
            //modifiedFileBytes = ModifyFileBytes(modifiedFileBytes, startEmptyIamBuildingsOffset, buildingsBytes);


        }

        private void DumpBuildingData_Click(object sender, RoutedEventArgs e)
        {
           
                if (modifiedFileBytes == null)
                {
                    MessageBox.Show("No map file loaded. Please load a map file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                buildingFunctions.DumpBuildingData();
            
        }

        private void ViewRawBuildingData_Click(object sender, RoutedEventArgs e)
        {
            if (modifiedFileBytes == null)
            {
                MessageBox.Show("No map file loaded. Please load a map file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Use the overloaded DumpBuildingData method to get building data as text
                string rawDataText = buildingFunctions.DumpBuildingData(true);

                // Show raw data in a text box popup
                Window rawDataWindow = new Window
                {
                    Title = "Raw Building Data",
                    Width = 600,
                    Height = 400,
                    Content = new TextBox
                    {
                        Text = rawDataText,
                        IsReadOnly = true,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                        TextWrapping = TextWrapping.Wrap  // Enable text wrapping
                    }
                };

                rawDataWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load raw building data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        ///////////////
        //Events
        ///////////////
        private void PrimSelectionWindow_Closed(object sender, EventArgs e)
        {
            PrimSelectionMenuItem.IsEnabled = true; // Enable the menu item when the window is closed
        }
        private void LightSelectionWindow_Closed(object sender, EventArgs e)
        {
            LightSelectionMenuItem.IsEnabled = true; // Enable the menu item when the window is closed
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
        /// Miscellaneous
        /////////////////


        private Point GetNearestCorner(double x, double y)
        {
            // Assuming a grid cell size of 64x64
            int gridSize = 64;
            double nearestX = Math.Round(x / gridSize) * gridSize;
            double nearestY = Math.Round(y / gridSize) * gridSize;
            return new Point(nearestX, nearestY);
        }


        // Helper function to create a DataGrid for displaying buildings
        private DataGrid CreateWallsDataGrid(List<Wall> walls)
        {
            DataGrid dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                IsReadOnly = true,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                SelectionMode = DataGridSelectionMode.Single,
                SelectionUnit = DataGridSelectionUnit.FullRow
            };

            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Wall #", Binding = new Binding("WallNumber") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Wall Type", Binding = new Binding("WallTypeDescription") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Wall Height (Storeys)", Binding = new Binding("WallHeightStoreys") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "X1", Binding = new Binding("X1") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "X2", Binding = new Binding("X2") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Z1", Binding = new Binding("Z1") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Z2", Binding = new Binding("Z2") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Start Storey", Binding = new Binding("StartStorey") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Climbable", Binding = new Binding("IsClimbable") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Scale", Binding = new Binding("Scale") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Raw Data", Binding = new Binding("RawDataHex") });

            dataGrid.ItemsSource = walls;
            return dataGrid;
        }
        private DataGrid CreateBuildingsDataGrid(List<Building> buildings)
        {
            DataGrid dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                IsReadOnly = true,
                ItemsSource = buildings
            };

            // Define columns for the DataGrid
            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Starting Wall Index",
                Binding = new Binding("StartingWallIndex"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Ending Wall Index",
                Binding = new Binding("EndingWallIndex"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Roof Type",
                Binding = new Binding("Roof"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Building Type",
                Binding = new Binding("Type"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            // Add a new column for raw data in hex format
            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Raw Data",
                Binding = new Binding("RawDataHex"),
                Width = new DataGridLength(2, DataGridLengthUnitType.Star)
            });

            return dataGrid;
        }


        private async void LoadAsync(string filePath)
        {
            // Check if there are unsaved changes
            if (CheckForUnsavedChanges())
            {
                // Changes were saved, proceed with loading the new map
            }

            //Nothing becomes available until the file is fully loaded
            ModifyButtonStatus(ButtonFlags.None);
            SaveMenuItem.IsEnabled = false;
            SaveAsMenuItem.IsEnabled = false;
            ExportMenuItem.IsEnabled = false;

            // Clear the Prim related arrays and displays if populated from previous load
            gridModel.PrimArray.Clear();
            gridModel.MapWhoArray.Clear();
            OverlayGrid.Children.Clear();

            // Clear the Texture related displays and cells if populated from previous load
            MainContentGrid.Children.Clear();
            MainContentGrid.RowDefinitions.Clear();
            MainContentGrid.ColumnDefinitions.Clear();
            gridModel.Cells.Clear();

            //Clear selected texture so previous world textures may not persist
            ClearSelectedTexture();

            // Close the PrimSelectionWindow if it is open
            if (primSelectionWindow != null && primSelectionWindow.IsLoaded)
            {
                primSelectionWindow.Close();
                primSelectionWindow = null;
            }

            // Close the TextureSelectionWindow if it is open
            if (textureSelectionWindow != null && textureSelectionWindow.IsLoaded)
            {
                textureSelectionWindow.Close();
                textureSelectionWindow = null;
            }


            if (isNewFile)
            {
                // Load the default.iam file from embedded resources
                 var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "UC_MapPainter.Map.default.iam"; // Adjust the namespace if necessary

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    loadedFileBytes = memoryStream.ToArray();
                }
                filePath = "New Unsaved Map"; // Store the loaded file path
            }
            else
            {
                // Load the file from the specified file path
                loadedFilePath = filePath; // Store the loaded file path
                loadedFileBytes = File.ReadAllBytes(filePath);
            }
            modifiedFileBytes = (byte[])loadedFileBytes.Clone(); // New File bytes will be manipulated via edit process
            UpdateWindowTitle(Path.GetFileName(filePath));

            var loadingWindow = new LoadingWindow()
            {
                TaskDescription = "Loading Map"
            };
            loadingWindow.Owner = this;

            loadingWindow.Show();

            // Determine the world number
            loadingWindow.TaskDescription = "Getting World Number";
            selectedWorldNumber = Map.ReadTextureWorld(loadedFileBytes, Map.ReadMapSaveType(loadedFileBytes));

            // Validate the world number
            if (!textureFunctions.IsValidWorld(selectedWorldNumber))
            {
                MessageBox.Show("World not assigned to Map. Please select a world.", "Invalid World", MessageBoxButton.OK, MessageBoxImage.Warning);
                var worldSelectionWindow = new WorldSelectionWindow();
                if (worldSelectionWindow.ShowDialog() == true)
                {
                    selectedWorldNumber = int.Parse(worldSelectionWindow.SelectedWorld);
                    Map.WriteTextureWorld(modifiedFileBytes, selectedWorldNumber, Map.ReadMapSaveType(modifiedFileBytes));
                }
                else
                {
                    return;
                }
            }

            loadingWindow.Close();

            //selectedWorldNumber = 1;

            //MessageBox.Show("File loaded successfully. You can now edit textures, heights, or prims.", "Load Successful", MessageBoxButton.OK, MessageBoxImage.Information);

            SaveMenuItem.IsEnabled = true;
            SaveAsMenuItem.IsEnabled = true;
            ExportMenuItem.IsEnabled = true;

            ModifyButtonStatus(ButtonFlags.All);
        }

        public void Cell_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border cell)
            {
                var position = e.GetPosition(MainContentGrid);
                int pixelX = (int)position.X;
                int pixelZ = (int)position.Y;

                if (currentEditMode == "Textures")
                {
                    int row = 127 - Grid.GetRow(cell);
                    int col = 127 - Grid.GetColumn(cell);
                    int cellOffset = textureFunctions.FindCellTexOffset(col, row);

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
                            cellData.UpdateTileSequence(isDefaultTexture, cellOffset);
                        }
                    }
                }

                else if (currentEditMode == "Height")
                {
                    int row = 127 - Grid.GetRow(cell);
                    int col = 127 - Grid.GetColumn(cell);
                    int cellOffset = textureFunctions.FindCellTexOffset(col, row);

                    var cellData = gridModel.Cells.FirstOrDefault(c => c.Row == row && c.Column == col);
                    if (cellData != null)
                    {
                        int increment = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? 10 : 1;
                        cellData.Height = Math.Min(cellData.Height + increment, 127);
                        cellData.UpdateTileHeight(cellOffset);
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

                else if (currentEditMode == "Buildings")
                {

                    // Get the row and column
                    int row = 127 - Grid.GetRow(cell);
                    int col = 127 - Grid.GetColumn(cell);

                    // Calculate the nearest corner
                    //Point clickedCorner = GetNearestCorner((128 - col) * 64, (128 - row) * 64);

                    Point clickedCorner = new Point(col, row);

                    if (currentStartPoint == null)
                    {
                        // Start a new wall
                        currentStartPoint = clickedCorner;
                        //currentBuildingPoints.Add(clickedCorner);
                    }
                    //else
                    //{
                    //    // Draw the wall and update the state
                    //    DrawWallLine(currentStartPoint.Value, clickedCorner);
                    //    currentStartPoint = clickedCorner;
                    //    //currentBuildingPoints.Add(clickedCorner);
                    //}

                    // Remove the preview line after confirming the wall
                    if (previewLine != null)
                    {
                        OverlayGrid.Children.Remove(previewLine);
                        previewLine = null;
                    }

                    currentBuildingPoints.Add(clickedCorner);

                    // If there's at least one previous point, create a line and a facet
                    if (currentBuildingPoints.Count > 1)
                    {
                        var startPoint = currentBuildingPoints[^2]; // Second-to-last point
                        var endPoint = currentBuildingPoints[^1];   // Last point

                        // Create a new facet
                        var newFacet = new DFacet
                        {
                            //X = new byte[] { (byte)(startPoint.X / 64), (byte)(endPoint.X / 64) },
                            //Z = new byte[] { (byte)(startPoint.Y / 64), (byte)(endPoint.Y / 64) }
                            
                            X = new byte[] { (byte)(startPoint.X), (byte)(endPoint.X ) },
                            Z = new byte[] { (byte)(startPoint.Y), (byte)(endPoint.Y ) },
                            StyleIndex = (ushort) (facets.Count + 1),
                        };

                        facets.Add(newFacet);

                        dstyles.Add(1);


                        // Render the wall line
                        Line wallLine = new Line
                        {
                            Stroke = Brushes.Magenta,
                            StrokeThickness = 12,
                            X1 = (128 - clickedCorner.X          ) * 64,
                            Y1 = (128 - clickedCorner.Y          ) * 64,
                            X2 = (128 - currentStartPoint.Value.X) * 64,
                            Y2 = (128 - currentStartPoint.Value.Y) * 64
                            //IsHitTestVisible = true
                        };

                        //newFacet.wallCanvasLine = wallLine;

                        // Attach mouse event for selection
                        // Doesn't really work
                        //wallLine.MouseDown += WallLine_MouseDown;

                        // Add the wall line to the grid overlay and dictionary
                        OverlayGrid.Children.Add(wallLine);
                        wallLines[wallLine] = newFacet;
                        wallCanvasLines.Add(wallLine);

                        currentFacetIndex++;


                        currentStartPoint = clickedCorner;
                    }


                }

                else if (currentEditMode == "Prims" && selectedPrimNumber != -1)
                {
                    // Calculate necessary values
                    int mapWhoRow = 31 - (pixelZ / 256);
                    int mapWhoCol = 31 - (pixelX / 256);
                    int mapWhoIndex = mapWhoCol * 32 + mapWhoRow;
                    int relativeX = pixelX % 256;
                    int relativeZ = pixelZ % 256;
                    int globalTileX = pixelX / 64;
                    int globalTileZ = pixelZ / 64;

                    // Get the MapWho cell
                    var mapWho = gridModel.MapWhoArray[mapWhoIndex];

                    // Check if the total number of objects exceeds 2000
                    if (gridModel.TotalPrimCount >= 2000)
                    {
                        MessageBox.Show("Can't place Object. Maximum number of objects in the map is 2000", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return; // Exit the method without placing the prim
                    }

                    // Check if the MapWho cell already contains 31 prims
                    if (gridModel.MapWhoPrimCounts[mapWhoIndex] >= 31)
                    {
                        MessageBox.Show("Can't place Object. Maximum number of objects per MapWho cell is 31", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return; // Exit the method without placing the prim
                    }

                    // Get the current yaw from the PrimSelectionWindow
                    byte currentYaw = primSelectionWindow.GetCurrentYaw();
                    byte flags = primSelectionWindow.GetFlagsValue();
                    short currentHeight = primSelectionWindow.GetCurrentHeight();

                    // Create the new Prim object
                    var newPrim = new Prim
                    {
                        PrimNumber = (byte)selectedPrimNumber,
                        X = (byte) (256 - relativeX),
                        Z = (byte) (256 - relativeZ),
                        Y = currentHeight, // Initial Y position, you can modify this later
                        Yaw = currentYaw, // Initial yaw from the PrimSelectionWindow
                        Flags = flags, // Set the flags
                        InsideIndex = 0 // Initial inside idx, modify as necessary
                    };

                    // Add the new Prim to the grid model
                    gridModel.PrimArray.Add(newPrim);

                    // Update the MapWhoPrimCounts and TotalPrimCount
                    gridModel.MapWhoPrimCounts[mapWhoIndex]++;
                    gridModel.TotalPrimCount++;

                    primFunctions.PlacePrim(newPrim, pixelX, pixelZ, mapWhoIndex, mapWhoRow, mapWhoCol, relativeX, relativeZ, globalTileX, globalTileZ, OverlayGrid);

                    // Reset the selected prim - Uncomment to unsticky
                    //selectedPrimNumber = -1;
                    //if (primSelectionWindow != null && primSelectionWindow.IsLoaded)
                    //{
                    //    primSelectionWindow.UpdateSelectedPrimImage(-1); // Clear the selected prim image
                    //}

                    //Update MapWho and Object Section
                    primFunctions.RebuildMapWhoAndPrimArrays(out List<Prim> newPrimArray, out List<MapWho> newMapWhoArray);

                    // Calculate the original object offset and objectSectionSize
                    int saveType = Map.ReadMapSaveType(modifiedFileBytes);
                    int objectBytes = Map.ReadObjectSize(modifiedFileBytes);

                    //Get the physical size of the object section
                    int size = Map.ReadObjectSectionSize(modifiedFileBytes, saveType, objectBytes);

                    // Calculate the object offset
                    int objectOffset = size + 8;
                    // Retrieve the original number of objects
                    int originalNumObjects = Map.ReadNumberPrimObjects(modifiedFileBytes, objectOffset);

                    // Determine the new object section size and number of objects
                    int objectSectionSize = ((newPrimArray.Count + 1) * 8) + 4 + 2048;
                    int numObjects = newPrimArray.Count + 1;

                    // Calculate the difference in object count
                    int objectDifference = numObjects - originalNumObjects;

                    //Old MapWho Offset
                    int originalMapWhoOffset = objectOffset + 4 + (originalNumObjects * 8);

                    // Calculate the new file size
                    int newFileSize = modifiedFileBytes.Length + (objectDifference * 8);

                    // Create the new transitory fileBytes containing the updated object data.
                    byte[] swapFileBytes = new byte[newFileSize];

                    // Copy existing data up to the object offset
                    Array.Copy(modifiedFileBytes, swapFileBytes, objectOffset);

                    // Write the updated object section size in the transitory file
                    Map.WriteObjectSize(swapFileBytes, objectSectionSize);

                    //Write new prims
                    Map.WritePrims(swapFileBytes,newPrimArray,objectOffset);

                    // Insert the new MapWho data after the object data
                    int mapWhoOffset = objectOffset + 4 + ((newPrimArray.Count + 1) * 8);

                    Map.WriteMapWho(swapFileBytes, newMapWhoArray, mapWhoOffset);

                    // Copy any remaining data from the original file
                    if (modifiedFileBytes.Length > originalMapWhoOffset + 2048)
                    {
                        Array.Copy(modifiedFileBytes, originalMapWhoOffset + 2048, swapFileBytes, mapWhoOffset + 2048, modifiedFileBytes.Length - (originalMapWhoOffset + 2048));
                    }
                    
                    modifiedFileBytes = (byte[])swapFileBytes.Clone();
                }

                else if (currentEditMode == "Lights")
                {
                    // Calculate necessary position values
                    int relativeX = pixelX;
                    int relativeZ = pixelZ;

                    // Prepare the new LightEntry with values from LightSelectionWindow sliders
                    LightEntry newLightEntry = new LightEntry
                    {
                        Range = (byte)lightSelectionWindow.RangeSlider.Value,
                        Red = (sbyte)lightSelectionWindow.RedSlider.Value,
                        Green = (sbyte)lightSelectionWindow.GreenSlider.Value,
                        Blue = (sbyte)lightSelectionWindow.BlueSlider.Value,
                        X = (8192 - relativeX) * 4,
                        Y = (int)(lightSelectionWindow.YStoreysSlider.Value * 256),
                        Z = (8192 - relativeZ) * 4,
                        Used = 1
                    };

                    // Check for an available slot or unused entry to overwrite
                    int availableIndex = lightEntries.FindIndex(entry => entry.Used == 0);
                    if (availableIndex != -1)
                    {
                        // Overwrite the unused entry
                        lightEntries[availableIndex] = newLightEntry;

                        // Update EdLightFree to be 1-based and point to the next available slot
                        int nextAvailableIndex = lightEntries.FindIndex(entry => entry.Used == 0);
                        EdLightFree = nextAvailableIndex != -1 ? nextAvailableIndex + 1 : 0;
                    }
                    else if (lightEntries.Count < 255)
                    {
                        // Add the new entry if there’s space under 256 entries
                        lightEntries.Add(newLightEntry);

                        // Update EdLightFree to point to the next available slot in 1-based terms
                        int nextAvailableIndex = lightEntries.FindIndex(entry => entry.Used == 0);
                        EdLightFree = nextAvailableIndex != -1 ? nextAvailableIndex + 1 : 0;
                    }
                    else
                    {
                        // Refuse addition if all entries are used and no free slots are left
                        MessageBox.Show("Cannot add more lights. All 255 LightEntries are in use.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Update the LightSelectionWindow UI
                    if (lightSelectionWindow != null && lightSelectionWindow.IsLoaded)
                    {
                        lightSelectionWindow.SetLightEntries(lightEntries);
                    }

                    // Draw all lights on the OverlayGrid to update the map view
                    lightFunctions.DrawLights(OverlayGrid, lightEntries);
                }
            }
        }


        public void Cell_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Border cell && currentStartPoint != null && currentEditMode == "Buildings")
            {
                // Get the mouse position and calculate the nearest corner
                int row = 127 - Grid.GetRow(cell);
                int col = 127 - Grid.GetColumn(cell);

                Point nearestCorner = new Point(col, row);

                // Update or create the preview line
                if (previewLine == null)
                {
                    previewLine = new Line
                    {
                        Stroke = new SolidColorBrush(Color.FromArgb(128, 255, 0, 255)), // Semi-transparent pink
                        StrokeThickness = 12
                    };
                    OverlayGrid.Children.Add(previewLine);
                }

                previewLine.X1 = (128 - currentStartPoint.Value.X)*64;
                previewLine.Y1 = (128 - currentStartPoint.Value.Y) * 64;
                previewLine.X2 = (128 - nearestCorner.X) * 64;
                previewLine.Y2 = (128 - nearestCorner.Y) * 64;
            }
        }

        // Doesn't really work
        //private void WallLine_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (e.ChangedButton == MouseButton.Middle)
        //    {
        //        Line clickedWall = sender as Line;

        //        // Deselect the previously selected wall
        //        if (selectedWall != null)
        //        {
        //            selectedWall.Stroke = Brushes.Magenta; // Restore the original color
        //        }

        //        // Select the new wall
        //        selectedWall = clickedWall;
        //        selectedWall.Stroke = Brushes.Yellow; // Highlight with a different color

        //        // Optionally, fetch the associated DFacet for editing
        //        if (wallLines.TryGetValue(selectedWall, out DFacet selectedFacet))
        //        {
        //            // Display or modify properties of selectedFacet here
        //            MessageBox.Show($"Selected wall facet: {selectedFacet}");
        //        }
        //    }
        //}

        private double DistanceFromPointToLine(Point p, Point lineStart, Point lineEnd)
        {
            // Calculate the line segment vector
            double lineLengthSquared = Math.Pow(lineEnd.X - lineStart.X, 2) + Math.Pow(lineEnd.Y - lineStart.Y, 2);

            if (lineLengthSquared == 0)
                return Math.Sqrt(Math.Pow(p.X - lineStart.X, 2) + Math.Pow(p.Y - lineStart.Y, 2)); // Line start and end are the same

            // Projection of point onto the line segment
            double t = ((p.X - lineStart.X) * (lineEnd.X - lineStart.X) + (p.Y - lineStart.Y) * (lineEnd.Y - lineStart.Y)) / lineLengthSquared;
            t = Math.Max(0, Math.Min(1, t)); // Clamp to [0, 1] to ensure the projection is on the segment

            // Find the closest point on the line segment
            double closestX = lineStart.X + t * (lineEnd.X - lineStart.X);
            double closestY = lineStart.Y + t * (lineEnd.Y - lineStart.Y);

            // Return the distance between the point and the closest point on the segment
            return Math.Sqrt(Math.Pow(p.X - closestX, 2) + Math.Pow(p.Y - closestY, 2));
        }

        private void SelectWall(Line clickedLine)
        {
            if (selectedWall != null)
            {
                selectedWall.Stroke = Brushes.Magenta; // Restore original color
            }

            selectedWall = clickedLine;
            HighlightSelectedWall(selectedWall);
            //selectedWall.Stroke = Brushes.Yellow; // Highlight selected line

            // Optionally, fetch the associated DFacet for editing
            if (wallLines.TryGetValue(selectedWall, out DFacet facet))
            {
                selectedFacet = facet;
                // Display the selected facet's information (replace with your logic)


                // Pass the selected facet to WallSelectionWindow
                if (wallSelectionWindow == null || !wallSelectionWindow.IsVisible)
                {
                    wallSelectionWindow = new WallSelectionWindow();
                    wallSelectionWindow.Show();
                }

                int i = facets.IndexOf(facet);
                wallSelectionWindow.SetSelectedFacet(selectedFacet, i, dstyles);


                //MessageBox.Show($"Selected wall: X1={selectedFacet.X[0]}, X2={selectedFacet.X[1]}, Z1={selectedFacet.Z[0]}, Z2={selectedFacet.Z[1]}");

                //  selectedFacet.setClimbable();
            }
        }
        private void HighlightSelectedWall(Line highlightedWall)
        {
            highlightedWall.Stroke = Brushes.Yellow; // Highlight selected line
        }
        public void Cell_MouseMiddleButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border cell)
            {
                if (currentEditMode == "Buildings")
                {
                    if (e.ChangedButton == MouseButton.Middle)
                    {
                        // Get the click position
                        //Point clickPoint = e.GetPosition(OverlayGrid);


                        int row = 127 - Grid.GetRow(cell);
                        int col = 127 - Grid.GetColumn(cell);

                        Point clickPoint = new Point(col, row);


                        Line closestLine = null;
                        double minDistance = double.MaxValue;

                        foreach (var kvp in wallLines)
                        {
                            Line wallLine = kvp.Key;
                            DFacet facet = kvp.Value;

                            // Get the start and end points of the line
                            Point lineStart = new Point(facet.X[0], facet.Z[0] );
                            Point lineEnd = new Point(facet.X[1] , facet.Z[1] );

                            // Calculate the distance from the click to this line
                            double distance = DistanceFromPointToLine(clickPoint, lineStart, lineEnd);

                            // Update the closest line if this one is closer
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                closestLine = wallLine;
                            }
                        }

                        // Highlight the closest line
                        if (closestLine != null)
                        {
                            SelectWall(closestLine);
                        }
                    }
                }
            }
        }
        public void Cell_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed && sender is Border cell)
            {
                if (currentEditMode == "Textures")
                {
                    //Do something
                }
                else if (currentEditMode == "Height")
                {
                    int row = 127 - Grid.GetRow(cell);
                    int col = 127 - Grid.GetColumn(cell);
                    int cellOffset = textureFunctions.FindCellTexOffset(col, row);

                    var cellData = gridModel.Cells.FirstOrDefault(c => c.Row == row && c.Column == col);
                    if (cellData != null)
                    {
                        int decrement = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? 10 : 1;
                        cellData.Height = Math.Max(cellData.Height - decrement, -127);
                        cellData.UpdateTileHeight(cellOffset);
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
                else if (currentEditMode == "Buildings")
                {
                    if (currentBuildingPoints.Count < 2)
                    {
                        MessageBox.Show("A building requires at least 2 points!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Create a new building
                    var newBuilding = new DBuilding
                    {
                        StartFacet = (ushort)(currentFacetIndex - currentBuildingPoints.Count + 1),
                        EndFacet = (ushort)currentFacetIndex,
                        Walkable = 1
                    };

                    buildings.Add(newBuilding);

                    // Reset the current building points for the next building
                    currentBuildingPoints.Clear();

                    currentStartPoint = null;

                    MessageBox.Show($"Building created with facets {newBuilding.StartFacet} to {newBuilding.EndFacet}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    OverlayGrid.Children.Remove(previewLine);
                    previewLine = null;
                }
                else if (currentEditMode == "Prims")
                {
                    //do something
                }
                else if (currentEditMode == "Lights")
                {
                    //do something
                }

                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    
                    int row = 127 - Grid.GetRow(cell);
                    int col = 127 - Grid.GetColumn(cell);

                    var cellData = gridModel.Cells.FirstOrDefault(c => c.Row == row && c.Column == col);
                    int cellOffset = textureFunctions.FindCellTexOffset(col, row);
                    byte[] cellBytes = Map.ReadTextureCell(modifiedFileBytes,cellOffset);
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
                                              $"Tile Bytes: {BitConverter.ToString(cellBytes)}"; // Use the stored tile bytes
                        MessageBox.Show(debugMessage, "Cell Debug Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void ModifyButtonStatus(ButtonFlags flags)
        {
            EditTextureButton.IsEnabled = (flags & ButtonFlags.Textures) != 0;
            EditHeightButton.IsEnabled = (flags & ButtonFlags.Height) != 0;
            EditBuildingsButton.IsEnabled = (flags & ButtonFlags.Buildings) != 0;
            EditPrimsButton.IsEnabled = (flags & ButtonFlags.Prims) != 0;
            EditLightsButton.IsEnabled = (flags & ButtonFlags.Lights) != 0;
            EditWallsButton.IsEnabled = (flags & ButtonFlags.Lights) != 0;
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
                    var texturePath = textureFunctions.GetTexturePath(cell.TextureType, cell.TextureNumber, modifiedFileBytes);
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

            // Render the drawing to the RenderTargetBitmap
            renderTargetBitmap.Render(drawingVisual);

            // Save the BMP version
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var bitmapEncoder = new BmpBitmapEncoder();
                bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
                bitmapEncoder.Save(fileStream);
            }

            // Save the PNG version
            string pngFilePath = Path.ChangeExtension(filePath, ".png");
            using (var fileStream = new FileStream(pngFilePath, FileMode.Create))
            {
                var pngEncoder = new PngBitmapEncoder();
                pngEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
                pngEncoder.Save(fileStream);
            }

            MessageBox.Show($"Map exported to {filePath} and {pngFilePath}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
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

        public async void SetEditMode(string mode)
        {
            currentEditMode = mode;

            var loadingWindow = new LoadingWindow();
            loadingWindow.Owner = this;

            switch (mode)
            {
                case "Textures":
                    InitializeTextureSelectionWindow();
                    textureSelectionWindow.SetSelectedWorld(selectedWorldNumber);
                    textureSelectionWindow.LockWorld();
                    textureSelectionWindow.LoadWorldTextures(selectedWorldNumber);
                    ModifyButtonStatus(ButtonFlags.Height | ButtonFlags.Buildings | ButtonFlags.Prims | ButtonFlags.Lights);
                    OverlayGrid.Visibility = Visibility.Collapsed;
                    ClearSelectedTexture();
                    loadingWindow.TaskDescription = "Loading Textures";
                    loadingWindow.Show();
                    await textureFunctions.DrawCells(modifiedFileBytes, selectedWorldNumber);
                    loadingWindow.Close();
                    break;
                case "Height":
                    ModifyButtonStatus(ButtonFlags.Textures | ButtonFlags.Buildings | ButtonFlags.Prims | ButtonFlags.Lights);
                    OverlayGrid.Visibility = Visibility.Collapsed;
                    loadingWindow.TaskDescription = "Loading Heights";
                    loadingWindow.Show();
                    await textureFunctions.DrawCells(modifiedFileBytes, selectedWorldNumber);
                    ClearSelectedTexture();
                    loadingWindow.Close();
                    break;
                case "Buildings":
                    ModifyButtonStatus(ButtonFlags.Textures | ButtonFlags.Height | ButtonFlags.Prims | ButtonFlags.Lights);
                    if (MainContentGrid.Children.Count == 0)
                    {
                        OverlayGrid.Visibility = Visibility.Collapsed;
                        loadingWindow.TaskDescription = "Loading Textures";
                        loadingWindow.Show();
                        textureFunctions.DrawCells(modifiedFileBytes, selectedWorldNumber);
                    }
                    loadingWindow.TaskDescription = "Loading Buildings";
                    loadingWindow.Show();
                    loadingWindow.Close();
                    OverlayGrid.Visibility = Visibility.Visible;
                    ClearSelectedTexture();
                    break;
                case "Prims":
                    ModifyButtonStatus(ButtonFlags.Textures | ButtonFlags.Height | ButtonFlags.Buildings | ButtonFlags.Lights);
                    ClearSelectedTexture();
                    InitializePrimSelectionWindow(); // Open the PrimNumber Selection Window
                    // Get Save Type
                    loadingWindow.TaskDescription = "Getting Map Save Type";
                    int saveType = Map.ReadMapSaveType(modifiedFileBytes);
                    loadingWindow.TaskDescription = "Getting Size of the Object Section";
                    loadingWindow.Show();
                    int objectSectionSize = Map.ReadObjectSize(modifiedFileBytes);
                    loadingWindow.TaskDescription = "Reading Prim Data";
                    if (MainContentGrid.Children.Count == 0)
                    {
                        OverlayGrid.Visibility = Visibility.Collapsed;
                        loadingWindow.TaskDescription = "Loading Textures";
                        loadingWindow.Show();
                        textureFunctions.DrawCells(modifiedFileBytes, selectedWorldNumber);
                    }
                    OverlayGrid.Visibility = Visibility.Visible;
                    // Explicitly await DrawCells before calling ReadObjectData
                    loadingWindow.TaskDescription = "Loading Prims";
                    await primFunctions.ReadObjectData(modifiedFileBytes, saveType, objectSectionSize);
                    loadingWindow.Close();
                    break;
                case "Lights":
                    ModifyButtonStatus(ButtonFlags.Textures | ButtonFlags.Height | ButtonFlags.Buildings | ButtonFlags.Prims);
                    if (MainContentGrid.Children.Count == 0)
                    {
                        loadingWindow.TaskDescription = "Loading Textures";
                        loadingWindow.Show();
                        textureFunctions.DrawCells(modifiedFileBytes, selectedWorldNumber);
                        loadingWindow.Close();
                    }
                    break;
                default:
                    ModifyButtonStatus(ButtonFlags.All); // Enable all buttons by default
                    break;
            }
        }

        private void DrawWallLine(Point start, Point end)
        {
            Brush pink = new SolidColorBrush(Color.FromRgb(255, 0, 255));
            Line wallLine = new Line
            {
                Stroke = pink,
                StrokeThickness = 12,
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                IsHitTestVisible = true // Ensure this line responds to mouse events
            };

            OverlayGrid.Children.Add(wallLine);
        }

        private void DrawMapWhoGrid()
        {
            primFunctions.DrawMapWhoGrid(MapWhoGridCanvas);
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

        public void UpdateSelectedPrim(int primNumber)
        {
            primFunctions.UpdateSelectedPrim(primNumber);
        }

        private bool CheckForUnsavedChanges()
        {
            if (loadedFileBytes != null && modifiedFileBytes != null && !loadedFileBytes.SequenceEqual(modifiedFileBytes))
            {
                var result = MessageBox.Show("It looks like there have been changes to the currently loaded map, would you like to save them?", "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    SaveMap_Click(null, null);
                    return true; // Changes were saved
                }
            }
            return false; // No changes or user chose not to save
        }

        public void SaveAsMap()
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

                File.WriteAllBytes(userFilePath, modifiedFileBytes);
                loadedFilePath = userFilePath; // Store the loaded file path
                loadedFileBytes = File.ReadAllBytes(userFilePath);
                modifiedFileBytes = (byte[])loadedFileBytes.Clone(); // New File bytes will be manipulated via edit process
                UpdateWindowTitle(Path.GetFileName(userFilePath));
                isNewFile = false;
                MessageBox.Show($"Map saved to {userFilePath}", "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

    }
}
