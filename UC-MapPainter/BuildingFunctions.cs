using System;
using System.IO;
using System.Windows;

namespace UC_MapPainter
{
    public class BuildingFunctions
    {
        private PrimFunctions primFunctions;
        private MainWindow mainWindow;

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
                int objectOffset = primFunctions.CalculateObjectOffset(mainWindow.modifiedFileBytes.Length, Map.ReadMapSaveType(mainWindow.modifiedFileBytes), Map.ReadObjectSize(mainWindow.modifiedFileBytes));
                byte[] buildingData = Map.ReadBuildingData(mainWindow.modifiedFileBytes, objectOffset);

                string executableDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = Path.Combine(executableDirectory, "BuildingData.bin");

                File.WriteAllBytes(filePath, buildingData);
                MessageBox.Show($"Building data dumped to {filePath}", "Dump Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to dump building data: {ex.Message}", "Dump Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
