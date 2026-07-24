using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using LearnSpace.Core.Models;

namespace LearnSpace.Views
{
    public partial class KioskWindow : Window
    {
        public static List<AppInfo> AllowedApps { get; set; } = new List<AppInfo>();

        // Biến kiểm soát việc cố tình đóng cửa sổ bằng Alt+F4
        public bool IsForceLocked { get; set; } = true;

        public KioskWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Hiển thị các ứng dụng được phép
            AppItemsControl.ItemsSource = AllowedApps;
        }

        private void BtnApp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is AppInfo app)
            {
                try
                {
                    // Chạy phần mềm theo đường dẫn
                    Process.Start(new ProcessStartInfo(app.ExecutablePath) { UseShellExecute = true });
                }
                catch
                {
                    MessageBox.Show($"Không thể mở ứng dụng: {app.Name}");
                }
            }
        }

        private void BtnUnlock_Click(object sender, RoutedEventArgs e)
        {
            var unlockDialog = new UnlockDialog
            {
                Owner = this
            };
            
            unlockDialog.ShowDialog();

            if (unlockDialog.IsUnlocked)
            {
                // Tắt chế độ Force Lock và đóng cửa sổ Kiosk
                IsForceLocked = false;
                this.Close();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Nếu đang trong chế độ khóa, cấm đóng bằng nút X hoặc Alt+F4
            if (IsForceLocked)
            {
                e.Cancel = true;
            }
        }
    }
}
