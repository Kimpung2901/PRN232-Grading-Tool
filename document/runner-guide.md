# Huong Dan Su Dung API Grading Runner

## 1. Muc dich

`Runner` la mot ung dung `Console App` tren `.NET 8` dung de cham tu dong bai nop REST API cua sinh vien bang `Newman`.

Runner hoat dong hoan toan cuc bo:

- Khong phu thuoc API ngoai
- Khong phu thuoc database ngoai
- Dung `System.Diagnostics.Process` de chay lenh
- Dung `System.IO` de xu ly file
- Dung `System.Text.Json` de doc ket qua JSON

## 2. Vi tri project Runner

Project nam tai:

```text
src/backend/Api_RestAPI_gradingTool/Runner
```

File chinh:

- `Program.cs`
- `Pipeline/GradingPipeline.cs`
- `Pipeline/UnzipService.cs`
- `Pipeline/BuildService.cs`
- `Pipeline/RunApiService.cs`
- `Pipeline/HealthCheckService.cs`
- `Pipeline/NewmanService.cs`
- `Pipeline/ReportParser.cs`
- `Pipeline/CleanupService.cs`
- `Models/TestOutcome.cs`

## 3. Cau truc thu muc can chuan bi

Khi chay runner, thu muc hien tai can co cac folder sau:

```text
Runner/
├ workspace/
├ submissions/
│  └ submission1.zip
├ collections/
│  └ exam.postman_collection.json
└ reports/
```

Y nghia:

- `workspace/`: noi giai nen bai nop tam thoi
- `submissions/`: chua file zip bai nop cua sinh vien
- `collections/`: chua Postman collection de cham
- `reports/`: chua log va report JSON sau khi cham

## 4. Quy trinh cham bai

Pipeline duoc thuc hien theo thu tu sau:

1. Tim file `.zip` dau tien trong `submissions/`
2. Giai nen vao `workspace/submission-{timestamp}`
3. Tim file `.csproj` dau tien trong bai nop
4. Chay `dotnet build`
5. Chay `dotnet run --no-build`
6. Gan API vao `http://localhost:5000`
7. Health check toi da 10 lan, moi lan cach nhau 2 giay
8. Chay Newman voi collection
9. Doc `report.json` va map thanh `List<TestOutcome>`
10. Kill API process
11. Xoa workspace tam

## 5. Cach chay

Di chuyen vao thu muc project runner:

```powershell
cd src\backend\Api_RestAPI_gradingTool\Runner
```

Build:

```powershell
dotnet build
```

Run:

```powershell
dotnet run
```

Neu moi truong local gap loi first-run cua `dotnet`, co the dung:

```powershell
$env:DOTNET_CLI_HOME="<duong-dan-local>"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT="1"
dotnet run
```

## 6. Yeu cau moi truong

Can cai san:

- `.NET SDK 8+`
- `Newman CLI`
- `npm` neu muon runner tu dong cai `Newman`

Cai Newman:

```powershell
npm install -g newman
```

Kiem tra:

```powershell
newman -v
dotnet --version
npm -v
```

### Tu dong kiem tra Newman

Khi pipeline bat dau, runner se tu dong kiem tra `newman` da co san hay chua.

- Neu da cai san: tiep tuc cham bai
- Neu chua cai: runner hoi tren console co muon cai tu dong hay khong
- Neu nguoi dung chon `y` hoac `yes`: runner chay `npm install -g newman`
- Neu nguoi dung chon `n`: pipeline dung voi ma loi `TEST_RUN_FAILED`
- Neu may chua co `npm`: pipeline dung voi ma loi `TEST_RUN_FAILED`
- Tren Windows, runner uu tien dung `npm.cmd` va `newman.cmd`

## 7. Dau vao

### Submission

- File bai nop phai la `.zip`
- Ben trong phai co it nhat 1 file `.csproj`

### Postman collection

- File collection phai co dinh dang `.postman_collection.json`
- Runner hien tai se lay file dau tien trong `collections/`

## 8. Dau ra

Runner in JSON ra console theo dang:

```json
{
  "error": null,
  "errorMessage": null,
  "results": [
    {
      "name": "Status code is 200",
      "passed": true
    }
  ],
  "buildLog": "...",
  "runLog": "..."
}
```

Trong do:

- `error`: ma loi neu pipeline that bai
- `errorMessage`: thong diep loi de debug nhanh hon
- `results`: danh sach test assertions
- `buildLog`: noi dung file build log
- `runLog`: noi dung file run log

## 9. Log va report

Sau khi chay, thu muc `reports/` se co:

- `build.log`
- `run.log`
- `newman.log`
- `report.json`

Y nghia:

- `build.log`: output cua `dotnet build`
- `run.log`: log khi API dang chay
- `newman.log`: output cua lenh Newman
- `report.json`: JSON report xuat tu Newman

## 10. Ma loi hien tai

Runner tra ve cac ma loi ro rang:

- `BUILD_FAILED`
- `API_START_FAILED`
- `TEST_RUN_FAILED`

### BUILD_FAILED

Xay ra khi:

- Khong tim thay `.csproj`
- `dotnet build` tra ve exit code khac 0

### API_START_FAILED

Xay ra khi:

- Khong start duoc process API
- API thoat som
- Health check that bai sau 10 lan thu

### TEST_RUN_FAILED

Xay ra khi:

- Khong tim thay submission zip
- Khong tim thay Postman collection
- Newman chay that bai
- Khong doc duoc report
- Loi khac trong pipeline

## 11. Luu y ky thuat

- Runner hien tai mac dinh chay API tai port `5000`
- Health check goi `http://localhost:5000`
- Runner se kill API process trong `finally`, ke ca khi co exception
- Workspace tam se duoc xoa sau moi lan chay
- Runner hien tai lay file `.zip` dau tien va collection dau tien trong thu muc

## 12. Goi y cai tien sau nay

Co the mo rong them:

- Cho phep truyen `submission path` bang command line args
- Cho phep cau hinh `port`
- Ho tro nhieu bai nop trong mot lan chay
- Chon dung endpoint health check, vi du `/health`
- Luu them diem so tong hop
- Xuat ket qua CSV hoac HTML

## 13. Kiem tra nhanh

Checklist truoc khi demo:

- Co file zip trong `submissions/`
- Co file collection trong `collections/`
- May da cai `newman`
- Bai nop build duoc bang `dotnet build`
- Bai nop co the chay tren localhost
