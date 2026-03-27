# 🛠️ Module Chấm điểm (Grading Service) - Dev2

Tài liệu đặc tả nghiệp vụ và danh mục API bàn giao cho module xử lý chấm điểm tự động.

---

## 1. Quy trình nghiệp vụ (Business Workflow)

Khi giảng viên nhấn nút **"Grade/Chấm"** trên giao diện, hệ thống thực hiện chuỗi xử lý sau:

### 🔄 Thay đổi trạng thái (State Transition)

- Chuyển trạng thái của bản ghi bài nộp (Submission): `Uploaded` → `Queued` hoặc `Running`.
- Đảm bảo UI nhận được tín hiệu để hiển thị loading/spinner cho giảng viên.

### ⚙️ Xử lý chấm điểm

- **Tạo Job:** Khởi tạo job chấm điểm đẩy xuống Runner.
- **Xử lý đồng bộ (MVP):** Trong giai đoạn đầu, có thể xử lý phản hồi trực tiếp từ Runner trước khi trả về kết quả cho Client.

### 🧮 Logic tính điểm (Scoring Rules)

Dựa trên kết quả thô (Raw Result) từ Runner, hệ thống áp dụng quy tắc sau:

| Kết quả Testcase    | Trạng thái ghi nhận | Logic tính điểm                                                                    |
| :------------------ | :------------------ | :--------------------------------------------------------------------------------- |
| **Pass**            | `Passed`            | Cộng điểm vào tổng điểm của bài nộp.                                               |
| **Fail**            | `Failed`            | 0 điểm cho testcase này.                                                           |
| **Dependency Fail** | `Skipped`           | Nếu testcase cha (cha của nó) bị Fail, các testcase phụ thuộc sẽ bị bỏ qua (Skip). |

---

## 2. Lưu trữ dữ liệu (Data Persistence)

Kết quả sau khi tính toán phải được lưu vào Database để phục vụ tra cứu:

1. **TestResult:** Chi tiết kết quả của từng Testcase (bao gồm: trạng thái, điểm đạt được, log lỗi từ Runner).
2. **TotalScore:** Tổng điểm cuối cùng (Sum of passed testcases) được cập nhật vào bảng Submission.

---

## 3. Danh mục API bàn giao (Deliverables)

Dev2 chịu trách nhiệm cung cấp và đảm bảo các API sau hoạt động đúng thiết kế:

### 📍 Thực hiện chấm điểm

- **Endpoint:** `POST /submissions/{id}/grade`
- **Chức năng:** Kích hoạt luồng chấm điểm, xử lý logic dependency và tính tổng điểm.

### 📍 Lấy báo cáo kết quả

- **Endpoint:** `GET /submissions/{id}/report`
- **Chức năng:** Trả về dữ liệu chi tiết từng testcase và tổng điểm để hiển thị lên UI cho Giảng viên/Sinh viên.

### 📍 Kiểm tra trạng thái (Optional)

- **Endpoint:** `GET /submissions/{id}/status`
- **Chức năng:** Trả về trạng thái hiện tại của Submission (`Queued`, `Running`, `Completed`, `Error`).

---

## 4. Tiêu chuẩn nghiệm thu (Acceptance Criteria)

- [ ] **Scoring Dependency:** Chạy đúng logic nhảy bước (skip) khi testcase cha thất bại.
- [ ] **Data Integrity:** `TotalScore` phải khớp với tổng điểm của các `TestResult` thành công.
- [ ] **Performance:** API trả về kết quả hoặc phản hồi trạng thái trong thời gian cho phép (timeout).
- [ ] **Logging:** Lưu trữ đầy đủ log thô từ Runner để giảng viên có thể debug khi cần.
