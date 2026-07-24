using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LearnSpace.Core.Models;
using LearnSpace.Core.Services;
using System.Threading.Tasks;
using System.Windows;

namespace LearnSpaceManager.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly FirebaseService _firebaseService;
        private string _targetDeviceToken;

        [ObservableProperty]
        private int _currentTabIndex = 0;

        [RelayCommand]
        public void SwitchTab(string indexStr)
        {
            if (int.TryParse(indexStr, out int index))
            {
                CurrentTabIndex = index;
            }
        }

        [ObservableProperty]
        private string _statusMessage = "Chọn lệnh điều khiển để gửi đến máy con.";

        [ObservableProperty]
        private bool _isSendingCommand;

        [ObservableProperty]
        private string _exitCode = "------";

        [ObservableProperty]
        private string _unlockCode = "----";

        [ObservableProperty]
        private string _liveScreenUrl;

        [ObservableProperty]
        private string _recentKeylogs = "";

        [ObservableProperty]
        private System.Collections.ObjectModel.ObservableCollection<AppInfo> _installedApps = new();

        private string _rawKeylogs = "";

        private string ParseKeylogs(string raw)
        {
            var sb = new System.Text.StringBuilder();
            int i = 0;
            while (i < raw.Length)
            {
                if (raw[i] == '[' && raw.IndexOf(']', i) > i)
                {
                    int end = raw.IndexOf(']', i);
                    string tag = raw.Substring(i, end - i + 1).ToUpperInvariant();
                    
                    if (tag == "[BACKSPACE]" || tag == "[BACK]")
                    {
                        if (sb.Length > 0) sb.Length--;
                    }
                    else if (tag == "[ENTER]" || tag == "[RETURN]")
                    {
                        sb.AppendLine();
                    }
                    else if (tag == "[TAB]")
                    {
                        sb.Append("    ");
                    }
                    else if (tag == "[SPACE]")
                    {
                        sb.Append(" ");
                    }
                    // Bỏ qua các tag khác (như [SHIFT], [CTRL]...)
                    i = end + 1;
                }
                else
                {
                    sb.Append(raw[i]);
                    i++;
                }
            }
            return sb.ToString();
        }

        [RelayCommand]
        public async Task RefreshSecurityCodesAsync()
        {
            var random = new Random();
            var newExitCode = random.Next(100000, 999999).ToString();
            var newUnlockCode = random.Next(1000, 9999).ToString();
            
            var codes = new DeviceSecurityCodes 
            { 
                ExitCode = newExitCode, 
                UnlockCode = newUnlockCode, 
                LastUpdated = DateTime.UtcNow 
            };
            await _firebaseService.SetSecurityCodesAsync(_targetDeviceToken, codes);
        }

        public DashboardViewModel(string deviceToken)
        {
            _targetDeviceToken = deviceToken;
            _firebaseService = new FirebaseService();
            
            // Lắng nghe Security Codes từ Firebase
            _firebaseService.ListenToSecurityCodes(_targetDeviceToken, codes =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ExitCode = codes.ExitCode;
                    UnlockCode = codes.UnlockCode;
                });
            });

            // Gửi lệnh tạo mã ngẫu nhiên lần đầu (nếu chưa có thì Firebase Listener sẽ update)
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000);
                if (ExitCode == "------" || string.IsNullOrEmpty(ExitCode))
                {
                    await RefreshSecurityCodesAsync();
                }
            });

            // Lắng nghe Live Screen
            _firebaseService.ListenToLiveScreen(_targetDeviceToken, url =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    LiveScreenUrl = url;
                });
            });

            // Lắng nghe Keylogs
            _firebaseService.ListenToKeylogs(_targetDeviceToken, keys =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _rawKeylogs += keys;
                    if (_rawKeylogs.Length > 5000)
                    {
                        _rawKeylogs = _rawKeylogs.Substring(_rawKeylogs.Length - 5000);
                    }
                    RecentKeylogs = ParseKeylogs(_rawKeylogs);
                });
            });

            // Lắng nghe danh sách App
            _firebaseService.ListenToApps(_targetDeviceToken, apps =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    InstalledApps.Clear();
                    foreach (var app in apps)
                    {
                        InstalledApps.Add(app);
                    }
                });
            });
        }

        [RelayCommand]
        public async Task ToggleAppAllowedAsync(AppInfo app)
        {
            if (app != null)
            {
                // Cập nhật trạng thái mới lên Firebase (app.IsAllowed đã được UI toggle thông qua Binding TwoWay, hoặc ta có thể đảo ngược nếu cần)
                // Do CheckBox WPF tự động thay đổi thuộc tính bool nên ta chỉ cần truyền isAllowed hiện tại
                await _firebaseService.UpdateAppAllowedStatusAsync(_targetDeviceToken, app.Id, app.IsAllowed);
            }
        }

        [RelayCommand]
        public async Task LockScreenAsync()
        {
            await SendCommandInternal("lock_screen", "Chế độ Khóa toàn màn hình, chỉ hiển thị thông báo.");
        }

        [RelayCommand]
        public async Task LockStudyModeAsync()
        {
            // Payload chứa danh sách app được phép (Word, Chrome)
            await SendCommandInternal("lock_study", "word,chrome");
        }

        [RelayCommand]
        public async Task LockSpecificAppAsync(string appName)
        {
            await SendCommandInternal("lock_app", appName);
        }

        [RelayCommand]
        public async Task UnlockAsync()
        {
            await SendCommandInternal("unlock", "");
        }

        private async Task SendCommandInternal(string action, string payload)
        {
            if (IsSendingCommand || string.IsNullOrEmpty(_targetDeviceToken)) return;

            try
            {
                IsSendingCommand = true;
                StatusMessage = "Đang gửi lệnh...";

                var command = new RemoteCommand
                {
                    Action = action,
                    Payload = payload
                };

                await _firebaseService.SendCommandAsync(_targetDeviceToken, command);
                StatusMessage = "Gửi lệnh thành công!";
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                IsSendingCommand = false;
            }
        }
    }
}
