# FE API Specification (Management)

Base URL: `https://{host}`

Common response envelopes

`PagedResult<T>`

```json
{
  "page": 1,
  "pageSize": 20,
  "total": 123,
  "items": []
}
```

`ProblemDetails` (error)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Error message here",
  "traceId": "00-..."
}
```

## Exams

### GET `/api/exams`

List exams.

Query params

- `search` string?
- `sort` string?
- `order` string?
- `page` int (default 1)
- `pageSize` int (default 20)

Response 200

```json
{
  "page": 1,
  "pageSize": 20,
  "total": 2,
  "items": [
    {
      "examId": 1,
      "examName": "SQL Basic",
      "sqlScriptPath": "storage/exams/1.sql"
    }
  ]
}
```

### GET `/api/exams/{examId}`

Get exam by id.

Path params

- `examId` int

Response 200

```json
{
  "examId": 1,
  "examName": "SQL Basic",
  "sqlScriptPath": "storage/exams/1.sql"
}
```

Response 404

- `ProblemDetails`

### POST `/api/exams`

Create exam. `multipart/form-data`

Form fields

- `examName` string (required)
- `sqlFile` file (optional)

Response 201

```json
{
  "examId": 1,
  "examName": "SQL Basic",
  "sqlScriptPath": "storage/exams/1.sql"
}
```

Response 400, 409

- `ProblemDetails`

### PATCH `/api/exams/{examId}`

Patch exam. `multipart/form-data`

Path params

- `examId` int

Form fields

- `examName` string?
- `sqlFile` file?

Response 200

```json
{
  "examId": 1,
  "examName": "SQL Basic - Updated",
  "sqlScriptPath": "storage/exams/1.sql"
}
```

Response 400, 404, 409

- `ProblemDetails`

## Grading runner

### GET `/api/testrunner`

Start runner (fire-and-forget).

Response 200

```json
{
  "message": "Runner started."
}
```

Response 409

```json
{
  "message": "Runner is already running.",
  "isRunning": true,
  "processId": 12345,
  "startedAtUtc": "2026-03-27T10:20:30.0000000+00:00"
}
```

### GET `/api/testrunner/status`

Get current runner status.

Response 200

```json
{
  "isRunning": false,
  "processId": null,
  "startedAtUtc": "2026-03-27T10:20:30.0000000+00:00",
  "lastCompletedAtUtc": "2026-03-27T10:22:01.0000000+00:00",
  "lastExitCode": 0,
  "lastError": null
}
```

### POST `/api/testresults`

Runner pushes grading result. API saves to database and broadcasts to FE.

Request body

```json
{
  "studentName": "1_HE150001_20260327",
  "score": 80,
  "status": "build ok",
  "reportFilePath": "D:/Project/PRN232-Grading-Tool/src/backend/Api_RestAPI_gradingTool/Runner/reports/HE150001/result.json"
}
```

Response 200

```json
{
  "studentName": "1_HE150001_20260327",
  "score": 80,
  "status": "build ok",
  "reportFilePath": "D:/Project/PRN232-Grading-Tool/src/backend/Api_RestAPI_gradingTool/Runner/reports/HE150001/result.json"
}
```

Response 400

- `ProblemDetails` (`studentName is required.`)

Response 404

- `ProblemDetails` (`Submission not found for studentName '...'.`)

## SignalR

### Hub name and route

- Hub class: `TestHub`
- Hub route: `/testHub`

### Server-to-client events

- `ReceiveTestResult`

```json
{
  "studentName": "1_HE150001_20260327",
  "score": 80,
  "status": "build ok",
  "reportFilePath": "D:/Project/PRN232-Grading-Tool/src/backend/Api_RestAPI_gradingTool/Runner/reports/HE150001/result.json"
}
```

- `RunnerStatusChanged` (when runner starts/finishes/fails)

```json
{
  "isRunning": true,
  "processId": 12345,
  "startedAtUtc": "2026-03-27T10:20:30.0000000+00:00"
}
```

```json
{
  "isRunning": false,
  "processId": null,
  "lastCompletedAtUtc": "2026-03-27T10:22:01.0000000+00:00",
  "lastExitCode": 0,
  "lastError": null
}
```

- `RunnerCompleted`

```json
{
  "status": "ok",
  "output": "..."
}
```

```json
{
  "status": "build false",
  "error": "..."
}
```

### Client-to-server method available in hub

- Method: `BroadcastTestResult(TestResultRequest result)`
- Broadcast event emitted by method: `ReceiveTestResult`

### DELETE `/api/exams/{examId}`

Delete exam.

Path params

- `examId` int

Response 204

- No body

Response 400, 404, 409

- `ProblemDetails`

## Submissions

### GET `/api/exams/{examId}/submissions`

List submissions by exam.

Path params

- `examId` int

Query params

- `search` string?
- `sort` string?
- `order` string?
- `page` int (default 1)
- `pageSize` int (default 20)

Response 200

```json
{
  "page": 1,
  "pageSize": 20,
  "total": 2,
  "items": [
    {
      "id": 10,
      "examId": 1,
      "studentName": "Nguyen Van A",
      "studentCode": "HE150001",
      "filePath": "storage/submissions/10.zip",
      "status": 0
    }
  ]
}
```

Response 400, 404, 409

- `ProblemDetails`

### GET `/api/submissions/{id}`

Get submission by id.

Path params

- `id` int

Response 200

```json
{
  "id": 10,
  "examId": 1,
  "studentName": "Nguyen Van A",
  "studentCode": "HE150001",
  "filePath": "storage/submissions/10.zip",
  "status": 0
}
```

Response 404

- `ProblemDetails`

### POST `/api/exams/{examId}/submissions`

Upload submission. `multipart/form-data`

Path params

- `examId` int

Form fields

- `studentName` string (required)
- `studentCode` string (required)
- `file` file (required)

Response 201

```json
{
  "id": 10,
  "examId": 1,
  "studentName": "Nguyen Van A",
  "studentCode": "HE150001",
  "filePath": "storage/submissions/10.zip",
  "status": 0
}
```

Response 400, 404, 409

- `ProblemDetails`

### PATCH `/api/submissions/{id}`

Patch submission. `multipart/form-data`

Path params

- `id` int

Form fields

- `studentName` string?
- `studentCode` string?
- `file` file?

Response 200

```json
{
  "id": 10,
  "examId": 1,
  "studentName": "Nguyen Van A",
  "studentCode": "HE150001",
  "filePath": "storage/submissions/10.zip",
  "status": 0
}
```

Response 400, 404, 409

- `ProblemDetails`

### DELETE `/api/submissions/{id}`

Delete submission.

Path params

- `id` int

Response 204

- No body

Response 400, 404, 409

- `ProblemDetails`

## Test cases

### GET `/api/exams/{examId}/testcases`

List testcases by exam.

Path params

- `examId` int

Query params

- `search` string?
- `sort` string?
- `order` string?
- `page` int (default 1)
- `pageSize` int (default 20)

Response 200

```json
{
  "page": 1,
  "pageSize": 20,
  "total": 2,
  "items": [
    {
      "id": 5,
      "examId": 1,
      "filePath": "storage/testcases/5.zip"
    }
  ]
}
```

Response 400, 404, 409

- `ProblemDetails`

### GET `/api/exams/{examId}/testcases/{id}`

Get testcase by id.

Path params

- `examId` int
- `id` int

Response 200

```json
{
  "id": 5,
  "examId": 1,
  "filePath": "storage/testcases/5.zip"
}
```

Response 404

- `ProblemDetails`

### POST `/api/exams/{examId}/testcases`

Upload testcase collection. `multipart/form-data`

Path params

- `examId` int

Form fields

- `file` file (required)

Response 200

```json
{
  "id": 5,
  "examId": 1,
  "filePath": "storage/testcases/5.zip"
}
```

Response 400, 404, 409

- `ProblemDetails`

### PATCH `/api/exams/{examId}/testcases/{id}`

Patch testcase. `multipart/form-data`

Path params

- `examId` int
- `id` int

Form fields

- `filePath` string?
- `file` file?

Response 200

```json
{
  "id": 5,
  "examId": 1,
  "filePath": "storage/testcases/5.zip"
}
```

Response 400, 404, 409

- `ProblemDetails`

### DELETE `/api/exams/{examId}/testcases/{id}`

Delete testcase.

Path params

- `examId` int
- `id` int

Response 204

- No body

Response 400, 404, 409

- `ProblemDetails`
