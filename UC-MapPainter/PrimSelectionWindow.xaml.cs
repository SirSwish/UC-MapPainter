using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UC_MapPainter
{
    public partial class PrimSelectionWindow : Window
    {
        private MainWindow _mainWindow;
        private int selectedPrimNumber = -1;
        private byte yaw = 0;
        private short height = 0;

        public PrimSelectionWindow()
        {
            InitializeComponent();
            this.Loaded += PrimSelectionWindow_Loaded;
        }

        private void PrimSelectionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPrimButtons();
        }
        private void LoadPrimButtons()
        {
            string appBasePath = AppDomain.CurrentDomain.BaseDirectory;
            string buttonPrimsFolder = Path.Combine(appBasePath, "Prims", "ButtonPrims");

            for (int i = 1; i <= 255; i++) // Note: start from 1 as there is no 000.png
            {
                string primFilePath = Path.Combine(buttonPrimsFolder, $"{i:D3}.png");
                if (File.Exists(primFilePath))
                {
                    var button = new Button
                    {
                        Content = new Image
                        {
                            Source = new BitmapImage(new Uri(primFilePath)),
                            Width = 64,
                            Height = 64
                        },
                        Width = 64,
                        Height = 64,
                        Tag = i // Store the prim number in the Tag property
                    };
                    button.Click += PrimButton_Click;
                    PrimGrid.Children.Add(button);
                }
            }
        }

        private void PrimButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int primNumber)
            {
                _mainWindow.SelectedPrimNumber = primNumber;
                UpdateSelectedPrimTopImage(primNumber);
            }
        }

        public void UpdateSelectedPrimTopImage(int primNumber)
        {
            if (primNumber == -1)
            {
                SelectedPrimImage.Source = null;
            }
            else
            {
                string appBasePath = AppDomain.CurrentDomain.BaseDirectory;
                string topPrimImagePath = Path.Combine(appBasePath, "Prims", "TopPrims", $"{primNumber}.png");
                if (File.Exists(topPrimImagePath))
                {
                    var bitmap = new BitmapImage(new Uri(topPrimImagePath));
                    SelectedPrimImage.Source = bitmap;
                }
            }
        }

        public void UpdateSelectedPrimImage(int primNumber)
        {
            if (primNumber == -1)
            {
                SelectedPrimImage.Source = null;
            }
            else
            {
                string appBasePath = AppDomain.CurrentDomain.BaseDirectory;
                string primImagePath = Path.Combine(appBasePath, "Prims", "ButtonPrims", $"{primNumber:D3}.png");
                if (File.Exists(primImagePath))
                {
                    var bitmap = new BitmapImage(new Uri(primImagePath));
                    SelectedPrimImage.Source = bitmap;
                }
            }
        }

        public void SetMainWindow(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        private void LoadPrims()
        {
            for (int i = 0; i < 256; i++)
            {
                var border = new Border
                {
                    Background = Brushes.LightGray,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(2),
                    Width = 64,
                    Height = 64,
                    Child = new TextBlock
                    {
                        Text = i.ToString(),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    }
                };
                border.MouseLeftButtonDown += Prim_MouseLeftButtonDown;
                PrimGrid.Children.Add(border);
            }
        }

        public byte GetFlagsValue()
        {
            byte flags = 0;

            if (OnFloorCheckBox.IsChecked == true) flags |= 1 << 0;
            if (SearchableCheckBox.IsChecked == true) flags |= 1 << 1;
            if (NotOnPsxCheckBox.IsChecked == true) flags |= 1 << 2;
            if (DamagedCheckBox.IsChecked == true) flags |= 1 << 3;
            if (WarehouseCheckBox.IsChecked == true) flags |= 1 << 4;
            if (HiddenItemCheckBox.IsChecked == true) flags |= 1 << 5;
            if (Reserved1CheckBox.IsChecked == true) flags |= 1 << 6;
            if (Reserved2CheckBox.IsChecked == true) flags |= 1 << 7;

            return flags;
        }


        private void Prim_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Child is TextBlock textBlock)
            {
                selectedPrimNumber = int.Parse(textBlock.Text);
                SelectedPrimImage.Source = new DrawingImage(new GeometryDrawing
                {
                    Geometry = new RectangleGeometry(new Rect(0, 0, 64, 64)),
                    Brush = Brushes.LightGray,
                    Pen = new Pen(Brushes.Black, 2)
                });

                if (_mainWindow != null)
                {
                    _mainWindow.UpdateSelectedPrim(selectedPrimNumber);
                }
            }
        }

        private void RotateLeftButton_Click(object sender, RoutedEventArgs e)
        {
            yaw = (byte)((yaw - 1 + 256) % 256); // Decrease yaw by 1 and wrap around
            ApplyRotation();
        }

        private void RotateRightButton_Click(object sender, RoutedEventArgs e)
        {
            yaw = (byte)((yaw + 1) % 256); // Increase yaw by 1 and wrap around
            ApplyRotation();
        }
        private void ApplyRotation()
        {
            if (SelectedPrimImage.Source != null)
            {
                double rotationAngle = -((yaw / 255.0) * 360);
                var rotateTransform = new RotateTransform(rotationAngle, 32, 32);
                SelectedPrimImage.RenderTransform = rotateTransform;
            }
        }

        public byte GetCurrentYaw()
        {
            return yaw;
        }

        public short GetCurrentHeight()
        {
            return height;
        }

        private void AdjustHeightButton_Click(object sender, RoutedEventArgs e)
        {
            HeightAdjustmentPanel.Visibility = Visibility.Visible;
            HeightSlider.Value = height;
            HeightTextBox.Text = height.ToString();
            UpdateStoreyLabel();
        }

        private void HeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (HeightTextBox != null)
            {
                height = (short)HeightSlider.Value;
                HeightTextBox.Text = height.ToString();
                UpdateStoreyLabel();
            }
        }

        private void HeightTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (short.TryParse(HeightTextBox.Text, out short value))
            {
                height = value;
                HeightSlider.Value = height;
                UpdateStoreyLabel();
            }
        }

        private void IncreaseHeight_Click(object sender, RoutedEventArgs e)
        {
            height = (short)Math.Min(height + 255, 32767);
            HeightSlider.Value = height;
            HeightTextBox.Text = height.ToString();
            UpdateStoreyLabel();
        }

        private void DecreaseHeight_Click(object sender, RoutedEventArgs e)
        {
            height = (short)Math.Max(height - 255, -32768);
            HeightSlider.Value = height;
            HeightTextBox.Text = height.ToString();
            UpdateStoreyLabel();
        }

        private void UpdateStoreyLabel()
        {
            int storeys = height / 255;
            StoreyLabel.Text = $"{storeys} Storeys";
        }
    }
}
