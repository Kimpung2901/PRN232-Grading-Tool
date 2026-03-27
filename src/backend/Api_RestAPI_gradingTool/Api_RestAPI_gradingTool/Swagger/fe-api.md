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
