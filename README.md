# PRN232 – API Auto Grading System

Automatic REST API examination and scoring tool for PRN232.

## Overview
This project automates grading for REST API assignments. It manages exams, endpoint specs, test cases, and runs automated checks to compute scores and reports.

## Tech Stack
- Backend: ASP.NET Core Web API (.NET 8), EF Core
- Database: SQL Server (Docker)
- Frontend: React + TypeScript (Ant Design)
- DevOps: Docker Compose

## Local Setup
### 1) Prerequisites
- Docker Desktop (Linux containers)
- .NET SDK 8

### 2) Environment variables
Copy the sample env file and edit passwords:
```cmd
copy .env.example .env
```
Edit `.env`:
```
MSSQL_SA_PASSWORD=your_password
GRADING_DB_CONNECTION=Server=localhost,1433;Database=PRN232_GradingTool;User Id=sa;Password=your_password;Encrypt=False;TrustServerCertificate=True;
```

### 3) Start SQL Server
```cmd
docker compose up -d
```

### 4) Restore database (from shared backup)
The database backup (`.bak`) is shared separately (not committed to git).
Use SSMS:
1. Connect to `localhost,1433`
2. Restore Database… and select the `.bak` file

### 5) Run API
```cmd
dotnet restore
dotnet run --project src/backend/Api_RestAPI_gradingTool/Api_RestAPI_gradingTool.csproj
```

## Notes
- `.env` is ignored by git. Do not commit secrets.
- If you need the database, ask the team for the latest `.bak` backup.

## Docs
- Notion: DB/Spec (internal link)
