using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LearnSpaceManager;

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
        var registryService = new LearnSpace.Core.Win32.RegistryService();
        string token = registryService.GetDeviceToken();
        
        if (!string.IsNullOrEmpty(token))
        {
            // Mở thẳng Dashboard nếu đã ghép nối trước đó
            var dashboard = new LearnSpaceManager.Views.DashboardWindow(token);
            dashboard.Show();
            this.Close();
        }
    }
}