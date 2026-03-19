# Grading MVP Flow

## Trigger grading
- POST `api/submissions/{id}/run`

## Results
- GET `api/submissions/{id}/results`
- GET `api/submissions/{id}/logs`

## Import TestCases from collection.json
- POST `api/exams/{examId}/testcases/import-from-collection?defaultScore=1`

## Upload endpoint-spec.json
- Upload via `api/exams/{examId}/resources` (multipart/form-data) with field `endpointSpec`

## Runner requirements
- `dotnet` in PATH
- `newman` in PATH (`npm i -g newman`)
- Optional: `sqlcmd` in PATH for database.sql import

## Config
Edit `appsettings.json`:
```
"GradingRunner": {
  "WorkingRoot": "data",
  "DotnetPath": "dotnet",
  "NewmanPath": "newman",
  "HealthCheckSeconds": 30,
  "HealthPath": "/",
  "RestoreTimeoutSeconds": 300,
  "BuildTimeoutSeconds": 600,
  "NewmanTimeoutSeconds": 300,
  "RunTimeoutSeconds": 900,
  "EnableSpecCheck": true,
  "SwaggerPath": "/swagger/v1/swagger.json",
  "SqlCmdPath": "sqlcmd",
  "SqlCmdServer": "localhost,1433",
  "SqlCmdUser": "sa",
  "SqlCmdPassword": "YourStrong!Pass1",
  "StudentDbConnection": "Server=localhost,1433;Database=FA25BearDB;User Id=sa;Password=YourStrong!Pass1;Encrypt=False;TrustServerCertificate=True;"
}
```

## Notes
- `.rar` extraction requires `7z` in PATH.
- The runner picks a random port between 5000-5999.
