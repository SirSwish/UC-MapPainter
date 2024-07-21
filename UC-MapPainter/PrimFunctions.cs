using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UC_MapPainter
{
    public class PrimFunctions
    {
        private GridModel gridModel;
        private MainWindow mainWindow;
        private PrimSelectionWindow primSelectionWindow;

        public PrimFunctions(MainWindow mainWindow, GridModel gridModel, PrimSelectionWindow primSelectionWindow)
        {
            this.mainWindow = mainWindow;
            this.gridModel = gridModel;
            this.primSelectionWindow = primSelectionWindow;
        }

        //Read the entire object section from file, including all Prim Data and MapWho array.
        public async Task ReadObjectData(byte[] loadedFileBytes, int saveType, int objectBytes)
        {
            //Clear any old objects if we have previously loaded a file
            gridModel.PrimArray.Clear();
            gridModel.MapWhoArray.Clear();

            int objectOffset = CalculateObjectOffset(loadedFileBytes.Length, saveType, objectBytes);

            //Read prims and add to grid
            List<Prim> primArray = Map.ReadPrims(loadedFileBytes, objectOffset);
            foreach (var prim in primArray)
            {
                gridModel.PrimArray.Add(prim);
            }

            // Read MapWho
            List<MapWho> mapWhoArray = Map.ReadMapWho(loadedFileBytes, objectOffset);
            foreach (var mapWho in mapWhoArray)
            {
                gridModel.MapWhoArray.Add(mapWho);
            }

            gridModel.MapWhoPrimCounts.Clear();
            gridModel.TotalPrimCount = 0;

            // Initialize MapWhoPrimCounts based on loaded MapWho data
            for (int i = 0; i < gridModel.MapWhoArray.Count; i++)
            {
                gridModel.MapWhoPrimCounts[i] = gridModel.MapWhoArray[i].Num;
                gridModel.TotalPrimCount += gridModel.MapWhoArray[i].Num;
            }
        }

        // Calculate the object offset in the file, return as an integer
        public int CalculateObjectOffset(int fileLength, int saveType, int objectBytes)
        {
            int sizeAdjustment = saveType >= 25 ? 2000 : 0;
            return fileLength - 12 - sizeAdjustment - objectBytes + 8;
        }

        //Update the Prim Image preview if the user selects a Prim from the Prim selection Window
        public void UpdateSelectedPrim(int primNumber)
        {
            mainWindow.SelectedPrimNumber = primNumber;
            if (primSelectionWindow != null && primSelectionWindow.IsLoaded)
            {
                primSelectionWindow.UpdateSelectedPrimImage(primNumber);
            }
        }

        //Draw all the Prims to the canvas which overlays the tiles
        public void DrawPrims(Canvas overlayGrid)
        {
            overlayGrid.Children.Clear(); // Clear existing objects

            for (int mapWhoIndex = 0; mapWhoIndex < gridModel.MapWhoArray.Count; mapWhoIndex++)
            {
                var mapWho = gridModel.MapWhoArray[mapWhoIndex];
                int mapWhoRow = mapWhoIndex / 32;
                int mapWhoCol = mapWhoIndex % 32;

                for (int i = 0; i < mapWho.Num; i++)
                {
                    var ob = gridModel.PrimArray[mapWho.Index + i];

                    int relativeX = ob.X & 0xFF;
                    int relativeZ = ob.Z & 0xFF;

                    int startX = 8192 - (mapWhoRow * 256);
                    int startZ = 8192 - (mapWhoCol * 256);

                    int pixelX = startX - relativeX;
                    int pixelZ = startZ - relativeZ;

                    int globalTileX = mapWhoCol * 4 + (relativeX / 64);
                    int globalTileZ = mapWhoRow * 4 + (relativeZ / 64);

                    int finalPixelX = pixelX;
                    int finalPixelZ = pixelZ;

                    PlacePrim(ob, finalPixelX, finalPixelZ, mapWhoIndex, mapWhoRow, mapWhoCol, relativeX, relativeZ, globalTileX, globalTileZ, overlayGrid);
                }
            }
        }

        //Physically draw the Prims to Map
        public void PlacePrim(Prim prim, int pixelX, int pixelZ, int mapWhoIndex, int mapWhoRow, int mapWhoCol, int relativeX, int relativeZ, int globalTileX, int globalTileZ, Canvas overlayGrid)
        {
            prim.PixelX = pixelX;
            prim.PixelZ = pixelZ;
            prim.MapWhoIndex = mapWhoIndex;

            string appBasePath = AppDomain.CurrentDomain.BaseDirectory;
            string topPrimsFolder = System.IO.Path.Combine(appBasePath, "Prims", "TopPrims");

            // Add the Prim image to the overlay grid
            string primImagePath = System.IO.Path.Combine(topPrimsFolder, $"{prim.PrimNumber}.png");
            if (File.Exists(primImagePath))
            {
                var primImage = new Image
                {
                    Source = new BitmapImage(new Uri(primImagePath))
                };

                double rotationAngle = -((prim.Yaw / 255.0) * 360);
                var rotateTransform = new RotateTransform(rotationAngle);
                primImage.RenderTransform = rotateTransform;
                primImage.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

                primImage.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                primImage.Arrange(new System.Windows.Rect(0, 0, primImage.DesiredSize.Width, primImage.DesiredSize.Height));

                Canvas.SetLeft(primImage, pixelX - primImage.DesiredSize.Width / 2);
                Canvas.SetTop(primImage, pixelZ - primImage.DesiredSize.Height / 2);
                overlayGrid.Children.Add(primImage);
                Canvas.SetZIndex(primImage, 0);
            }

            // Draw the ellipse on the overlay grid
            var ellipse = new Ellipse
            {
                Width = 15,
                Height = 15,
                Fill = Brushes.Red,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            Canvas.SetLeft(ellipse, pixelX - ellipse.Width / 2);
            Canvas.SetTop(ellipse, pixelZ - ellipse.Height / 2);

            Canvas.SetZIndex(ellipse, 1);

            ellipse.MouseLeftButtonDown += (s, e) =>
            {
                ShowPrimInfo(prim, mapWhoIndex, mapWhoRow, mapWhoCol, relativeX, relativeZ, globalTileX, globalTileZ, pixelX, pixelZ);
            };

            overlayGrid.Children.Add(ellipse);
        }

        public void RebuildMapWhoAndPrimArrays(out List<Prim> newPrimArray, out List<MapWho> newMapWhoArray)
        {
            // Sort the prims by their MapWho index
            gridModel.PrimArray.Sort((a, b) => a.MapWhoIndex.CompareTo(b.MapWhoIndex));

            List<Prim> sortedPrimArray = gridModel.PrimArray.Where(p => p.PrimNumber != 0).ToList();

            // Initialize new arrays as lists
            newPrimArray = new List<Prim>();
            newMapWhoArray = new List<MapWho>(new MapWho[1024]);

            // Initialize the newMapWhoArray with empty MapWho entries
            for (int i = 0; i < 1024; i++)
            {
                newMapWhoArray[i] = new MapWho
                {
                    Index = 0,
                    Num = 0
                };
            }

            // Rebuild the Prim array
            newPrimArray.AddRange(sortedPrimArray);

            // Rebuild the MapWho array
            int currentMapWhoIndex = -1;
            int currentPrimCount = 0;
            int currentPrimStartIndex = 0;
            for (int i = 0; i < sortedPrimArray.Count; i++)
            {
                var prim = sortedPrimArray[i];
                int mapWhoIndex = prim.MapWhoIndex;
                if (mapWhoIndex != currentMapWhoIndex)
                {
                    if (currentMapWhoIndex != -1)
                    {
                        newMapWhoArray[currentMapWhoIndex].Index = currentPrimStartIndex+1;
                        newMapWhoArray[currentMapWhoIndex].Num = currentPrimCount;
                        currentPrimStartIndex += currentPrimCount;
                    }

                    // Update the currentMapWhoIndex and reset the count
                    currentMapWhoIndex = mapWhoIndex;
                    currentPrimCount = 0;
                }
                currentPrimCount++;
            }

            // Set the MapWho entry for the last cell
            if (currentMapWhoIndex != -1)
            {
                newMapWhoArray[currentMapWhoIndex].Index = currentPrimStartIndex+1;
                newMapWhoArray[currentMapWhoIndex].Num = currentPrimCount;
            }
        }


        //Show information about the Prim when it's elipse is selected
        public void ShowPrimInfo(Prim ob, int mapWhoIndex, int mapWhoRow, int mapWhoCol, int relativeX, int relativeZ, int globalTileX, int globalTileZ, int pixelX, int pixelZ)
        {
            string objectInfo = $"MapWho Index: {mapWhoIndex}\n" +
                                $"MapWho Cell: [{mapWhoRow}, {mapWhoCol}]\n" +
                                $"X Position in MapWho: {relativeX}\n" +
                                $"Z Position in MapWho: {relativeZ}\n" +
                                $"Y: {ob.Y}\n" +
                                $"PrimNumber: {ob.PrimNumber} ({ob.DisplayName})\n" +
                                $"Yaw: {ob.Yaw}\n" +
                                $"Flags: {ob.Flags}\n" +
                                $"InsideIndex: {ob.InsideIndex}\n" +
                                $"Calculated Tile Position: TileX = {globalTileX}, TileZ = {globalTileZ}\n" +
                                $"Calculated Pixel Position: PixelX = {8192 - pixelX}, PixelZ = {8192 - pixelZ}";
            MessageBox.Show(objectInfo, "Prim Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void DisplayObjectData()
        {
            var sb = new StringBuilder();
            foreach (var ob in gridModel.PrimArray)
            {
                sb.AppendLine($"Y: {ob.Y}, X: {ob.X}, Z: {ob.Z}, Prim: {ob.PrimNumber} ({ob.DisplayName}), Yaw: {ob.Yaw}, Flags: {ob.Flags}, InsideIndex: {ob.InsideIndex}");
            }

            ShowScrollablePopup("Prim Information", sb.ToString());
        }

        public void DisplayMapWhoInfo()
        {
            var sb = new StringBuilder();
            foreach (var mw in gridModel.MapWhoArray)
            {
                sb.AppendLine($"Index: {mw.Index}, Num: {mw.Num}");
            }

            ShowScrollablePopup("MapWho Information", sb.ToString());
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

        //Draw the MapWho grid and cell numbers to the canvas
        public void DrawMapWhoGrid(Canvas MapWhoGridCanvas)
        {
            MapWhoGridCanvas.Children.Clear();

            for (int i = 0; i <= 128; i += 4)
            {
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

            for (int row = 0; row < 32; row++)
            {
                for (int col = 0; col < 32; col++)
                {
                    int index = (31 - row) * 32 + (31 - col);
                    var label = new TextBlock
                    {
                        Text = $"Index: {index}\nRow: {31 - row}\nCol: {31 - col}",
                        Foreground = Brushes.Red,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 255, 255, 255))
                    };
                    Canvas.SetLeft(label, col * 4 * 64 + 2);
                    Canvas.SetTop(label, row * 4 * 64 + 2);
                    MapWhoGridCanvas.Children.Add(label);
                }
            }
        }
    }
}
