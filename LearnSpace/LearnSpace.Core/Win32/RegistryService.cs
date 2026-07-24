using Microsoft.Win32;
using System;
using System.Reflection;

namespace LearnSpace.Core.Win32
{
    public class RegistryService
    {
        private const string AppName = "LearnSpaceClient";
        private const string RegistryKeyPath = @"SOFTWARE\LearnSpace";

        /// <summary>
        /// Ghi đường dẫn .exe vào Registry để tự khởi động cùng Windows
        /// </summary>
        public void SetAutoStart(bool enable)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (key != null)
                {
                    if (enable)
                    {
                        // Lấy đường dẫn exe hiện tại
                        string exePath = Assembly.GetEntryAssembly()?.Location;
                        // Phải lấy đường dẫn .exe thật, nếu là .dll do .NET Core, ta đổi sang .exe
                        if (exePath != null && exePath.EndsWith(".dll"))
                        {
                            exePath = exePath.Substring(0, exePath.Length - 4) + ".exe";
                        }
                        
                        if (!string.IsNullOrEmpty(exePath))
                        {
                            key.SetValue(AppName, $"\"{exePath}\"");
                        }
                    }
                    else
                    {
                        key.DeleteValue(AppName, false);
                    }
                }
            }
        }

        /// <summary>
        /// Lưu DeviceToken đã được mã hóa cơ bản vào Registry
        /// </summary>
        public void SaveDeviceToken(string token)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
            {
                if (key != null)
                {
                    // Lưu dưới dạng chuỗi
                    key.SetValue("DeviceToken", token);
                }
            }
        }

        /// <summary>
        /// Đọc DeviceToken từ Registry
        /// </summary>
        public string GetDeviceToken()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
            {
                if (key != null)
                {
                    return key.GetValue("DeviceToken") as string;
                }
            }
            return null;
        }

        public void SaveSecurityCodes(string exitCode, string unlockCode)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
            {
                if (key != null)
                {
                    if (!string.IsNullOrEmpty(exitCode)) key.SetValue("ExitCode", exitCode);
                    if (!string.IsNullOrEmpty(unlockCode)) key.SetValue("UnlockCode", unlockCode);
                }
            }
        }

        public string GetExitCode()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
            {
                if (key != null)
                {
                    return key.GetValue("ExitCode") as string;
                }
            }
            return null;
        }

        public string GetUnlockCode()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
            {
                if (key != null)
                {
                    return key.GetValue("UnlockCode") as string;
                }
            }
            return null;
        }
    }
}
