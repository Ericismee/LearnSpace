using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace LearnSpace.Core.Win32
{
    public class WindowMonitorService
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        /// <summary>
        /// Lấy thông tin ứng dụng đang hiện trên cùng (Foreground)
        /// </summary>
        public ForegroundAppInfo GetActiveWindowInfo()
        {
            IntPtr hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero) return null;

            // Lấy tiêu đề cửa sổ (Title)
            int length = GetWindowTextLength(hWnd);
            StringBuilder sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            string windowTitle = sb.ToString();

            // Lấy tên tiến trình (Process Name)
            GetWindowThreadProcessId(hWnd, out uint processId);
            string processName = "Unknown";
            try
            {
                Process p = Process.GetProcessById((int)processId);
                processName = p.ProcessName;
            }
            catch (Exception) { /* Bỏ qua lỗi nếu process đã tắt */ }

            return new ForegroundAppInfo
            {
                Title = windowTitle,
                ProcessName = processName,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public class ForegroundAppInfo
    {
        public string Title { get; set; }
        public string ProcessName { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
