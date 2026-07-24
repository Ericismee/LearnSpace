using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LearnSpace.Core.Services;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace LearnSpace.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly FirebaseService _firebaseService;

        [ObservableProperty]
        private string _pinInput = "";

        [ObservableProperty]
        private string _statusMessage = "Nhập mã PIN gồm 6 chữ số từ máy của phụ huynh.";

        [ObservableProperty]
        private bool _isConnecting;

        public MainWindowViewModel()
        {
            _firebaseService = new FirebaseService();
        }

        [RelayCommand]
        public async Task ConnectAsync()
        {
            if (string.IsNullOrWhiteSpace(PinInput) || PinInput.Length != 6)
            {
                StatusMessage = "Vui lòng nhập đúng mã PIN 6 số.";
                return;
            }

            if (IsConnecting) return;

            try
            {
                IsConnecting = true;
                StatusMessage = "Đang kiểm tra mã PIN...";

                // Sinh ra DeviceToken
                string deviceToken = Guid.NewGuid().ToString("N");

                // Xác thực với Firebase
                bool isSuccess = await _firebaseService.ValidateAndPairAsync(PinInput, deviceToken);

                if (isSuccess)
                {
                    StatusMessage = "Kết nối thành công! Đang thiết lập hệ thống chạy ngầm...";
                    
                    // Lưu deviceToken vào Registry và thiết lập tự khởi động
                    var registryService = new LearnSpace.Core.Win32.RegistryService();
                    registryService.SaveDeviceToken(deviceToken);
                    registryService.SetAutoStart(true);
                    
                    await Task.Delay(2000); // Đợi 2s để user nhìn thấy thông báo
                    
                    // Khởi động Background Service ngay lập tức
                    var backgroundService = new LearnSpace.Services.BackgroundMonitorService(deviceToken);
                    backgroundService.Start();

                    // Chuyển sang chạy ngầm
                    Application.Current.MainWindow.Hide();
                }
                else
                {
                    StatusMessage = "Mã PIN không đúng hoặc đã hết hạn.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                IsConnecting = false;
            }
        }
    }
}
