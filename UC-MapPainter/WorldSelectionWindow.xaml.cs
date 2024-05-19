using System.Windows;
using System.Windows.Controls;

namespace UC_MapPainter
{
    public partial class WorldSelectionWindow : Window
    {
        public string SelectedWorld { get; private set; }

        public WorldSelectionWindow()
        {
            InitializeComponent();
            WorldComboBox.SelectedIndex = 0;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (WorldComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                SelectedWorld = selectedItem.Content.ToString();
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Please select a world.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
