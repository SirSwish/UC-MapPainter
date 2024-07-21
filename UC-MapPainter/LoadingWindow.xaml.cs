using System.Windows;

namespace UC_MapPainter
{
    public partial class LoadingWindow : Window
    {
        public static readonly DependencyProperty TaskDescriptionProperty =
            DependencyProperty.Register("TaskDescription", typeof(string), typeof(LoadingWindow), new PropertyMetadata(string.Empty));

        public string TaskDescription
        {
            get { return (string)GetValue(TaskDescriptionProperty); }
            set { SetValue(TaskDescriptionProperty, value); }
        }

        public LoadingWindow()
        {
            InitializeComponent();
        }
    }
}
