using System.Windows;
using LearnSpace.Core.Win32;

namespace LearnSpace.Views
{
    public partial class UnlockDialog : Window
    {
        public bool IsUnlocked { get; private set; } = false;

        public UnlockDialog()
        {
            InitializeComponent();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            var registryService = new RegistryService();
            string token = registryService.GetDeviceToken();
            
            if (!string.IsNullOrEmpty(token))
            {
                // Lấy mã mở khóa động từ Registry
                string expectedCode = registryService.GetUnlockCode();
                
                // Fallback nếu vì lý do nào đó chưa có mã động
                if (string.IsNullOrEmpty(expectedCode))
                {
                    string numericHash = LearnSpace.Core.Utils.CoreUtils.GetNumericHash(token);
                    expectedCode = numericHash.Substring(6, 4);
                }
                if (TxtUnlockCode.Text.Trim().ToUpper() == expectedCode)
                {
                    IsUnlocked = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Mã mở khóa không chính xác!");
                }
            }
            else
            {
                // Fallback nếu token lỗi
                this.Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
