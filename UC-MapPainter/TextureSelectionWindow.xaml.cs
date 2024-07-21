using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace UC_MapPainter
{
    public partial class TextureSelectionWindow : Window
    {
        private bool _isLoaded = false;
        private MainWindow _mainWindow;
        private bool _isWorldLocked = false;

        public TextureSelectionWindow()
        {
            InitializeComponent();
            this.Loaded += TextureSelectionWindow_Loaded;
        }

        public void SetMainWindow(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        private async void TextureSelectionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Wait until the window is fully rendered
            await Task.Delay(1000);

            // Mark the window as loaded
            _isLoaded = true;

            // Load shared and prim textures once
            LoadSharedAndPrimTextures();

            // Trigger the initial texture load
            LoadInitialTextures();
        }

        private void LoadSharedAndPrimTextures()
        {
            LoadTexturesFromFolder("Textures/shared", SharedTexturesGrid, "shared");
            LoadTexturesFromFolder("Textures/shared/prims", PrimTexturesGrid, "prims");
        }

        private void LoadTexturesFromFolder(string folderPath, UniformGrid grid, string type)
        {
            string appBasePath = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.Combine(appBasePath, folderPath);

            if (Directory.Exists(fullPath))
            {
                foreach (var file in Directory.GetFiles(fullPath, "*.bmp"))
                {
                    var image = new Image
                    {
                        Source = new BitmapImage(new Uri(file)),
                        Width = 64,
                        Height = 64,
                        Margin = new Thickness(2),
                        Tag = new TextureInfo { Type = type, FilePath = file }
                    };
                    image.MouseLeftButtonDown += TextureImage_MouseLeftButtonDown;
                    grid.Children.Add(image);
                }
            }
        }

        private void LoadInitialTextures()
        {
            if (!_isLoaded)
                return;

            // Manually set the initial selection to trigger texture loading
            WorldNumberComboBox.SelectedIndex = 0;
        }

        private void WorldNumberComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (WorldNumberComboBox.SelectedItem is ComboBoxItem selectedItem && !_isWorldLocked)
            {
                // Parse the content to an integer
                if (int.TryParse(selectedItem.Content.ToString(), out int selectedWorld))
                {
                    LoadWorldTextures(selectedWorld);
                }
                else
                {
                    // Handle the case where the content is not a valid integer
                    MessageBox.Show("The selected item's content is not a valid integer.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        public void LoadWorldTextures(int world)
        {
            try
            {
                // Clear existing textures in the WorldTexturesGrid
                WorldTexturesGrid.Children.Clear();
                string worldNumber = world.ToString(); // Convert the integer to a string

                // Load textures from the selected world folder
                string appBasePath = AppDomain.CurrentDomain.BaseDirectory;
                string worldFolderPath = Path.Combine(appBasePath, "Textures/world" + worldNumber);

                if (Directory.Exists(worldFolderPath))
                {
                    foreach (var file in Directory.GetFiles(worldFolderPath, "*.bmp"))
                    {
                        if (Path.GetFileName(file).ToLower() != "sky.bmp")
                        {
                            var image = new Image
                            {
                                Source = new BitmapImage(new Uri(file)),
                                Width = 64,
                                Height = 64,
                                Margin = new Thickness(2),
                                Tag = new TextureInfo { Type = "world", FilePath = file }
                            };
                            image.MouseLeftButtonDown += TextureImage_MouseLeftButtonDown;
                            WorldTexturesGrid.Children.Add(image);
                        }
                    }
                }

                // Load World Sky texture
                string worldSkyPath = Path.Combine(worldFolderPath, "sky.bmp");

                if (File.Exists(worldSkyPath))
                {
                    WorldSkyImage.Source = new BitmapImage(new Uri(worldSkyPath));
                }
                else
                {
                    WorldSkyImage.Source = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error during LoadWorldTextures: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TextureImage_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Image image && _mainWindow != null)
            {
                if (image.Tag is TextureInfo textureInfo)
                {
                    _mainWindow.UpdateSelectedTexture(image.Source, textureInfo.Type, GetTextureNumberFromFilePath(textureInfo.FilePath));
                }
            }
        }

        private int GetTextureNumberFromFilePath(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            if (fileName.StartsWith("tex") && fileName.EndsWith("hi"))
            {
                string numberPart = fileName.Substring(3, 3);
                if (int.TryParse(numberPart, out int number))
                {
                    return number;
                }
            }
            return -1; // Default or error value
        }

        public int GetSelectedWorld()
        {
            if (WorldNumberComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                // Ensure the content is a string that can be parsed to an integer
                if (int.TryParse(selectedItem.Content.ToString(), out int result))
                {
                    return result;
                }
                else
                {
                    throw new InvalidOperationException("The selected item's content is not a valid integer.");
                }
            }
            return -1;
        }

        public bool IsWorldLocked
        {
            get { return _isWorldLocked; }
        }

        public void LockWorld()
        {
            _isWorldLocked = true;
            WorldNumberComboBox.IsEnabled = false;
        }

        public void UnlockWorld()
        {
            _isWorldLocked = false;
            WorldNumberComboBox.IsEnabled = true;
        }

        public void SetSelectedWorld(int world)
        {
            string worldString = world.ToString(); // Convert the integer to a string
            foreach (ComboBoxItem item in WorldNumberComboBox.Items)
            {
                if (item.Content.ToString() == worldString) // Compare the string representation
                {
                    WorldNumberComboBox.SelectedItem = item;
                    break;
                }
            }
        }
    }

    public class TextureInfo
    {
        public string Type { get; set; }
        public string FilePath { get; set; }
    }
}
