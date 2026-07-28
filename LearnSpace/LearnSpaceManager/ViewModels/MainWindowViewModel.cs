using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LearnSpace.Core.Models;
using LearnSpace.Core.Services;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LearnSpaceManager.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly FirebaseService _firebaseService;
        private IDisposable _pairingSubscription;

        [ObservableProperty]
        private string _pinCode = "------";

        [ObservableProperty]
        private string _statusMessage = "Bấm 'Tạo Mã PIN' để bắt đầu ghép nối với máy của trẻ.";

        [ObservableProperty]
        private bool _isGenerating;

        public MainWindowViewModel()
        {
            _firebaseService = new FirebaseService();
        }

        [RelayCommand]
        public async Task GeneratePinAsync()
        {
            if (IsGenerating) return;

            try
            {
                IsGenerating = true;
                StatusMessage = "Đang tạo mã PIN...";
                
                // Gọi service tạo PIN
                PinCode = await _firebaseService.CreatePairingRequestAsync();
                StatusMessage = "Đang chờ thiết bị con nhập mã PIN...";

                // Lắng nghe sự thay đổi trạng thái
                _pairingSubscription?.Dispose();
                _pairingSubscription = _firebaseService.ListenToPairingRequest(PinCode, request =>
                {
                    // Đã được ghép nối
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = "Ghép nối thành công! Mở bảng điều khiển...";
                        
                        // Lưu token lại để lần sau mở app không cần ghép nối lại
                        var registryService = new LearnSpace.Core.Win32.RegistryService();
                        registryService.SaveDeviceToken(request.DeviceToken);

                        // Mở Dashboard Window
                        var dashboard = new LearnSpaceManager.Views.DashboardWindow(request.DeviceToken);
                        dashboard.Show();
                        
                        // Đóng cửa sổ hiện tại
                        Application.Current.MainWindow.Close();
                    });
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                IsGenerating = false;
            }
        }
    }
}
