# NexusLearningSys — Deployment Guide

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| Docker Desktop | 4.x+ | Container runtime |
| Docker Compose | 2.x+ | Multi-service orchestration |
| .NET SDK | 9.0 | Build & EF migrations |
| SQL Server | 2022 (or Azure) | Database (included in Docker) |

---

## Quick Start with Docker Compose

### 1. Clone the repository
```bash
git clone <repo-url>
cd NexusLearningSys
```

### 2. (Optional) Set your OpenAI API Key
```bash
# Windows PowerShell
$env:OPENAI_API_KEY = "sk-..."

# Linux / macOS
export OPENAI_API_KEY="sk-..."
```

### 3. Build and start all services
```bash
docker-compose up --build -d
```

This will:
- Start **SQL Server 2022** on port `1433`
- Build and start the **Nexus Web App** on port `8080`
- Automatically apply database migrations on first startup

### 4. Open in browser
```
http://localhost:8080
```

### 5. Default Admin credentials (seeded on startup)
| Email | Password | Role |
|-------|----------|------|
| `admin@nexus.edu` | `Admin@123!` | Admin |

---

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ConnectionStrings__DefaultConnection` | (set in compose) | SQL Server connection string |
| `ASPNETCORE_ENVIRONMENT` | `Production` | App environment |
| `RunMigrationsOnStartup` | `true` | Auto-apply EF migrations |
| `OpenAI__ApiKey` | *(empty)* | OpenAI API key for AI features |
| `OpenAI__ChatModel` | `gpt-4o-mini` | Model to use |

---

## Development Without Docker

### 1. Update connection string
Edit `Nexus.Web/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=NexusLearningDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "RunMigrationsOnStartup": true
}
```

### 2. Apply migrations manually
```bash
cd Nexus.Data
dotnet ef database update --startup-project ../Nexus.Web
```

### 3. Run the app
```bash
cd Nexus.Web
dotnet run
```

---

## Running Migrations Manually

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project Nexus.Data --startup-project Nexus.Web

# Apply migrations
dotnet ef database update --project Nexus.Data --startup-project Nexus.Web

# Rollback last migration
dotnet ef database update <PreviousMigrationName> --project Nexus.Data --startup-project Nexus.Web
```

---

## Stopping the Application

```bash
# Stop containers (keep data)
docker-compose down

# Stop and remove all data (full reset)
docker-compose down -v
```

---

## Module Summary (Abu Bakar's Contribution)

| Module | Description |
|--------|-------------|
| **Semester Lifecycle** | Admin manages academic sessions; courses linked to semesters |
| **Attendance** | Teachers mark attendance; students view their records |
| **Weighted Grading + GPA** | Configurable weights per course; 4.0 GPA + CGPA calculation |
| **Exam Security** | MaxAttempts restriction + question shuffling for quizzes |
| **Report Generation** | PDF transcript, Excel gradebook, PDF attendance report |
| **Docker Deployment** | Full containerized deployment with SQL Server |
