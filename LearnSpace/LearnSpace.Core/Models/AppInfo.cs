namespace LearnSpace.Core.Models
{
    public class AppInfo
    {
        public string Id { get; set; } // MD5 of Path or Guid
        public string Name { get; set; }
        public string ExecutablePath { get; set; }
        public string IconBase64 { get; set; } // Ảnh 32x32 encode sang Base64
        public bool IsAllowed { get; set; } // Quyền sử dụng trong Kiosk
    }
}
