using System.Windows;
using LearnSpace.Core.Win32;

namespace LearnSpace
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Kiểm tra xem máy đã được ghép nối chưa
            var registryService = new RegistryService();
            string token = registryService.GetDeviceToken();
            
            if (!string.IsNullOrEmpty(token))
            {
                // Nếu đã ghép nối, khởi động Background Service và ẩn giao diện
                var backgroundService = new LearnSpace.Services.BackgroundMonitorService(token);
                backgroundService.Start();
                this.Hide();
            }
        }

        private void MenuOpenUI_Click(object sender, RoutedEventArgs e)
        {
            // Hiển thị lại giao diện cho Developer
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            // Mở hộp thoại yêu cầu mã thoát
            var exitDialog = new Views.ExitDialog();
            exitDialog.ShowDialog();
        }
    }
}