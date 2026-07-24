# LearnSpace (FocusStation)

Hệ thống Quản lý học tập và Giám sát máy tính (Parental Control) trên Windows 10.
Sử dụng kiến trúc Serverless với Firebase, phát triển bằng C# WPF (.NET 8) tuân thủ mô hình MVVM.

## Cấu trúc Dự án
Dự án bao gồm 3 thành phần chính:
1. **LearnSpace.Core**: Thư viện chứa các Models dùng chung, Firebase Services và các lời gọi Win32 API cấp thấp.
2. **LearnSpaceManager (ManagerApp)**: Ứng dụng dành cho Phụ huynh. Dùng để tạo mã PIN kết nối, xem màn hình giám sát từ xa, và gửi các lệnh điều khiển (Khóa máy, Gửi tin nhắn).
3. **LearnSpace (ClientApp)**: Ứng dụng dành cho máy Trẻ em. Chạy ngầm dưới System Tray, tự động khởi động cùng Windows. Nhiệm vụ chụp ảnh màn hình, ghi nhận ứng dụng đang mở, block các phím tắt thoát ứng dụng trong chế độ Kiosk.

## Công nghệ sử dụng
- **Ngôn ngữ**: C#
- **Framework**: .NET 8.0 WPF
- **UI/UX**: 100% Flat Design, Bảng màu Solid (Defined in `App.xaml`)
- **MVVM Framework**: CommunityToolkit.Mvvm
- **Database**: Firebase Realtime Database & Firebase Storage (`FirebaseDatabase.net`, `FirebaseStorage.net`)

## Hướng dẫn Build và Chạy
**Yêu cầu hệ thống:**
- Windows 10/11
- .NET 8.0 SDK

**Các bước Build:**
1. Clone repository hoặc tải thư mục source code về máy.
2. Mở Terminal (Command Prompt hoặc PowerShell) tại thư mục chứa file `LearnSpace.sln`.
3. Chạy lệnh khôi phục các gói thư viện (Nuget Packages):
   ```bash
   dotnet restore
   ```
4. Build toàn bộ Solution:
   ```bash
   dotnet build -c Release
   ```
5. Để chạy ứng dụng Manager (Phụ huynh):
   ```bash
   dotnet run --project LearnSpaceManager\LearnSpaceManager.csproj
   ```
6. Để chạy ứng dụng Client (Máy con):
   ```bash
   dotnet run --project LearnSpace\LearnSpace.csproj
   ```

*(Lưu ý: Ứng dụng yêu cầu quyền Administrator đối với ClientApp để có thể can thiệp vào Registry và hook phím cấp thấp Win32).*
