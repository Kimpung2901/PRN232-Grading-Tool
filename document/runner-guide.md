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
- `Pipeline/EnvironmentSetupService.cs`
- `Pipeline/RunnerPathResolver.cs`
- `Models/TestOutcome.cs`

## 3. Cau truc thu muc

Runner su dung cau truc thu muc sau:

```text
Runner/
|- collections/
|  `- exam.postman_collection.json
|- submissions/
|  `- submission1.zip
|- workspace/
|- reports/
`- .tools/
```

Y nghia:

- `submissions/`: chua file zip bai nop cua sinh vien
- `collections/`: chua Postman collection de cham
- `workspace/`: noi giai nen bai nop tam thoi
- `reports/`: chua log va report JSON sau khi cham
- `.tools/`: noi runner cai `Newman` local va luu npm cache local

Trong project da co san cac thu muc sau:

- `submissions/`
- `collections/`
- `workspace/`
- `reports/`

## 4. Quy trinh cham bai

Pipeline duoc thuc hien theo thu tu sau:

1. Kiem tra `Newman` co san hay chua
2. Tim file `.zip` dau tien trong `submissions/`
3. Giai nen vao `workspace/submission-{timestamp}`
4. Tim file `.csproj` dau tien trong bai nop
5. Chay `dotnet build`
6. Chay `dotnet run --no-build`
7. Gan API vao `http://localhost:5000`
8. Health check toi da 10 lan, moi lan cach nhau 2 giay
9. Chay Newman voi collection
10. Doc `report.json` va map thanh `List<TestOutcome>`
11. Kill API process
12. Xoa workspace tam

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
- `npm` neu muon runner tu dong cai `Newman`

Co the cai Newman thu cong:

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

- Neu da co `newman` local hoac global: tiep tuc cham bai
- Neu chua co: runner hoi tren console co muon cai tu dong hay khong
- Neu nguoi dung chon `y` hoac `yes`: runner cai `newman` local vao `Runner/.tools/newman`
- Neu nguoi dung chon `n`: pipeline dung voi ma loi `TEST_RUN_FAILED`
- Neu may chua co `npm`: pipeline dung voi ma loi `TEST_RUN_FAILED`
- Tren Windows, runner uu tien dung `npm.cmd` va `newman.cmd`

## 7. Dau vao

### Submission

- File bai nop phai la `.zip`
- Ben trong phai co it nhat 1 file `.csproj`
- Runner hien tai lay file `.zip` dau tien trong `submissions/`

### Postman collection

- File collection phai co dinh dang `.postman_collection.json`
- Runner hien tai lay file dau tien trong `collections/`

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

Runner co the tao them:

- `.tools/newman/`
- `.tools/npm-cache/`
- `.tools/npm-home/`

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
- Newman khong ton tai va nguoi dung khong dong y cai
- Loi khac trong pipeline

## 11. Luu y ky thuat

- Runner hien tai mac dinh chay API tai port `5000`
- Health check goi `http://localhost:5000`
- Runner se kill API process trong `finally`, ke ca khi co exception
- Workspace tam se duoc xoa sau moi lan chay
- Runner hien tai lay file `.zip` dau tien va collection dau tien trong thu muc
- Runner tu dong resolve root cua project `Runner`, khong phu thuoc `bin/Debug/net8.0`

## 12. Kiem tra nhanh

Checklist truoc khi demo:

- Co file zip trong `submissions/`
- Co file collection trong `collections/`
- May co `npm` hoac da co `newman`
- Bai nop build duoc bang `dotnet build`
- Bai nop co the chay tren localhost
