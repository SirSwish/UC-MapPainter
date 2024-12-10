using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace UC_MapPainter
{
    public partial class WallSelectionWindow : Window
    {
        private MainWindow _mainWindow;
        private int selectedPrimNumber = -1;
        private byte yaw = 0;
        private short height = 0;

        public WallSelectionWindow()
        {
            InitializeComponent();
            this.Loaded += WallSelectionWindow_Loaded;
        }

        private void WallSelectionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //LoadPrimButtons();
        }

        public void SetMainWindow(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
