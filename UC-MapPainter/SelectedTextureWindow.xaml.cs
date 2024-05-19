using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;

namespace UC_MapPainter
{
    public partial class SelectedTextureWindow : Window
    {
        public string SelectedTextureType { get; private set; }
        public int SelectedTextureNumber { get; private set; }
        public int SelectedTextureRotation { get; private set; }

        public SelectedTextureWindow()
        {
            InitializeComponent();
            SelectedTextureRotation = 0;
        }

        public void UpdateSelectedTexture(ImageSource newTexture, string type, int number)
        {
            SelectedTextureImage.Source = newTexture;
            SelectedTextureType = type;
            SelectedTextureNumber = number;
            SelectedTextureRotation = 0; // Reset rotation when a new texture is selected

            // Print the selected texture type, number, and rotation to the output window
            PrintSelectedTextureInfo();
            ApplyRotation(); // Ensure the rotation is correctly applied
        }

        private void RotateLeft_Click(object sender, RoutedEventArgs e)
        {
            SelectedTextureRotation = (SelectedTextureRotation - 90) % 360;
            if (SelectedTextureRotation < 0)
            {
                SelectedTextureRotation += 360;
            }
            ApplyRotation();
            PrintSelectedTextureInfo();
        }

        private void RotateRight_Click(object sender, RoutedEventArgs e)
        {
            SelectedTextureRotation = (SelectedTextureRotation + 90) % 360;
            ApplyRotation();
            PrintSelectedTextureInfo();
        }

        private void ApplyRotation()
        {
            var transform = new RotateTransform(SelectedTextureRotation)
            {
                CenterX = SelectedTextureImage.Width / 2,
                CenterY = SelectedTextureImage.Height / 2
            };
            SelectedTextureImage.RenderTransform = transform;
        }

        private void PrintSelectedTextureInfo()
        {
            Debug.WriteLine($"Selected Texture Type: {SelectedTextureType}");
            Debug.WriteLine($"Selected Texture Number: {SelectedTextureNumber}");
            Debug.WriteLine($"Selected Texture Rotation: {SelectedTextureRotation} degrees");
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
