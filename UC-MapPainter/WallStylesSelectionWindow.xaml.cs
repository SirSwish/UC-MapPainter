using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32; // Add this namespace for OpenFileDialog

namespace UC_MapPainter
{
    public partial class WallStylesSelectionWindow : Window
    {
        private int _worldNumber;
        private TMAFile _tmaFile; // Store the TMA data globally within the class
        private Border _previousSelectedBorder = null;



        public WallStylesSelectionWindow(int worldNumber)
        {
            InitializeComponent();
            _worldNumber = worldNumber;
            LoadTMAFile(false); // Initially load the default TMA file
        }

        // Load TMA file button click
        private void LoadTmaButton_Click(object sender, RoutedEventArgs e)
        {
            LoadTMAFile(true); // Pass true to open the file dialog
        }

        // Save TMA file button click (currently stubbed)
        private void SaveTmaButton_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for save functionality
            MessageBox.Show("Save functionality is not yet implemented.", "Save TMA", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TextureEntry_Click(object sender, MouseButtonEventArgs e)
        {
            // Get the Border that was clicked
            if (sender is Border clickedBorder && clickedBorder.Tag is Tuple<int, int> indices)
            {
                int styleIndex = indices.Item1;
                int entryIndex = indices.Item2;

                // Reset the border color of the previously selected texture entry
                if (_previousSelectedBorder != null)
                {
                    _previousSelectedBorder.BorderBrush = Brushes.Black;
                }

                // Set the border color of the currently selected texture entry to red
                clickedBorder.BorderBrush = Brushes.Red;

                // Update the reference to the currently selected border
                _previousSelectedBorder = clickedBorder;

                // Retrieve the corresponding flag
                if (_tmaFile != null && _tmaFile.TextureStyles.Count > styleIndex)
                {
                    var textureStyle = _tmaFile.TextureStyles[styleIndex];

                    if (_tmaFile.SaveType > 2 && textureStyle.Flags != null && entryIndex < textureStyle.Flags.Count)
                    {
                        TextureFlag flag = textureStyle.Flags[entryIndex];
                        byte flipValue = textureStyle.Entries[entryIndex].Flip;
                        // Update checkboxes
                        UpdateCheckboxFlags(flag, flipValue);
                    }
                    else
                    {
                        // No flags available, clear checkboxes
                        ClearCheckboxFlags();
                    }
                }
            }
        }

        // Load the TMA file and populate the UI accordingly
        private void LoadTMAFile(bool custom)
        {
            try
            {
                string tmaFilePath;

                if (custom)
                {
                    // Open a file dialog to select the TMA file
                    OpenFileDialog openFileDialog = new OpenFileDialog
                    {
                        Filter = "TMA files (*.tma)|*.tma|All files (*.*)|*.*"
                    };

                    if (openFileDialog.ShowDialog() == true)
                    {
                        tmaFilePath = openFileDialog.FileName;
                    }
                    else
                    {
                        // User canceled the dialog, so we exit the method
                        return;
                    }
                }
                else
                {
                    // Construct the default file path for the TMA file
                    tmaFilePath = $"textures/world{_worldNumber}/style.tma";
                }

                // Check if the file exists
                if (File.Exists(tmaFilePath))
                {
                    // Load the TMA file using the TMAReader
                    TMAReader tmaReader = new TMAReader();
                    _tmaFile = tmaReader.ReadTMAFile(tmaFilePath); // Store in class member

                    // Set the FilePathLabel content to the loaded file path
                    FilePathLabel.Content = tmaFilePath;

                    // Clear the styles stack panel to remove any previous rows
                    StylesStackPanel.Children.Clear();

                    // Clear the checkboxes
                    ClearCheckboxFlags();

                    // Reset the previously selected border
                    _previousSelectedBorder = null;

                    // Iterate through each style in the TMA file and create a row for each one
                    for (int styleIndex = 0; styleIndex < _tmaFile.TextureStyles.Count; styleIndex++)
                    {
                        var textureStyle = _tmaFile.TextureStyles[styleIndex];

                        StackPanel styleRowPanel = new StackPanel
                        {
                            Orientation = Orientation.Vertical,
                            Margin = new Thickness(5, 10, 5, 10)
                        };

                        // Create a horizontal panel for the texture entries
                        StackPanel entriesPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Margin = new Thickness(0, 5, 0, 5)
                        };

                        // Iterate through each entry in the texture style
                        for (int entryIndex = 0; entryIndex < textureStyle.Entries.Count; entryIndex++)
                        {
                            var textureEntry = textureStyle.Entries[entryIndex];
                            Border textureBorder = new Border
                            {
                                BorderBrush = Brushes.Black,
                                BorderThickness = new Thickness(1),
                                Margin = new Thickness(5)
                            };

                            StackPanel textureEntryPanel = new StackPanel
                            {
                                Width = 64,
                                Height = 64
                            };

                            string imagePath = GetTextureImagePath(textureEntry);

                            if (imagePath != null)
                            {
                                try
                                {
                                    Image textureImage = new Image
                                    {
                                        Source = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute)),
                                        Width = 64,
                                        Height = 64,
                                        Stretch = Stretch.UniformToFill
                                    };

                                    // Apply flip if needed
                                    ApplyFlipTransform(textureImage, textureEntry.Flip);

                                    textureEntryPanel.Children.Add(textureImage);
                                }
                                catch (Exception)
                                {
                                    // Handle exceptions related to image loading
                                    TextBlock errorText = new TextBlock
                                    {
                                        Text = "Image Load Error",
                                        TextWrapping = TextWrapping.Wrap,
                                        HorizontalAlignment = HorizontalAlignment.Center
                                    };
                                    textureEntryPanel.Children.Add(errorText);
                                }
                            }
                            else
                            {
                                // Display details if image is not found
                                TextBlock entryText = new TextBlock
                                {
                                    Text = $"(Page: {textureEntry.Page}, Tx: {textureEntry.Tx}, Ty: {textureEntry.Ty}, Flip: {textureEntry.Flip})",
                                    TextWrapping = TextWrapping.Wrap,
                                    HorizontalAlignment = HorizontalAlignment.Center
                                };
                                textureEntryPanel.Children.Add(entryText);
                            }

                            textureBorder.Child = textureEntryPanel;

                            // Assign indices and event handler
                            textureBorder.Tag = new Tuple<int, int>(styleIndex, entryIndex);
                            textureBorder.MouseLeftButtonDown += TextureEntry_Click;

                            entriesPanel.Children.Add(textureBorder);
                        }

                        // Add style number label before the style name
                        TextBlock styleNumberLabel = new TextBlock
                        {
                            Text = $"Style {styleIndex}:",
                            FontWeight = FontWeights.Bold,
                            Margin = new Thickness(5, 0, 0, 5)
                        };

                        // Add style name label
                        TextBlock styleNameLabel = new TextBlock
                        {
                            Text = textureStyle.Name,
                            FontWeight = FontWeights.Bold,
                            Margin = new Thickness(5, 0, 0, 5)
                        };

                        // Create a panel to hold both labels
                        StackPanel labelsPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Margin = new Thickness(5, 0, 0, 5)
                        };

                        labelsPanel.Children.Add(styleNumberLabel);
                        labelsPanel.Children.Add(styleNameLabel);

                        // Add the labels panel and entries panel to the style row panel
                        styleRowPanel.Children.Add(labelsPanel);
                        styleRowPanel.Children.Add(entriesPanel);

                        // Add the entire style row panel to the styles stack panel
                        StylesStackPanel.Children.Add(styleRowPanel);
                    }

                    // No need to set checkboxes here; they update on click
                }
                else
                {
                    // If no file is found, display default information
                    FilePathLabel.Content = "style.tma (Default)";
                }
            }
            catch (Exception ex)
            {
                // Show an error message if something goes wrong while loading the file
                MessageBox.Show($"Failed to load TMA file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Update checkbox states based on the provided list of flags
        private void UpdateCheckboxFlags(TextureFlag flag, byte flipValue)
        {
            GouraudCheckBox.IsChecked = flag.HasFlag(TextureFlag.Gouraud);
            TexturedCheckBox.IsChecked = flag.HasFlag(TextureFlag.Textured);
            MaskedCheckBox.IsChecked = flag.HasFlag(TextureFlag.Masked);
            TransparentCheckBox.IsChecked = flag.HasFlag(TextureFlag.Transparent);
            AlphaCheckBox.IsChecked = flag.HasFlag(TextureFlag.Alpha);
            TiledCheckBox.IsChecked = flag.HasFlag(TextureFlag.Tiled);
            TwoSidedCheckBox.IsChecked = flag.HasFlag(TextureFlag.TwoSided);
            // Update Flipped checkbox based on flipValue
            FlippedCheckBox.IsChecked = flipValue == 1;
        }

        private void ClearCheckboxFlags()
        {
            GouraudCheckBox.IsChecked = false;
            TexturedCheckBox.IsChecked = false;
            MaskedCheckBox.IsChecked = false;
            TransparentCheckBox.IsChecked = false;
            AlphaCheckBox.IsChecked = false;
            TiledCheckBox.IsChecked = false;
            TwoSidedCheckBox.IsChecked = false;
            FlippedCheckBox.IsChecked = false;
        }

        private void ApplyFlipTransform(Image image, byte flipValue)
        {
            if (flipValue == 1)
            {
                ScaleTransform flipTransform = new ScaleTransform
                {
                    ScaleX = -1, // Flip horizontally
                    ScaleY = 1,  // No vertical flip
                    CenterX = 0.5,
                    CenterY = 0.5
                };

                // Set the RenderTransformOrigin to the center
                image.RenderTransformOrigin = new Point(0.5, 0.5);
                image.RenderTransform = flipTransform;
            }
            // No flip applied if flipValue is 0
        }

        private string GetTextureImagePath(TextureEntry textureEntry)
        {
            int indexInPage = textureEntry.Ty * 8 + textureEntry.Tx;
            int totalImageIndex = textureEntry.Page * 64 + indexInPage;

            // Determine the base directory and file path
            string imageFileName = $"tex{totalImageIndex:D3}hi.bmp"; // D3 formats the number to 3 digits with leading zeros
            string imageFilePath;

            if (textureEntry.Page >= 0 && textureEntry.Page <= 3)
            {
                // World textures
                string appBasePath = AppDomain.CurrentDomain.BaseDirectory;
                imageFilePath = Path.Combine(appBasePath,$"Textures/world{_worldNumber}", imageFileName);
            }
            else if (textureEntry.Page >= 4 && textureEntry.Page <= 7)
            {
                // Shared textures
                string appBasePath = AppDomain.CurrentDomain.BaseDirectory;
                imageFilePath = Path.Combine(appBasePath,"Textures/shared", imageFileName);
            }
            else if (textureEntry.Page == 8)
            {
                // Inside textures
                string appBasePath = AppDomain.CurrentDomain.BaseDirectory;
                imageFilePath = Path.Combine(appBasePath,$"Textures/world{_worldNumber}/insides", imageFileName);
            }
            else
            {
                // Invalid page, handle accordingly
                imageFilePath = null;
            }

            // Check if the file exists
            if (imageFilePath != null && File.Exists(imageFilePath))
            {
                return imageFilePath;
            }
            else
            {
                // Return a placeholder or null if the image does not exist
                return null;
            }
        }
    }
}
