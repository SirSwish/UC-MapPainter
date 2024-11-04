using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace UC_MapPainter
{
    public class LightFunctions
    {
        private MainWindow mainWindow;
        private LightSelectionWindow lightSelectionWindow;
        private const uint NIGHT_FLAG_LIGHTS_UNDER_LAMPOSTS = 1 << 0;
        private const uint NIGHT_FLAG_DARKEN_BUILDING_POINTS = 1 << 1;
        private const uint NIGHT_FLAG_DAYTIME = 1 << 2;

        public LightFunctions(MainWindow mainWindow, LightSelectionWindow lightSelectionWindow)
        {
            this.mainWindow = mainWindow;
            this.lightSelectionWindow = lightSelectionWindow;
        }

        // Read Light Header from the light file bytes
        public LightHeader ReadLightHeader(byte[] lightFileBytes)
        {
            if (lightFileBytes == null || lightFileBytes.Length < 12)
            {
                throw new ArgumentException("Invalid light file bytes.");
            }

            LightHeader lightHeader = new LightHeader
            {
                SizeOfEdLight = BitConverter.ToInt32(lightFileBytes, 0),
                EdMaxLights = BitConverter.ToInt32(lightFileBytes, 4),
                SizeOfNightColour = BitConverter.ToInt32(lightFileBytes, 8)
            };

            return lightHeader;
        }

        // Reads the Light Entries from the byte array
        public List<LightEntry> ReadLightEntries(byte[] lightFileBytes)
        {
            const int headerSize = 12; // Header is 12 bytes long
            const int headerPadding = 20; //Technically a "light entry", but is always reserved / never used
            const int entrySize = 20; // Each light entry is 20 bytes
            const int totalEntries = 255; // Exactly 255 light entries
            int offset = headerSize + headerPadding;

            List<LightEntry> lightEntries = new List<LightEntry>();

            for (int i = 0; i < totalEntries; i++)
            {
                LightEntry lightEntry = new LightEntry
                {
                    Range = lightFileBytes[offset],
                    Red = (sbyte)lightFileBytes[offset + 1],
                    Green = (sbyte)lightFileBytes[offset + 2],
                    Blue = (sbyte)lightFileBytes[offset + 3],
                    Next = lightFileBytes[offset + 4],
                    Used = lightFileBytes[offset + 5],
                    Flags = lightFileBytes[offset + 6],
                    Padding = lightFileBytes[offset + 7],
                    X = BitConverter.ToInt32(lightFileBytes, offset + 8),
                    Y = BitConverter.ToInt32(lightFileBytes, offset + 12),
                    Z = BitConverter.ToInt32(lightFileBytes, offset + 16)
                };

                lightEntries.Add(lightEntry);
                offset += entrySize;
            }

            return lightEntries;
        }

        // Read Light Properties from the light file bytes
        public LightProperties ReadLightProperties(byte[] lightFileBytes)
        {
            if (lightFileBytes == null || lightFileBytes.Length < 51)
            {
                throw new ArgumentException("Invalid light file bytes.");
            }

            int offset = 12 + 20 + (255 * 20); // Start after the header and light entries (assuming 256 light entries)

            LightProperties lightProperties = new LightProperties
            {
                EdLightFree = BitConverter.ToInt32(lightFileBytes, offset),
                NightFlag = BitConverter.ToUInt32(lightFileBytes, offset + 4),
                NightAmbD3DColour = BitConverter.ToUInt32(lightFileBytes, offset + 8),
                NightAmbD3DSpecular = BitConverter.ToUInt32(lightFileBytes, offset + 12),
                NightAmbRed = BitConverter.ToInt32(lightFileBytes, offset + 16),
                NightAmbGreen = BitConverter.ToInt32(lightFileBytes, offset + 20),
                NightAmbBlue = BitConverter.ToInt32(lightFileBytes, offset + 24),
                NightLampostRed = (sbyte)lightFileBytes[offset + 28],
                NightLampostGreen = (sbyte)lightFileBytes[offset + 29],
                NightLampostBlue = (sbyte)lightFileBytes[offset + 30],
                Padding = lightFileBytes[offset + 31],
                NightLampostRadius = BitConverter.ToInt32(lightFileBytes, offset + 32)
            };

            return lightProperties;
        }

        // Read Light Night Colour from the light file bytes
        public LightNightColour ReadLightNightColour(byte[] lightFileBytes)
        {
            if (lightFileBytes == null || lightFileBytes.Length < 3)
            {
                throw new ArgumentException("Invalid light file bytes.");
            }

            int offset = 12 + 20 + (255 * 20) + 36; // Start after the header, light entries, and light properties

            LightNightColour lightNightColour = new LightNightColour
            {
                Red = lightFileBytes[offset],
                Green = lightFileBytes[offset + 1],
                Blue = lightFileBytes[offset + 2]
            };

            return lightNightColour;
        }

        public void DrawLights(Canvas overlayGrid, List<LightEntry> lightEntries)
        {
            overlayGrid.Children.Clear();

            foreach (var light in lightEntries)
            {
                if (light.Used == 1) // Only draw if the light is marked as used
                {
                    double canvasX = (32768 - light.X) / 4.0;
                    double canvasZ = (32768 - light.Z) / 4.0;
                    double ellipseSize = 64 * (light.Range / 255.0);

                    Ellipse lightEllipse = new Ellipse
                    {
                        Width = ellipseSize,
                        Height = ellipseSize,
                        Fill = new SolidColorBrush(Color.FromArgb(128, (byte)(light.Red + 128), (byte)(light.Green + 128), (byte)(light.Blue + 128))),
                        Stroke = Brushes.Black,
                        StrokeThickness = 1,
                        Tag = light // Store the LightEntry object in the Tag property for easy access
                    };

                    // Set the position of the ellipse on the canvas
                    Canvas.SetLeft(lightEllipse, canvasX - (ellipseSize / 2));
                    Canvas.SetTop(lightEllipse, canvasZ - (ellipseSize / 2));

                    // Add click event handler to display LightEntry information
                    lightEllipse.MouseLeftButtonUp += LightEllipse_MouseLeftButtonUp;

                    // Attach right-click event handler for deletion
                    lightEllipse.MouseRightButtonDown += LightEllipse_MouseRightButtonDown;

                    // Add the ellipse to the overlay grid
                    overlayGrid.Children.Add(lightEllipse);
                }
            }

            overlayGrid.Visibility = Visibility.Visible;
        }

        private void LightEllipse_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse ellipse && ellipse.Tag is LightEntry clickedLight)
            {
                int index = mainWindow.lightEntries.IndexOf(clickedLight);

                if (index != -1)
                {
                    // Mark the LightEntry as unused
                    mainWindow.lightEntries[index].Used = 0;

                    // Update EdLightFree to the next available entry
                    mainWindow.EdLightFree = mainWindow.lightEntries.FindIndex(entry => entry.Used == 0) + 1;

                    mainWindow.OverlayGrid.Children.Clear();

                    // Update the LightSelectionWindow UI
                    if (lightSelectionWindow != null && lightSelectionWindow.IsLoaded)
                    {
                        lightSelectionWindow.SetLightEntries(mainWindow.lightEntries);
                    }

                    // Redraw the lights on the overlay
                    DrawLights(mainWindow.OverlayGrid, mainWindow.lightEntries);
                }
            }
        }

        // Event handler to handle click on the ellipse
        private void LightEllipse_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse ellipse && ellipse.Tag is LightEntry lightEntry)
            {
                ShowLightDetailsPopup(lightEntry);
            }
        }
        private void ShowLightDetailsPopup(LightEntry lightEntry)
        {
            // Create a new Window for displaying light details
            Window detailsWindow = new Window
            {
                Title = "Light Details",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            // Create a main StackPanel to hold all elements and a secondary StackPanel for the color preview
            StackPanel mainPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10) };
            StackPanel detailsPanel = new StackPanel { Margin = new Thickness(10) };

            // Helper function to add a TextBlock with label and value
            void AddDetail(string label, string value, StackPanel panel)
            {
                TextBlock detailText = new TextBlock();
                detailText.Inlines.Add(new Run(label) { FontWeight = FontWeights.Bold });
                detailText.Inlines.Add(new Run($" {value}"));
                detailText.Margin = new Thickness(0, 5, 0, 5);
                panel.Children.Add(detailText);
            }

            // Range on a single line
            AddDetail("Range:", lightEntry.Range.ToString(), detailsPanel);

            // R, G, B on a single line
            StackPanel rgbPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            rgbPanel.Children.Add(new TextBlock { Text = "R:", FontWeight = FontWeights.Bold });
            rgbPanel.Children.Add(new TextBlock { Text = $" {lightEntry.Red} ", Margin = new Thickness(5, 0, 10, 0) });
            rgbPanel.Children.Add(new TextBlock { Text = "G:", FontWeight = FontWeights.Bold });
            rgbPanel.Children.Add(new TextBlock { Text = $" {lightEntry.Green} ", Margin = new Thickness(5, 0, 10, 0) });
            rgbPanel.Children.Add(new TextBlock { Text = "B:", FontWeight = FontWeights.Bold });
            rgbPanel.Children.Add(new TextBlock { Text = $" {lightEntry.Blue} " });
            detailsPanel.Children.Add(rgbPanel);

            // Next Free Light Index and Used Flag
            AddDetail("Next Free Light Index:", lightEntry.Next.ToString(), detailsPanel);
            AddDetail("Used Flag:", lightEntry.Used.ToString(), detailsPanel);

            // Flags and Position (X, Y, Z) values
            AddDetail("Flags (Bitflag):", lightEntry.Flags.ToString("X2"), detailsPanel);
            AddDetail("X-Pos:", (lightEntry.X / 256).ToString(), detailsPanel);
            AddDetail("Y-Pos:", (lightEntry.Y / 256).ToString(), detailsPanel);
            AddDetail("Z-Pos:", (lightEntry.Z / 256).ToString(), detailsPanel);

            // Create the color preview box
            Rectangle colorPreviewBox = new Rectangle
            {
                Width = 50,
                Height = 50,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Margin = new Thickness(20, 0, 0, 0),
                Fill = new SolidColorBrush(Color.FromArgb(
                    255, // Fully opaque
                    (byte)(lightEntry.Red + 128),
                    (byte)(lightEntry.Green + 128),
                    (byte)(lightEntry.Blue + 128)
                ))
            };

            // Add the details panel and the color preview box to the main panel
            mainPanel.Children.Add(detailsPanel);
            mainPanel.Children.Add(colorPreviewBox);

            // Set the content of the window to the main panel and show it
            detailsWindow.Content = mainPanel;
            detailsWindow.ShowDialog();
        }


    }
}
