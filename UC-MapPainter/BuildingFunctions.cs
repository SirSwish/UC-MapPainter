using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace UC_MapPainter
{
    public class BuildingFunctions
    {
        private PrimFunctions primFunctions;
        private MainWindow mainWindow;
        Brush fluorescentGreen = new SolidColorBrush(Color.FromRgb(0, 255, 0)); // Bright green

        // Constructor to initialize PrimFunctions and MainWindow references
        public BuildingFunctions(PrimFunctions primFunctions, MainWindow mainWindow)
        {
            this.primFunctions = primFunctions;
            this.mainWindow = mainWindow;
        }

        public void DumpBuildingData()
        {
            try
            {
                byte[] buildingData = GetBuildingData();
                string executableDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = System.IO.Path.Combine(executableDirectory, "BuildingData.bin");

                File.WriteAllBytes(filePath, buildingData);
                MessageBox.Show($"Building data dumped to {filePath}", "Dump Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to dump building data: {ex.Message}", "Dump Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Overloaded method to get building data as a hex string for display purposes
        public string DumpBuildingData(bool returnAsText)
        {
            try
            {
                byte[] buildingData = GetBuildingData();
                return BitConverter.ToString(buildingData).Replace("-", " ");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to retrieve building data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }
        }

        // Helper method to calculate the building data bytes
        private byte[] GetBuildingData()
        {
            int objectOffset = primFunctions.CalculateObjectOffset(mainWindow.modifiedFileBytes.Length, Map.ReadMapSaveType(mainWindow.modifiedFileBytes), Map.ReadObjectSize(mainWindow.modifiedFileBytes));
            return Map.ReadBuildingData(mainWindow.modifiedFileBytes, objectOffset);
        }

        public void DrawBuildings(byte[] modifiedFileBytes, Canvas overlayGrid)
        {
            // Clear existing overlay drawings if any
            overlayGrid.Children.Clear();

            try
            {
                // Extract Building Header
                int buildingHeaderOffset = Map.BuildingDataOffset;
                byte[] buildingHeader = new byte[48];
                Array.Copy(modifiedFileBytes, buildingHeaderOffset, buildingHeader, 0, 48);

                // Read total number of buildings from the header
                int totalBuildings = BitConverter.ToUInt16(buildingHeader, 2) - 1;

                // Read total number of walls from the header
                int totalWalls = BitConverter.ToUInt16(buildingHeader, 4) - 1;

                // Calculate the walls section offset
                int wallDataOffset = buildingHeaderOffset + 48 + (totalBuildings * 24) + 14;

                // Read walls data
                List<Wall> walls = new List<Wall>();
                for (int i = 0; i < totalWalls; i++)
                {
                    Wall wall = Wall.ReadWall(modifiedFileBytes, wallDataOffset + i * 26);
                    walls.Add(wall);
                }

                // Modified DrawBuildings method to pass the building number to DrawWall
                int buildingDataOffset = buildingHeaderOffset + 48;
                for (int i = 0; i < totalBuildings; i++)
                {
                    Building building = Building.ReadBuilding(modifiedFileBytes, buildingDataOffset + i * 24);

                    // Ensure the ending index is correctly interpreted
                    int numWalls = (building.EndingWallIndex - building.StartingWallIndex);

                    // Iterate through the range of walls for the building
                    for (int idx = 0; idx < numWalls; idx++)
                    {
                        DrawWall(walls[(building.StartingWallIndex + idx) - 1], overlayGrid, i + 1, (building.StartingWallIndex + idx));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to draw building data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Draws a wall on the OverlayGrid based on wall coordinates
        private void DrawWall(Wall wall, Canvas overlayGrid, int buildingNumber, int wallIndex)
        {
            // Draw the wall line
            Line wallLine = new Line
            {
                Stroke = fluorescentGreen,
                StrokeThickness = 3,  // Adjusted for better visualization
                X1 = (128 - wall.X1) * 64,  // Assuming 64 is the size of each cell in the grid
                Y1 = (128 - wall.Z1) * 64,
                X2 = (128 - wall.X2) * 64,
                Y2 = (128 - wall.Z2) * 64
            };

            // Modify the wall appearance based on WallTypeDescription
            switch (wall.WallTypeDescription)
            {
                case "Cable":
                    wallLine.Stroke = Brushes.Red;
                    break;
                case "Ladder":
                    wallLine.Stroke = Brushes.Orange;
                    wallLine.StrokeThickness = 12;
                    break;
                case "Barbed Wire Fence":
                case "Chain Fence":
                case "Jumpable Chain Fence":
                case "Unclimbable Bar Fence":
                    wallLine.Stroke = Brushes.Yellow;
                    break;
                case "Door":
                    wallLine.Stroke = Brushes.Purple;
                    wallLine.StrokeThickness = 12;
                    break;
            }

            overlayGrid.Children.Add(wallLine);

            // Draw a label (TextBlock) for the wall number
            TextBlock wallLabel = new TextBlock
            {
                Text = $"B:{buildingNumber} W:{wallIndex}",
                Foreground = Brushes.Black,
                Background = Brushes.White,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(2)
            };


            // Calculate the midpoint of the line to position the label
            double midX = (wallLine.X1 + wallLine.X2) / 2;
            double midY = (wallLine.Y1 + wallLine.Y2) / 2;

            Canvas.SetLeft(wallLabel, midX);
            Canvas.SetTop(wallLabel, midY);

            overlayGrid.Children.Add(wallLabel);
        }
    }
}
