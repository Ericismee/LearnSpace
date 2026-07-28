using System;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using LearnSpace.Core.Models;

namespace LearnSpace.Core.Services
{
    public class FirebaseService
    {
        private readonly string _firebaseUrl = "https://learnspace-ead6a-default-rtdb.asia-southeast1.firebasedatabase.app/";
        private readonly FirebaseClient _firebaseClient;

        public FirebaseService()
        {
            _firebaseClient = new FirebaseClient(_firebaseUrl);
        }

        /// <summary>
        /// Tạo một mã PIN mới và lưu lên Firebase
        /// </summary>
        /// <returns>Mã PIN 6 số</returns>
        public async Task<string> CreatePairingRequestAsync()
        {
            var random = new Random();
            var pinCode = random.Next(100000, 999999).ToString();

            var request = new PairingRequest
            {
                PinCode = pinCode,
                Status = "waiting"
            };

            // Lưu vào node "PairingRequests/{pinCode}"
            await _firebaseClient
                .Child("PairingRequests")
                .Child(pinCode)
                .PutAsync(request);

            return pinCode;
        }

        /// <summary>
        /// Theo dõi trạng thái của một mã PIN (dành cho ManagerApp)
        /// </summary>
        public IDisposable ListenToPairingRequest(string pinCode, Action<PairingRequest> onPaired)
        {
            return _firebaseClient
                .Child("PairingRequests")
                .AsObservable<dynamic>()
                .Subscribe(d =>
                {
                    if (d.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate && d.Key == pinCode)
                    {
                        var json = d.Object?.ToString();
                        if (!string.IsNullOrEmpty(json))
                        {
                            try
                            {
                                var request = Newtonsoft.Json.JsonConvert.DeserializeObject<PairingRequest>(json);
                                if (request != null && request.Status == "paired")
                                {
                                    onPaired?.Invoke(request);
                                }
                            }
                            catch { }
                        }
                    }
                });
        }

        /// <summary>
        /// Xác thực mã PIN và cấp quyền (dành cho ClientApp)
        /// </summary>
        public async Task<bool> ValidateAndPairAsync(string pinCode, string deviceToken)
        {
            var requestNode = _firebaseClient.Child("PairingRequests").Child(pinCode);
            var currentRequest = await requestNode.OnceSingleAsync<PairingRequest>();

            if (currentRequest == null) throw new Exception("Không tìm thấy mã PIN trên hệ thống.");
            if (currentRequest.Status != "waiting") throw new Exception("Mã PIN này đã được sử dụng hoặc hết hạn.");
            // Cập nhật trạng thái thành paired và lưu device token
            currentRequest.Status = "paired";
            currentRequest.DeviceToken = deviceToken;

            await requestNode.PutAsync(currentRequest);
            
            // Đồng thời, lưu DeviceToken vào node của Manager (để Manager biết máy nào thuộc về mình)
            // Giả lập lưu vào node "Devices/{deviceToken}"
            await _firebaseClient
                .Child("Devices")
                .Child(deviceToken)
                .PutAsync(new { Status = "Active", PairedAt = DateTime.UtcNow });

            return true;
        }

        /// <summary>
        /// Gửi lệnh điều khiển từ xa đến một thiết bị cụ thể (dành cho ManagerApp)
        /// </summary>
        public async Task SendCommandAsync(string deviceToken, RemoteCommand command)
        {
            await _firebaseClient
                .Child("Commands")
                .Child(deviceToken)
                .PostAsync(command);
        }

        /// <summary>
        /// Lắng nghe các lệnh điều khiển từ xa (dành cho ClientApp)
        /// </summary>
        public IDisposable ListenForCommands(string deviceToken, Action<RemoteCommand> onCommandReceived)
        {
            return _firebaseClient
                .Child("Commands")
                .Child(deviceToken)
                .AsObservable<RemoteCommand>()
                .Subscribe(d =>
                {
                    if (d.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate && d.Object != null)
                    {
                        onCommandReceived?.Invoke(d.Object);
                    }
                });
        }

        /// <summary>
        /// Lưu URL ảnh chụp màn hình lên Firebase (ClientApp)
        /// </summary>
        public async Task SaveLiveScreenAsync(string deviceToken, string url)
        {
            await _firebaseClient
                .Child("LiveScreen")
                .Child(deviceToken)
                .PutAsync(new { Url = url, Timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Lắng nghe ảnh Live Screen (ManagerApp)
        /// </summary>
        public IDisposable ListenToLiveScreen(string deviceToken, Action<string> onUrlReceived)
        {
            return _firebaseClient
                .Child("LiveScreen")
                .Child(deviceToken)
                .AsObservable<dynamic>()
                .Subscribe(d =>
                {
                    if (d.Object != null && d.Object.Url != null)
                    {
                        onUrlReceived?.Invoke(d.Object.Url.ToString());
                    }
                });
        }

        /// <summary>
        /// Đồng bộ danh sách ứng dụng đã cài đặt lên Firebase (ClientApp)
        /// Cập nhật nhưng KHÔNG ghi đè trường IsAllowed nếu đã tồn tại.
        /// </summary>
        public async Task SyncInstalledAppsAsync(string deviceToken, System.Collections.Generic.List<AppInfo> apps)
        {
            var appsNode = _firebaseClient.Child("Apps").Child(deviceToken);
            var currentApps = await appsNode.OnceAsync<AppInfo>();

            foreach (var app in apps)
            {
                var existingApp = currentApps.FirstOrDefault(a => a.Key == app.Id);
                if (existingApp != null)
                {
                    app.IsAllowed = existingApp.Object.IsAllowed; // Giữ nguyên trạng thái cũ
                }
                await appsNode.Child(app.Id).PutAsync(app);
            }
        }

        /// <summary>
        /// Cập nhật quyền truy cập 1 ứng dụng (ManagerApp)
        /// </summary>
        public async Task UpdateAppAllowedStatusAsync(string deviceToken, string appId, bool isAllowed)
        {
            // Chỉ update trường IsAllowed thay vì ghi đè cả object
            // Firebase.Database bản Lite có thể không hỗ trợ Patch trực tiếp, nên ta lấy ra và lưu lại.
            var node = _firebaseClient.Child("Apps").Child(deviceToken).Child(appId);
            var app = await node.OnceSingleAsync<AppInfo>();
            if (app != null)
            {
                app.IsAllowed = isAllowed;
                await node.PutAsync(app);
            }
        }

        /// <summary>
        /// Lắng nghe danh sách ứng dụng (Cả Manager và Client đều dùng)
        /// </summary>
        public IDisposable ListenToApps(string deviceToken, Action<System.Collections.Generic.List<AppInfo>> onAppsUpdated)
        {
            return _firebaseClient
                .Child("Apps")
                .Child(deviceToken)
                .AsObservable<AppInfo>()
                .Subscribe(async _ => 
                {
                    // Khi có thay đổi, kéo toàn bộ về để vẽ lại UI
                    var apps = await _firebaseClient.Child("Apps").Child(deviceToken).OnceAsync<AppInfo>();
                    var list = apps.Select(a => a.Object).ToList();
                    onAppsUpdated?.Invoke(list);
                });
        }

        /// <summary>
        /// Đẩy đoạn keylog lên Firebase (ClientApp)
        /// </summary>
        public async Task SaveKeylogAsync(string deviceToken, string keys)
        {
            var node = _firebaseClient.Child("Devices").Child(deviceToken).Child("Keylogs");
            await node.PostAsync(new { Keys = keys, Timestamp = DateTime.UtcNow });
        }

        public IDisposable ListenToKeylogs(string deviceToken, Action<string> onKeylogReceived)
        {
            return _firebaseClient
                .Child("Devices")
                .Child(deviceToken)
                .Child("Keylogs")
                .AsObservable<dynamic>()
                .Subscribe(d =>
                {
                    if (d.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate)
                    {
                        if (d.Object != null && d.Object.Keys != null)
                        {
                            onKeylogReceived?.Invoke(d.Object.Keys.ToString());
                        }
                    }
                });
        }

        /// <summary>
        /// Cập nhật mã PIN động cho thiết bị
        /// </summary>
        public async Task SetSecurityCodesAsync(string deviceToken, DeviceSecurityCodes codes)
        {
            await _firebaseClient
                .Child("Devices")
                .Child(deviceToken)
                .Child("SecurityCodes")
                .PutAsync(codes);
        }

        /// <summary>
        /// Lắng nghe mã PIN động cập nhật
        /// </summary>
        public IDisposable ListenToSecurityCodes(string deviceToken, Action<DeviceSecurityCodes> onCodesUpdated)
        {
            return _firebaseClient
                .Child("Devices")
                .Child(deviceToken)
                .AsObservable<dynamic>()
                .Subscribe(d =>
                {
                    if (d.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate && d.Key == "SecurityCodes")
                    {
                        var json = d.Object?.ToString();
                        if (!string.IsNullOrEmpty(json))
                        {
                            try 
                            {
                                var codes = Newtonsoft.Json.JsonConvert.DeserializeObject<DeviceSecurityCodes>(json);
                                if (codes != null) onCodesUpdated?.Invoke(codes);
                            }
                            catch { }
                        }
                    }
                });
        }
    }
}
