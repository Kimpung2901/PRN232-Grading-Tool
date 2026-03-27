# Context

Tôi đang phát triển một hệ thống chấm bài/kiểm tra tự động bằng .NET 8. Hệ thống cần một API để tiếp nhận thông tin kết quả, kích hoạt một Project Console (Runner) bên ngoài và thông báo trạng thái cho Frontend theo thời gian thực.

# Yêu cầu kỹ thuật

## 1. Cấu trúc dữ liệu (DTO)

Tạo class `TestResultRequest` với các thuộc tính:

- `studentName`: string (tên file zip/tên học sinh)
- `score`: int (điểm số)
- `status`: string (ví dụ: "build false", "ok")
- `reportFilePath`: string (đường dẫn file báo cáo)

## 2. SignalR Hub

- Tạo một Hub có tên `TestHub`.
- Thiết lập phương thức để gửi thông điệp tới tất cả các Client (Broadcast) mỗi khi có kết quả mới.

## 3. API Endpoint (Controller hoặc Minimal API)

Tạo POST API `/api/testresults`:

- **Bước 1 (Xử lý Process):** Khi nhận được request, khởi tạo một `System.Diagnostics.Process` để chạy một Project Console .NET (Runner) nằm ở thư mục chỉ định trên server.
  - Cấu hình `RedirectStandardOutput = true`.
  - Truyền `studentName` làm tham số đầu vào cho Runner.
- **Bước 2 (SignalR):** Sau khi khởi chạy (hoặc đợi Runner kết thúc), sử dụng `IHubContext<TestHub>` để bắn toàn bộ object dữ liệu vừa nhận được lên Frontend.

## 4. Cấu hình hệ thống (Program.cs)

- Đăng ký dịch vụ SignalR.
- Cấu hình CORS (AllowAnyHeader, AllowAnyMethod, AllowCredentials) để Frontend có thể kết nối.
- Map Hub vào route `/testHub`.

# Kết quả mong đợi

- Viết code C# chi tiết cho: `TestResultRequest.cs`, `TestHub.cs`, `TestResultsController.cs` và file `Program.cs`.
- Giải thích ngắn gọn cách cấu hình đường dẫn (Path) đến file .exe của Runner để đảm bảo API tìm thấy project console.
- Sử dụng async/await để không làm block thread khi chạy process.
