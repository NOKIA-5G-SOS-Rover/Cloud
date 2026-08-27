

# NOKIA 5G SOS Rover - Cloud Infrastructure & Backend API

This repository contains the cloud infrastructure, database configuration, and backend API for the **NOKIA 5G SOS Rover** system.

Built with **ASP.NET Core / .NET 8**, the backend acts as the central communication layer between the React frontend dashboard, AI/ML camera processing modules, the MySQL database, and the rover.

The backend handles SOS events, authentication, telemetry, camera streams, rover-control sessions, database persistence, and real-time communication through SignalR.

## Tech Stack

* **C# / .NET 8:** Core backend framework.
* **ASP.NET Core Web API:** REST API implementation.
* **Entity Framework Core:** ORM used for database access and migrations.
* **Pomelo.EntityFrameworkCore.MySql:** MySQL provider for Entity Framework Core.
* **MySQL 8:** Relational database running through Docker Compose.
* **SignalR:** Real-time bidirectional communication between the backend, frontend, and rover clients.
* **Swagger / OpenAPI:** API documentation and testing in development.
* **Docker & Docker Compose:** Containerization and service orchestration.
* **GitHub Actions:** Automated CI/CD and container image publishing.



## Project Structure

The repository is organized as follows:

```text
/
├── backend/
│   ├── Controllers/
│   ├── Data/
│   ├── Dtos/
│   ├── Hubs/
│   ├── Middleware/
│   ├── Migrations/
│   ├── Models/
│   ├── Services/
│   ├── wwwroot/
│   ├── backend.csproj
│   └── Program.cs
├── .github/
│   └── workflows/
│       └── docker-publish.yml
├── Dockerfile
├── docker-compose.yml
└── Cloud.code-workspace
```

Important locations:

* **`.github/workflows/docker-publish.yml`**: Contains the CI/CD workflow used to build and publish the backend container image to GitHub Container Registry (GHCR).
* **`backend/`**: Main C# / .NET 8 backend application.
* **`backend/Controllers/`**: Defines HTTP API endpoints for events, authentication, cameras, telemetry, rover commands, rover control, and administration.
* **`backend/Models/`**: Entity Framework Core database entities.
* **`backend/Dtos/`**: Request and response payload models.
* **`backend/Hubs/`**: SignalR hubs used for real-time communication.
* **`backend/Data/`**: Contains `AppDbContext` and database-related code.
* **`backend/Migrations/`**: Entity Framework Core migration history.
* **`backend/Middleware/`**: Application middleware, including session handling.
* **`backend/Services/`**: Backend services such as camera handling, permissions, sessions, audit logging, and rover-control ownership.
* **`backend/wwwroot/uploads/`**: Stores uploaded SOS event images.
* **`Dockerfile`**: Root-level multi-stage Dockerfile for the .NET 8 backend.
* **`docker-compose.yml`**: Defines the backend API and MySQL database services.

---

## API Endpoints & Routing

The system exposes several endpoints used by the frontend dashboard, rover clients, and AI/ML detection modules.

### Events

1. **`GET /events` & `POST /events`**

   Handles SOS alerts.

   * `GET /events` loads stored SOS events, including historical alerts.
   * `POST /events` registers a new SOS detection and broadcasts the new alert through SignalR.

2. **`GET /events/{id}`**

   Returns a specific SOS event.

3. **`POST /events/{id}/image`**

   Allows the AI/ML module or another client to upload a `.jpg`, `.jpeg`, or `.png` snapshot associated with a specific SOS event.

4. **`PUT /events/{id}/status`**

   Updates the status of an existing SOS event.

### Authentication

* **`POST /api/Auth/register`**  
  Creates a new user.

* **`POST /api/Auth/login`**  
  Authenticates a user and returns session information.

* **`GET /api/Auth/me`**  
  Returns the currently authenticated user and session.

* **`POST /api/Auth/logout`**  
  Ends the current session.

Authenticated requests use the session identifier returned during login:

```http
X-Session-Id: <session-id>
```

### Administration

* **`GET /api/Admin/users`**  
  Lists users and online-session information.

* **`GET /api/Admin/sessions`**  
  Lists active sessions.

* **`DELETE /api/Admin/sessions/{sessionId}`**  
  Revokes a specific session.

* **`DELETE /api/Admin/users/{userId}/sessions`**  
  Revokes all sessions belonging to a user.

* **`GET /api/Admin/users/{userId}/permissions`**  
  Returns the user's permissions.

### Camera Streaming

* **`GET /stream/status`**  
  Returns the current status of configured cameras.

* **`GET /stream/{cameraId}`**  
  Serves a live MJPEG camera stream.

* **`GET /stream/{cameraId}/snapshot`**  
  Returns the most recent JPEG frame.

* **`POST /stream/{cameraId}/frame`**  
  Allows the rover or another camera source to push a JPEG frame to the backend.

### Telemetry

* **`POST /telemetry`**  
  Stores rover telemetry and broadcasts a `ReceiveTelemetry` SignalR event.

* **`GET /telemetry/{roverId}/latest`**  
  Returns the latest telemetry entry for a rover.

* **`GET /telemetry/{roverId}/history?take=100`**  
  Returns telemetry history for a rover.

### Rover Control

* **`POST /api/rover-control/take`**  
  Claims control of the rover for the current user.

* **`POST /api/rover-control/release`**  
  Releases the current user's rover-control session.

* **`GET /api/rover-control/status`**  
  Returns the current rover-control ownership status.

* **`POST /commands`**  
  Sends an authenticated rover command after permission and rover-control ownership checks.

The command is broadcast through SignalR to the rover-specific group:

```text
rover-{RoverId}
```

using the event:

```text
ReceiveCommand
```

* **`POST /rover/command`**  
  Legacy/test endpoint that currently receives and logs command information.

> The legacy `/rover/command` route should not be considered direct physical rover transport unless the hardware communication layer is implemented separately.

### SignalR Hub

**`/dashboardHub`**

This is the real-time communication channel used by the dashboard and rover clients.

A rover can register itself by calling:

```text
RegisterRobot(roverId)
```

The rover then joins the group:

```text
rover-{roverId}
```

SignalR events currently include:

* `ReceiveAlert`
* `ReceiveTelemetry`
* `ReceiveCommand`
* `RobotRegistered`
* Camera online/offline status updates

SignalR allows the backend to push data instantly without requiring the frontend or rover to continuously poll the API.

---

## Local Development

### Requirements

To run the backend without Docker, install:

* .NET 8 SDK
* MySQL 8 or another compatible MySQL server

From the repository root:

```bash
dotnet restore ./backend/backend.csproj
dotnet run --project ./backend/backend.csproj
```

The development launch configuration uses:

```text
HTTP:  http://localhost:5042
HTTPS: https://localhost:7270
```

Swagger is available when the backend runs in the development environment.

The backend also exposes:

```text
GET /health
```

to verify backend and database connectivity.

---

## Docker & Docker Compose

The application is containerized using a root-level multi-stage Dockerfile.

The Docker build performs:

1. NuGet restore
2. Release build
3. Release publish
4. Startup through the ASP.NET Core .NET 8 runtime image

The containerized API listens on:

```text
8080
```

### Build the Backend Image

From the repository root:

```bash
docker build -t rover-backend .
```

### Start the Complete Stack

The current Docker Compose stack contains:

* **`api`**: ASP.NET Core backend
* **`database`**: MySQL 8 database

Start or rebuild the services with:

```bash
docker compose up -d --build
```

Stop them with:

```bash
docker compose down
```

View API logs:

```bash
docker compose logs -f api
```

View database logs:

```bash
docker compose logs -f database
```

The backend is available at:

```text
http://localhost:8080
```

---

## Database Configuration

The application reads its connection string from:

```text
ConnectionStrings:DefaultConnection
```

Inside Docker Compose, the equivalent environment variable is:

```text
ConnectionStrings__DefaultConnection
```

Because the API and MySQL services are part of the same Docker Compose network, the backend connects to MySQL using the database service name:

```text
database
```

The development Compose connection string is:

```text
Server=database;Port=3306;Database=RoverSOSDb;User=rover_admin;Password=SuperSecretPassword123!;
```

Example Docker Compose configuration:

```yaml
ConnectionStrings__DefaultConnection: "Server=database;Port=3306;Database=RoverSOSDb;User=rover_admin;Password=SuperSecretPassword123!;"
```

> `host.docker.internal` should only be used when MySQL is running directly on the Docker host instead of as a service in the same Compose project.

> The credentials above are development credentials. Production passwords should be provided using deployment secrets or secure environment variables and should not be committed to the repository.

---

## Database Startup & Health Check

The API is configured to start only after the MySQL service becomes healthy.

Docker Compose uses:

```yaml
depends_on:
  database:
    condition: service_healthy
```

The MySQL service has its own health check.

This prevents the backend from starting its database-dependent initialization before MySQL is ready to accept connections.

The backend health endpoint can be checked with:

```bash
curl http://localhost:8080/health
```

---

## Database Management & Migrations

The database structure is managed using **Entity Framework Core migrations**.

Migration files are stored in:

```text
backend/Migrations/
```

When the application starts, it checks for pending EF Core migrations and applies them automatically.

This means that when the backend container starts after a new deployment, the database schema is updated to match the migrations included in the deployed version.

### Creating a New Migration

If the database model changes, for example by adding a new field to an entity, create a migration before pushing the changes.

From the backend directory:

```bash
dotnet ef migrations add YourNewMigrationName
```

Or from the repository root:

```bash
dotnet ef migrations add YourNewMigrationName --project ./backend/backend.csproj
```

Review the generated migration files before committing them.

Do not modify previously applied migrations unless performing a deliberate migration repair or database recovery.

---

## Database Access

You can access the MySQL database directly through the running database container.

### 1. Access the MySQL Console

```bash
docker exec -it rover-database mysql -u rover_admin -p RoverSOSDb
```

Enter the configured password when prompted:

```text
SuperSecretPassword123!
```

> If the configured database container name is different, replace `rover-database` with the actual container name.

---

## Tutorial: Manual Database Inserts

Direct SQL can be useful for development, debugging, and controlled test data.

Normal application data should preferably be created through the backend API.

### 1. Inspect a Table

If you are unsure of the available columns:

```sql
DESCRIBE TableName;
```

For example:

```sql
DESCRIBE Events;
```

### 2. Insert Test Data

General syntax:

```sql
INSERT INTO TableName (Column1, Column2, Column3)
VALUES ('sample_text', 99, NOW());
```

Useful SQL rules:

* Text values are enclosed in single quotes: `'text'`
* Numeric values are written directly: `10`
* Use `NOW()` for the current database timestamp
* Ensure all required non-null columns are included

### 3. Example SOS Alert Insert

A test event can be inserted directly into the `Events` table if the current schema contains the listed columns:

```sql
INSERT INTO Events (
    Timestamp,
    RoverId,
    SessionId,
    AlertType,
    Source,
    DetectedAt,
    LocationX,
    LocationY,
    BoundingBoxWidth,
    BoundingBoxHeight,
    ConfidenceScore,
    MotorHaltRequested,
    InjuryClass,
    CameraId,
    Status
)
VALUES (
    NOW(),
    'ROVER-TEST',
    'Test-Session',
    'backend mysql test - manual',
    'Console Insert',
    NOW(),
    45.7,
    21.2,
    10.0,
    10.0,
    0.99,
    0,
    'none',
    'cam-test',
    'warning'
);
```

Before using a manual insert, verify the current table schema:

```sql
DESCRIBE Events;
```

### 4. Exit MySQL

```sql
exit
```

---

## Camera Configuration

Camera configuration is stored under the `Cameras` section.

Relevant settings include:

* **`OfflineAfterSeconds`**: Time after which a camera is considered offline.
* **`MaxFrameBytes`**: Maximum accepted JPEG frame size.
* **`Sources`**: Configured camera definitions.
* **`UpstreamUrl`**: Optional upstream camera URL.
* **`UpstreamPollIntervalMs`**: Delay between snapshot polling requests.

Configured camera IDs currently include entries such as:

```text
cam1
cam2
```

If an `UpstreamUrl` is configured, the backend can pull frames from the source.

If no upstream URL is configured, the rover can push frames directly using:

```text
POST /stream/{cameraId}/frame
```

Docker Compose can map variables such as:

```text
CAM1_UPSTREAM_URL
CAM2_UPSTREAM_URL
```

into the camera source configuration.

---

## Deployment & Server Updates

The backend is containerized and can be published to **GitHub Container Registry (GHCR)** through GitHub Actions.

The CI/CD workflow is located at:

```text
.github/workflows/docker-publish.yml
```

Because the Dockerfile is stored in the repository root, the workflow should build using:

```text
Build context: .
Dockerfile: ./Dockerfile
```

An example published image name is:

```text
ghcr.io/nokia-5g-sos-rover/rover-backend:latest
```

When the production environment is configured to use published images, a typical server update flow is:

```bash
# Pull the newest container images
docker compose pull

# Stop and remove existing containers
docker compose down

# Start the updated services
docker compose up -d
```

If the production Compose configuration builds locally instead of using prebuilt GHCR images, use:

```bash
docker compose up -d --build
```

---

## Useful Docker Commands

List running containers:

```bash
docker ps
```

List all containers:

```bash
docker ps -a
```

Remove a stopped container:

```bash
docker rm <container-name-or-id>
```

Remove all stopped containers:

```bash
docker container prune
```

Start or rebuild the project:

```bash
docker compose up -d --build
```

Stop the project:

```bash
docker compose down
```

View backend logs:

```bash
docker compose logs -f api
```

View MySQL logs:

```bash
docker compose logs -f database
```

---

## Checking MySQL Port 3306 on Windows

If MySQL cannot start because port `3306` is already in use, check the process using PowerShell.

```powershell
Get-NetTCPConnection -LocalPort 3306 |
    Select-Object LocalAddress, LocalPort, State, OwningProcess
```

To include the owning process name:

```powershell
Get-NetTCPConnection -LocalPort 3306 |
    Select-Object LocalAddress, LocalPort, State, OwningProcess,
        @{Name="ProcessName";Expression={(Get-Process -Id $_.OwningProcess).ProcessName}}
```

Alternative:

```powershell
netstat -ano | findstr :3306
```

Then inspect a process by PID:

```powershell
Get-Process -Id <PID>
```

---

## CI/CD

The GitHub Actions workflow is responsible for building and publishing the backend Docker image.

The workflow can use GitHub's automatically provided:

```text
GITHUB_TOKEN
```

with package write permissions to authenticate to GitHub Container Registry.

Before production deployment, verify that:

* the workflow uses the root Dockerfile
* the Docker build context is the repository root
* package publishing permissions are enabled
* the expected GHCR image tag is being generated

---

## Production Checklist

Before exposing the backend in a production environment:

* Provide the MySQL connection string through secure configuration.
* Do not store production database passwords in the repository.
* Set the appropriate `ASPNETCORE_ENVIRONMENT`.
* Verify that the API can connect to the MySQL service.
* Verify that the MySQL health check succeeds.
* Confirm that pending EF Core migrations can be applied.
* Preserve the MySQL volume when recreating containers.
* Preserve uploaded event images.
* Configure camera upstream URLs or rover frame pushing.
* Test the `/health` endpoint.
* Review Swagger exposure.
* Review CORS configuration.
* Test SOS event creation and status updates.
* Test event image uploads.
* Test camera streaming.
* Test SignalR connectivity.
* Test telemetry updates.
* Test authenticated rover-control ownership.
* Test SignalR rover-command delivery.
* Verify the GitHub Actions workflow and GHCR image.

---

## Current Architecture

The backend currently provides:

* REST API endpoints
* MySQL persistence
* Entity Framework Core migrations
* Automatic migration application at startup
* Session-based authentication
* Permission checks
* Administrative endpoints
* SOS event management
* Event image uploads
* Camera stream ingestion
* MJPEG streaming
* Rover telemetry
* Rover-control ownership
* SignalR real-time communication
* Docker containerization
* Docker Compose orchestration
* MySQL health-gated API startup
* GitHub Actions container publishing

The local .NET launch configuration uses:

```text
HTTP:  5042
HTTPS: 7270
```

The Dockerized API listens on:

```text
8080
```

---

## Security Notes

The credentials shown in this README are intended for development/testing only.

Before public or production deployment:

* move database credentials to secret storage
* rotate credentials that have previously been committed to Git history
* restrict direct database exposure
* configure production-safe CORS rules
* review Swagger availability
* terminate HTTPS through the intended production infrastructure
* review authentication and authorization settings

---

## Project Status

This repository contains the cloud/backend component of the **NOKIA 5G SOS Rover** system and is under active development.

Some hardware-facing functionality may still use test routes or SignalR-based communication until the final physical rover transport layer is fully integrated.
