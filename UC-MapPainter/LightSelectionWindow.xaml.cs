using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace UC_MapPainter
{
    public partial class LightSelectionWindow : Window
    {
        private MainWindow _mainWindow;
        private string _lightFilePath;
        private const uint NIGHT_FLAG_LIGHTS_UNDER_LAMPOSTS = 1 << 0;
        private const uint NIGHT_FLAG_DARKEN_BUILDING_POINTS = 1 << 1;
        private const uint NIGHT_FLAG_DAYTIME = 1 << 2;

        public LightSelectionWindow()
        {
            InitializeComponent();
            this.Loaded += LightSelectionWindow_Loaded;

            // Initialize slider value change event handlers
            RangeSlider.ValueChanged += RangeSlider_ValueChanged;
            RedSlider.ValueChanged += UpdateLightColorPreview;
            GreenSlider.ValueChanged += UpdateLightColorPreview;
            BlueSlider.ValueChanged += UpdateLightColorPreview;
            YStoreysSlider.ValueChanged += YStoreysSlider_ValueChanged;

            D3DAlphaSlider.ValueChanged += (s, e) => UpdateD3DColorPreview();
            D3DRedSlider.ValueChanged += (s, e) => UpdateD3DColorPreview();
            D3DGreenSlider.ValueChanged += (s, e) => UpdateD3DColorPreview();
            D3DBlueSlider.ValueChanged += (s, e) => UpdateD3DColorPreview();

            SpecularAlphaSlider.ValueChanged += (s, e) => UpdateSpecularColorPreview();
            SpecularRedSlider.ValueChanged += (s, e) => UpdateSpecularColorPreview();
            SpecularGreenSlider.ValueChanged += (s, e) => UpdateSpecularColorPreview();
            SpecularBlueSlider.ValueChanged += (s, e) => UpdateSpecularColorPreview();

            AmbientRedSlider.ValueChanged += UpdateAmbientColorPreview;
            AmbientGreenSlider.ValueChanged += UpdateAmbientColorPreview;
            AmbientBlueSlider.ValueChanged += UpdateAmbientColorPreview;

            LampostRedSlider.ValueChanged += UpdateLampostColorPreview;
            LampostGreenSlider.ValueChanged += UpdateLampostColorPreview;
            LampostBlueSlider.ValueChanged += UpdateLampostColorPreview;
            LampostRadiusSlider.ValueChanged += LampostRadiusSlider_ValueChanged;

            NightSkyRedSlider.ValueChanged += UpdateNightSkyColorPreview;
            NightSkyGreenSlider.ValueChanged += UpdateNightSkyColorPreview;
            NightSkyBlueSlider.ValueChanged += UpdateNightSkyColorPreview;

            // Night Flags Checkbox event handlers
            LampostsLightCheckbox.Checked += NightFlagCheckbox_Changed;
            LampostsLightCheckbox.Unchecked += NightFlagCheckbox_Changed;
            DarkenBuildingPointsCheckbox.Checked += NightFlagCheckbox_Changed;
            DarkenBuildingPointsCheckbox.Unchecked += NightFlagCheckbox_Changed;
            DaytimeCheckbox.Checked += NightFlagCheckbox_Changed;
            DaytimeCheckbox.Unchecked += NightFlagCheckbox_Changed;
        }

        public void SetMainWindow(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        private void LightSelectionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _lightFilePath = _mainWindow.loadedLightFilePath;
        }

        public void UpdateNightFlags()
        {
            if (_mainWindow != null)
            {
                uint nightFlag = _mainWindow.lightProperties.NightFlag;
                LampostsLightCheckbox.IsChecked = (nightFlag & NIGHT_FLAG_LIGHTS_UNDER_LAMPOSTS) != 0;
                DarkenBuildingPointsCheckbox.IsChecked = (nightFlag & NIGHT_FLAG_DARKEN_BUILDING_POINTS) != 0;
                DaytimeCheckbox.IsChecked = (nightFlag & NIGHT_FLAG_DAYTIME) != 0;
            }
        }

        private void NightFlagCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (_mainWindow != null)
            {
                uint nightFlag = 0;
                if (LampostsLightCheckbox.IsChecked == true)
                    nightFlag |= NIGHT_FLAG_LIGHTS_UNDER_LAMPOSTS;
                if (DarkenBuildingPointsCheckbox.IsChecked == true)
                    nightFlag |= NIGHT_FLAG_DARKEN_BUILDING_POINTS;
                if (DaytimeCheckbox.IsChecked == true)
                    nightFlag |= NIGHT_FLAG_DAYTIME;

                _mainWindow.NightFlag = nightFlag;
            }
        }

        private void LoadLightingFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Lighting Files (*.lgt)|*.lgt",
                Title = "Load Lighting File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _lightFilePath = openFileDialog.FileName;

                // Read the selected file as bytes
                _mainWindow.loadedLightFileBytes = File.ReadAllBytes(_lightFilePath);
                _mainWindow.modifiedLightFileBytes = (byte[])_mainWindow.loadedLightFileBytes.Clone();

                // Parse the lighting file to update light properties and entries
                _mainWindow.lightHeader = _mainWindow.lightFunctions.ReadLightHeader(_mainWindow.modifiedLightFileBytes);
                _mainWindow.lightEntries = _mainWindow.lightFunctions.ReadLightEntries(_mainWindow.modifiedLightFileBytes);
                _mainWindow.lightProperties = _mainWindow.lightFunctions.ReadLightProperties(_mainWindow.modifiedLightFileBytes);
                _mainWindow.lightNightColor = _mainWindow.lightFunctions.ReadLightNightColour(_mainWindow.modifiedLightFileBytes);

                // Update the UI elements with the new values
                SetLightProperties(_mainWindow.lightProperties);
                SetLightNightColour(_mainWindow.lightNightColor);
                SetLightEntries(_mainWindow.lightEntries);

                // Redraw the lights on the map
                _mainWindow.lightFunctions.DrawLights(_mainWindow.OverlayGrid, _mainWindow.lightEntries);
            }
        }

        private void SaveLightingFileButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Lighting Files (*.lgt)|*.lgt",
                Title = "Save Lighting File"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                WriteLightFile(filePath);
            }
        }

        private void YStoreysSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            YStoreysValueLabel.Text = e.NewValue.ToString("F0");
        }

        private void UpdateLightColorPreview(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            RedValueLabel.Text = RedSlider.Value.ToString("F0");
            GreenValueLabel.Text = GreenSlider.Value.ToString("F0");
            BlueValueLabel.Text = BlueSlider.Value.ToString("F0");

            LightColorPreview.Fill = new SolidColorBrush(Color.FromRgb(
                (byte)(RedSlider.Value + 128),
                (byte)(GreenSlider.Value + 128),
                (byte)(BlueSlider.Value + 128)
            ));
        }

        private void UpdateD3DColorPreview()
        {
            D3DAlphaValueLabel.Text = D3DAlphaSlider.Value.ToString("F0");
            D3DRedValueLabel.Text = D3DRedSlider.Value.ToString("F0");
            D3DGreenValueLabel.Text = D3DGreenSlider.Value.ToString("F0");
            D3DBlueValueLabel.Text = D3DBlueSlider.Value.ToString("F0");

            D3DColorPreview.Fill = new SolidColorBrush(Color.FromArgb(
                (byte)D3DAlphaSlider.Value,
                (byte)(D3DRedSlider.Value),
                (byte)(D3DGreenSlider.Value),
                (byte)(D3DBlueSlider.Value)
            ));

        }

        private void UpdateSpecularColorPreview()
        {
            SpecularAlphaValueLabel.Text = SpecularAlphaSlider.Value.ToString("F0");
            SpecularRedValueLabel.Text = SpecularRedSlider.Value.ToString("F0");
            SpecularGreenValueLabel.Text = SpecularGreenSlider.Value.ToString("F0");
            SpecularBlueValueLabel.Text = SpecularBlueSlider.Value.ToString("F0");

            SpecularColorPreview.Fill = new SolidColorBrush(Color.FromArgb(
                (byte)SpecularAlphaSlider.Value,
                (byte)(SpecularRedSlider.Value),
                (byte)(SpecularGreenSlider.Value),
                (byte)(SpecularBlueSlider.Value)
            ));
        }

        private void UpdateAmbientColorPreview(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AmbientRedValueLabel.Text = AmbientRedSlider.Value.ToString("F0");
            AmbientGreenValueLabel.Text = AmbientGreenSlider.Value.ToString("F0");
            AmbientBlueValueLabel.Text = AmbientBlueSlider.Value.ToString("F0");

            AmbientColorPreview.Fill = new SolidColorBrush(Color.FromRgb(
                (byte)(AmbientRedSlider.Value + 128),
                (byte)(AmbientGreenSlider.Value + 128),
                (byte)(AmbientBlueSlider.Value + 128)
            ));
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // Cancel the close operation
            this.Hide();     // Hide the window - the window never needs to be destroyed once initialized
            _mainWindow.LightSelectionMenuItem.IsEnabled = true;
        }

        private void UpdateLampostColorPreview(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LampostRedValueLabel.Text = LampostRedSlider.Value.ToString("F0");
            LampostGreenValueLabel.Text = LampostGreenSlider.Value.ToString("F0");
            LampostBlueValueLabel.Text = LampostBlueSlider.Value.ToString("F0");

            LampostColorPreview.Fill = new SolidColorBrush(Color.FromRgb(
                (byte)(LampostRedSlider.Value + 128),
                (byte)(LampostGreenSlider.Value + 128),
                (byte)(LampostBlueSlider.Value + 128)
            ));
        }

        private void LampostRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LampostRadiusValueLabel.Text = e.NewValue.ToString("F0");
        }

        private void UpdateNightSkyColorPreview(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            NightSkyRedValueLabel.Text = NightSkyRedSlider.Value.ToString("F0");
            NightSkyGreenValueLabel.Text = NightSkyGreenSlider.Value.ToString("F0");
            NightSkyBlueValueLabel.Text = NightSkyBlueSlider.Value.ToString("F0");

            NightSkyColorPreview.Fill = new SolidColorBrush(Color.FromRgb(
                (byte)(NightSkyRedSlider.Value),
                (byte)(NightSkyGreenSlider.Value),
                (byte)(NightSkyBlueSlider.Value)
            ));
        }

        private void RangeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            RangeValueLabel.Text = e.NewValue.ToString("F0");
        }

        public void SetLightProperties(LightProperties lightProperties)
        {
            // Set the range and night flags
            RangeSlider.Value = lightProperties.NightAmbRed;

            LampostsLightCheckbox.IsChecked = (lightProperties.NightFlag & NIGHT_FLAG_LIGHTS_UNDER_LAMPOSTS) != 0;
            DarkenBuildingPointsCheckbox.IsChecked = (lightProperties.NightFlag & NIGHT_FLAG_DARKEN_BUILDING_POINTS) != 0;
            DaytimeCheckbox.IsChecked = (lightProperties.NightFlag & NIGHT_FLAG_DAYTIME) != 0;

            // D3D color components
            D3DAlphaSlider.Value = lightProperties.D3DAlpha;
            D3DRedSlider.Value = lightProperties.D3DRed;
            D3DGreenSlider.Value = lightProperties.D3DGreen;
            D3DBlueSlider.Value = lightProperties.D3DBlue;

            // Specular color components
            SpecularAlphaSlider.Value = lightProperties.SpecularAlpha;
            SpecularRedSlider.Value = lightProperties.SpecularRed;
            SpecularGreenSlider.Value = lightProperties.SpecularGreen;
            SpecularBlueSlider.Value = lightProperties.SpecularBlue;

            // Set Night Ambient color components (unsigned)
            AmbientRedSlider.Value = (byte)(lightProperties.NightAmbRed);
            AmbientGreenSlider.Value = (byte)(lightProperties.NightAmbGreen);
            AmbientBlueSlider.Value = (byte)(lightProperties.NightAmbBlue);

            // Set Lamppost color and radius
            LampostRedSlider.Value = lightProperties.NightLampostRed;
            LampostGreenSlider.Value = lightProperties.NightLampostGreen;
            LampostBlueSlider.Value = lightProperties.NightLampostBlue;
            LampostRadiusSlider.Value = lightProperties.NightLampostRadius;
        }


        public void SetLightNightColour(LightNightColour lightNightColour)
        {
            NightSkyRedSlider.Value = lightNightColour.Red;
            NightSkyGreenSlider.Value = lightNightColour.Green;
            NightSkyBlueSlider.Value = lightNightColour.Blue;
        }

        public void SetLightEntries(List<LightEntry> lightEntries)
        {
            LightEntriesGrid.Children.Clear();

            for (int i = 0; i < lightEntries.Count; i++)
            {
                var entry = lightEntries[i];

                if (entry.Used == 1)
                {
                    int posX = entry.X / 256;
                    int posY = entry.Y / 256;
                    int posZ = entry.Z / 256;

                    var textBlock = new TextBlock
                    {
                        Text = $"Light {i + 1}: Range: {entry.Range}, R: {entry.Red}, G: {entry.Green}, B: {entry.Blue}, Position: ({posX}, {posY}, {posZ})",
                        Margin = new Thickness(5)
                    };

                    LightEntriesGrid.Children.Add(textBlock);
                }
            }
        }

        public void UpdateD3DColourSliders()
        {
            if (_mainWindow != null)
            {
                D3DAlphaSlider.Value = _mainWindow.lightProperties.D3DAlpha;
                D3DRedSlider.Value = _mainWindow.lightProperties.D3DRed;
                D3DGreenSlider.Value = _mainWindow.lightProperties.D3DGreen;
                D3DBlueSlider.Value = _mainWindow.lightProperties.D3DBlue;
                UpdateD3DColorPreview();
            }
        }

        public void UpdateSpecularSliders()
        {
            if (_mainWindow != null)
            {
                SpecularAlphaSlider.Value = _mainWindow.lightProperties.SpecularAlpha;
                SpecularRedSlider.Value = _mainWindow.lightProperties.SpecularRed;
                SpecularGreenSlider.Value = _mainWindow.lightProperties.SpecularGreen;
                SpecularBlueSlider.Value = _mainWindow.lightProperties.SpecularBlue;
                UpdateSpecularColorPreview();
            }
        }

        private void WriteLightFile(string filePath)
        {
            // Retrieve the header bytes from the initial file or embedded resource
            byte[] headerBytes = new byte[12];
            if (_mainWindow.loadedLightFileBytes != null && _mainWindow.loadedLightFileBytes.Length >= 12)
            {
                Array.Copy(_mainWindow.loadedLightFileBytes, 0, headerBytes, 0, 12);
            }
            else
            {
                MessageBox.Show("No header data available to write the .lgt file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Get the LightEntries from MainWindow
            List<LightEntry> lightEntries = _mainWindow.lightEntries;

            // Calculate EdLightFree
            int edLightFree = lightEntries.FindIndex(entry => entry.Used == 0) + 1;
            if (edLightFree == 0) edLightFree = 0; // If no unused entries, set to 0

            // Collect values from UI controls

            // Night Flags
            uint nightFlag = 0;
            if (LampostsLightCheckbox.IsChecked == true)
                nightFlag |= NIGHT_FLAG_LIGHTS_UNDER_LAMPOSTS;
            if (DarkenBuildingPointsCheckbox.IsChecked == true)
                nightFlag |= NIGHT_FLAG_DARKEN_BUILDING_POINTS;
            if (DaytimeCheckbox.IsChecked == true)
                nightFlag |= NIGHT_FLAG_DAYTIME;

            // D3D Color components
            byte d3dAlpha = (byte)D3DAlphaSlider.Value;
            byte d3dRed = (byte)D3DRedSlider.Value;
            byte d3dGreen = (byte)D3DGreenSlider.Value;
            byte d3dBlue = (byte)D3DBlueSlider.Value;

            // Combine D3D color into a uint (ARGB format)
            uint nightAmbD3DColour = (uint)((d3dAlpha << 24) | (d3dRed << 16) | (d3dGreen << 8) | d3dBlue);

            // Specular Color components
            byte specularAlpha = (byte)SpecularAlphaSlider.Value;
            byte specularRed = (byte)SpecularRedSlider.Value;
            byte specularGreen = (byte)SpecularGreenSlider.Value;
            byte specularBlue = (byte)SpecularBlueSlider.Value;

            // Combine Specular color into a uint (ARGB format)
            uint nightAmbD3DSpecular = (uint)((specularAlpha << 24) | (specularRed << 16) | (specularGreen << 8) | specularBlue);

            // Night Ambient colors (SLONGs)
            int nightAmbRed = (int)AmbientRedSlider.Value;
            int nightAmbGreen = (int)AmbientGreenSlider.Value;
            int nightAmbBlue = (int)AmbientBlueSlider.Value;

            // Lamppost color (SBYTEs)
            sbyte nightLampostRed = (sbyte)LampostRedSlider.Value;
            sbyte nightLampostGreen = (sbyte)LampostGreenSlider.Value;
            sbyte nightLampostBlue = (sbyte)LampostBlueSlider.Value;

            // Lamppost Radius (SLONG)
            int nightLampostRadius = (int)LampostRadiusSlider.Value;

            // Night Sky Color (UBYTEs)
            byte nightSkyRed = (byte)NightSkyRedSlider.Value;
            byte nightSkyGreen = (byte)NightSkyGreenSlider.Value;
            byte nightSkyBlue = (byte)NightSkyBlueSlider.Value;

            // Create the byte array for the .lgt file
            int totalSize = 5171;
            byte[] lgtFileBytes = new byte[totalSize];
            int offset = 0;

            // Write the header
            Array.Copy(headerBytes, 0, lgtFileBytes, offset, headerBytes.Length);
            offset += headerBytes.Length;

            // Write 20 bytes of padding (zeros)
            offset += 20; // The array is initialized with zeros by default

            // Write the 255 LightEntries
            for (int i = 0; i < 255; i++)
            {
                LightEntry entry = i < lightEntries.Count ? lightEntries[i] : new LightEntry();

                lgtFileBytes[offset++] = entry.Range;
                lgtFileBytes[offset++] = unchecked((byte)entry.Red);
                lgtFileBytes[offset++] = unchecked((byte)entry.Green);
                lgtFileBytes[offset++] = unchecked((byte)entry.Blue);
                lgtFileBytes[offset++] = entry.Next;
                lgtFileBytes[offset++] = entry.Used;
                lgtFileBytes[offset++] = entry.Flags;
                lgtFileBytes[offset++] = entry.Padding;

                // Write X (4 bytes, little-endian)
                Array.Copy(BitConverter.GetBytes(entry.X), 0, lgtFileBytes, offset, 4);
                offset += 4;

                // Write Y (4 bytes, little-endian)
                Array.Copy(BitConverter.GetBytes(entry.Y), 0, lgtFileBytes, offset, 4);
                offset += 4;

                // Write Z (4 bytes, little-endian)
                Array.Copy(BitConverter.GetBytes(entry.Z), 0, lgtFileBytes, offset, 4);
                offset += 4;
            }

            // Write EdLightFree (4 bytes)
            Array.Copy(BitConverter.GetBytes(edLightFree), 0, lgtFileBytes, offset, 4);
            offset += 4;

            // Write night_flag (4 bytes)
            Array.Copy(BitConverter.GetBytes(nightFlag), 0, lgtFileBytes, offset, 4);
            offset += 4;

            // Write night_amb_d3d_colour (4 bytes)
            Array.Copy(BitConverter.GetBytes(nightAmbD3DColour), 0, lgtFileBytes, offset, 4);
            offset += 4;

            // Write night_amb_d3d_specular (4 bytes)
            Array.Copy(BitConverter.GetBytes(nightAmbD3DSpecular), 0, lgtFileBytes, offset, 4);
            offset += 4;

            // Write night_amb_red (4 bytes)
            Array.Copy(BitConverter.GetBytes(nightAmbRed), 0, lgtFileBytes, offset, 4);
            offset += 4;

            // Write night_amb_green (4 bytes)
            Array.Copy(BitConverter.GetBytes(nightAmbGreen), 0, lgtFileBytes, offset, 4);
            offset += 4;

            // Write night_amb_blue (4 bytes)
            Array.Copy(BitConverter.GetBytes(nightAmbBlue), 0, lgtFileBytes, offset, 4);
            offset += 4;

            // Write night_lampost_red (1 byte)
            lgtFileBytes[offset++] = unchecked((byte)nightLampostRed);

            // Write night_lampost_green (1 byte)
            lgtFileBytes[offset++] = unchecked((byte)nightLampostGreen);

            // Write night_lampost_blue (1 byte)
            lgtFileBytes[offset++] = unchecked((byte)nightLampostBlue);

            // Write padding (1 byte)
            lgtFileBytes[offset++] = 0;

            // Write night_lampost_radius (4 bytes)
            Array.Copy(BitConverter.GetBytes(nightLampostRadius), 0, lgtFileBytes, offset, 4);
            offset += 4;

            // Write NIGHT_Colour (3 bytes)
            lgtFileBytes[offset++] = nightSkyRed;
            lgtFileBytes[offset++] = nightSkyGreen;
            lgtFileBytes[offset++] = nightSkyBlue;

            // Write the byte array to the file
            try
            {
                File.WriteAllBytes(filePath, lgtFileBytes);
                MessageBox.Show($"Lighting file saved successfully to {filePath}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while saving the lighting file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


    }
}
