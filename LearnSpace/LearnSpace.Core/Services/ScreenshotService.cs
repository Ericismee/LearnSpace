using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LearnSpace.Core.Services
{
    public class ScreenshotService
    {
        private readonly Cloudinary _cloudinary;

        // Mock config Cloudinary, sẽ cần thay thế bằng config thật
        private const string CloudName = "demo";
        private const string ApiKey = "123456789012345";
        private const string ApiSecret = "abcdefghijklmnopqrstuvwxyz123456";

        public ScreenshotService()
        {
            var account = new Account(CloudName, ApiKey, ApiSecret);
            _cloudinary = new Cloudinary(account);
        }

        /// <summary>
        /// Chụp ảnh toàn màn hình và trả về mảng byte dạng JPEG
        /// </summary>
        /// <param name="quality">Chất lượng JPEG (0-100), mặc định 75</param>
        public byte[] CaptureScreen(int quality = 75)
        {
            try
            {
                Rectangle bounds = Screen.PrimaryScreen.Bounds;
                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        // Hàm Win32 GDI wrapper để chụp ảnh
                        g.CopyFromScreen(System.Drawing.Point.Empty, System.Drawing.Point.Empty, bounds.Size);
                    }

                    // Cấu hình chất lượng nén JPEG
                    ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                    System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
                    EncoderParameters myEncoderParameters = new EncoderParameters(1);
                    EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, quality);
                    myEncoderParameters.Param[0] = myEncoderParameter;

                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmap.Save(ms, jpgEncoder, myEncoderParameters);
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi chụp màn hình: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Upload ảnh dạng mảng byte lên Cloudinary
        /// </summary>
        public async Task<string> UploadToCloudinaryAsync(byte[] imageBytes, string fileName)
        {
            if (imageBytes == null || imageBytes.Length == 0) return null;

            try
            {
                using (var stream = new MemoryStream(imageBytes))
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(fileName, stream),
                        Folder = "LearnSpace_Screenshots"
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    return uploadResult?.SecureUrl?.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi upload ảnh lên Cloudinary: {ex.Message}");
                return null;
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
