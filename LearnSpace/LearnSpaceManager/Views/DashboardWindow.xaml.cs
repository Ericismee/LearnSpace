using System.Windows;
using LearnSpaceManager.ViewModels;

namespace LearnSpaceManager.Views
{
    public partial class DashboardWindow : Window
    {
        public DashboardWindow(string deviceToken)
        {
            InitializeComponent();
            this.DataContext = new DashboardViewModel(deviceToken);
        }
    }
}
