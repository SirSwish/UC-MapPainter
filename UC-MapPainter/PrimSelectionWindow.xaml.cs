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

        public PrimSelectionWindow()
        {
            InitializeComponent();
            LoadPrims();
        }

        public void SetMainWindow(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void UpdateSelectedPrimImage(int primNumber)
        {
            // For now, we'll use a placeholder for the selected prim image
            // Update this to use actual prim images when available
            SelectedPrimImage.Source = new DrawingImage(new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 64, 64)),
                Brush = Brushes.LightGray,
                Pen = new Pen(Brushes.Black, 2)
            });

            if (primNumber != -1)
            {
                // Optionally, draw the number on the image
                DrawingVisual visual = new DrawingVisual();
                using (DrawingContext context = visual.RenderOpen())
                {
                    context.DrawRectangle(Brushes.LightGray, new Pen(Brushes.Black, 2), new Rect(0, 0, 64, 64));
                    FormattedText text = new FormattedText(
                        primNumber.ToString(),
                        System.Globalization.CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Verdana"),
                        32,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);
                    context.DrawText(text, new Point(10, 10));
                }
                RenderTargetBitmap bitmap = new RenderTargetBitmap(64, 64, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(visual);
                SelectedPrimImage.Source = bitmap;
            }
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
            // Handle rotation left logic
        }

        private void RotateRightButton_Click(object sender, RoutedEventArgs e)
        {
            // Handle rotation right logic
        }

        private void AdjustHeightButton_Click(object sender, RoutedEventArgs e)
        {
            // Handle adjust height logic
        }
    }
}
