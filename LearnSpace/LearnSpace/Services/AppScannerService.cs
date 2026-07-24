using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using LearnSpace.Core.Models;

namespace LearnSpace.Services
{
    public class AppScannerService
    {
        public List<AppInfo> ScanApps()
        {
            var apps = new List<AppInfo>();
            
            // Các thư mục phổ biến để quét (Desktop chung, Desktop cá nhân)
            string publicDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
            string userDesktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            
            // HashSet để tránh trùng lặp app
            var seenNames = new HashSet<string>();

            ScanDirectory(publicDesktop, apps, seenNames);
            ScanDirectory(userDesktop, apps, seenNames);

            return apps;
        }

        private void ScanDirectory(string dirPath, List<AppInfo> apps, HashSet<string> seenNames)
        {
            if (!Directory.Exists(dirPath)) return;

            // Quét các file .lnk (Shortcut)
            var files = Directory.GetFiles(dirPath, "*.lnk");
            foreach (var file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                
                // Tránh các file linh tinh
                if (name.ToLower().Contains("uninstall") || seenNames.Contains(name))
                    continue;

                seenNames.Add(name);

                // Lấy icon
                string base64Icon = GetIconBase64(file);

                // Nếu không lấy được icon thì bỏ qua (thường là shortcut rác)
                if (string.IsNullOrEmpty(base64Icon))
                    continue;

                apps.Add(new AppInfo
                {
                    Id = GetMd5Hash(file), // Tạo ID duy nhất từ đường dẫn
                    Name = name,
                    ExecutablePath = file, // Lưu đường dẫn shortcut để gọi Process.Start
                    IconBase64 = base64Icon,
                    IsAllowed = false // Mặc định không cho phép, chờ Manager duyệt
                });
            }
        }

        private string GetIconBase64(string filePath)
        {
            try
            {
                // Thử trích xuất icon liên kết (cần System.Drawing.Common)
                using (Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(filePath))
                {
                    if (icon != null)
                    {
                        using (Bitmap bmp = icon.ToBitmap())
                        {
                            // Resize về 48x48 cho đẹp
                            using (Bitmap resizedBmp = new Bitmap(bmp, new Size(48, 48)))
                            {
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    resizedBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                    byte[] byteImage = ms.ToArray();
                                    return Convert.ToBase64String(byteImage);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Bỏ qua nếu không lấy được
            }
            return null;
        }

        private string GetMd5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}
