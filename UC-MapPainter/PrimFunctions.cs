using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        //Read the entire object section from file, including all PrimNumber Data and MapWho array.
        public async Task ReadObjectData(byte[] fileBytes, int saveType, int objectBytes)
        {
            //Clear any old objects if we have previously loaded a file
            gridModel.PrimArray.Clear();
            gridModel.MapWhoArray.Clear();

            int fileSize = fileBytes.Length;
            int size = fileSize - 12;

            if (saveType >= 25)
            {
                size -= 2000;  // Adjust for texture data
            }

            size -= objectBytes;  // Subtract the object size
            int objectOffset = size + 8;
            int numObjects = BitConverter.ToInt32(fileBytes, objectOffset);

            // Read OB_Ob PrimNumber array
            for (int i = 0; i < numObjects; i++)
            {
                int index = objectOffset + 4 + (i * 8);
                var prim = new Prim
                {
                    Y = BitConverter.ToInt16(fileBytes, index),
                    X = fileBytes[index + 2],
                    Z = fileBytes[index + 3],
                    PrimNumber = fileBytes[index + 4],
                    Yaw = fileBytes[index + 5],
                    Flags = fileBytes[index + 6],
                    InsideIndex = fileBytes[index + 7]
                };
                gridModel.PrimArray.Add(prim);
            }

            // Read MapWho
            int mapWhoOffset = objectOffset + 4 + (numObjects * 8);
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
        public void DrawPrims(Canvas OverlayGrid)
        {
            OverlayGrid.Children.Clear(); // Clear existing objects
            string appBasePath = AppDomain.CurrentDomain.BaseDirectory;
            string topPrimsFolder = System.IO.Path.Combine(appBasePath, "Prims", "TopPrims");

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

                    string primImagePath = System.IO.Path.Combine(topPrimsFolder, $"{ob.PrimNumber}.png");
                    if (File.Exists(primImagePath))
                    {
                        var primImage = new Image
                        {
                            Source = new BitmapImage(new Uri(primImagePath))
                        };

                        double rotationAngle = -((ob.Yaw / 255.0) * 360);
                        var rotateTransform = new RotateTransform(rotationAngle);
                        primImage.RenderTransform = rotateTransform;
                        primImage.RenderTransformOrigin = new Point(0.5, 0.5);

                        primImage.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        primImage.Arrange(new Rect(0, 0, primImage.DesiredSize.Width, primImage.DesiredSize.Height));

                        Canvas.SetLeft(primImage, finalPixelX - primImage.DesiredSize.Width / 2);
                        Canvas.SetTop(primImage, finalPixelZ - primImage.DesiredSize.Height / 2);
                        OverlayGrid.Children.Add(primImage);
                        Canvas.SetZIndex(primImage, 0);
                    }

                    var ellipse = new Ellipse
                    {
                        Width = 15,
                        Height = 15,
                        Fill = Brushes.Red,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };

                    Canvas.SetLeft(ellipse, finalPixelX - ellipse.Width / 2);
                    Canvas.SetTop(ellipse, finalPixelZ - ellipse.Height / 2);

                    Canvas.SetZIndex(ellipse, 1);

                    ellipse.MouseLeftButtonDown += (s, e) => ShowPrimInfo(ob, mapWhoIndex, mapWhoRow, mapWhoCol, relativeX, relativeZ, globalTileX, globalTileZ, finalPixelX, finalPixelZ);

                    OverlayGrid.Children.Add(ellipse);
                }
            }
        }

        //Show information about the Primwhen it's elipse is selected
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
                        Background = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255))
                    };
                    Canvas.SetLeft(label, col * 4 * 64 + 2);
                    Canvas.SetTop(label, row * 4 * 64 + 2);
                    MapWhoGridCanvas.Children.Add(label);
                }
            }
        }
    }
}
