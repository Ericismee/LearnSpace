using System;

namespace LearnSpace.Core.Models
{
    public class RemoteCommand
    {
        // "lock", "unlock", "click", "keystroke"
        public string Action { get; set; }
        
        // Payload dạng JSON để mở rộng (Ví dụ: x, y cho chuột hoặc danh sách Allowed Apps)
        public string Payload { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
