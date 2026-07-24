using System.Windows;
using LearnSpace.Core.Win32;

namespace LearnSpace.Views
{
    public partial class ExitDialog : Window
    {
        public ExitDialog()
        {
            InitializeComponent();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            var registryService = new RegistryService();
            string token = registryService.GetDeviceToken();
            
            if (!string.IsNullOrEmpty(token))
            {
                // Lấy mã thoát động từ Registry
                string expectedCode = registryService.GetExitCode();
                
                // Fallback nếu vì lý do nào đó chưa có mã động
                if (string.IsNullOrEmpty(expectedCode))
                {
                    string numericHash = LearnSpace.Core.Utils.CoreUtils.GetNumericHash(token);
                    expectedCode = numericHash.Substring(0, 6);
                }

                if (TxtExitCode.Text.Trim().ToUpper() == expectedCode)
                {
                    Application.Current.Shutdown();
                }
                else
                {
                    MessageBox.Show("Mã thoát không chính xác!");
                }
            }
            else
            {
                // Nếu chưa ghép nối thì thoát luôn
                Application.Current.Shutdown();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
