using System;

namespace LearnSpace.Core.Models
{
    public class PairingRequest
    {
        // Mã PIN 6 số do ManagerApp sinh ra
        public string PinCode { get; set; } = string.Empty;

        // Trạng thái: "waiting", "paired"
        public string Status { get; set; } = "waiting";

        // UUID của thiết bị con sau khi ghép nối thành công
        public string DeviceToken { get; set; } = string.Empty;
        
        // Tên máy con (tùy chọn để quản lý dễ hơn)
        public string MachineName { get; set; } = Environment.MachineName;

        // Thời gian tạo request
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
