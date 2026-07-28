
---

# NOKIA 5G SOS Rover - Cloud Infrastructure & Backend API

This repository contains the cloud infrastructure, database configurations, and backend API for the NOKIA 5G SOS Rover system. Built with ASP.NET Core, this backend serves as the central dispatcher linking the React frontend dashboard, the AI/ML camera processing modules, and the embedded hardware on the rover.

## Tech Stack

* **C# / .NET 8:** Core backend framework.
* **Entity Framework (EF) Core:** ORM for database management.
* **MySQL:** Relational database running via Docker.
* **SignalR:** WebSockets for real-time bidirectional communication.
* **Docker & Docker Compose:** Containerization and deployment orchestration.
* **GitHub Actions:** Automated CI/CD pipeline.

---

## Project Structure

Based on the repository layout, here is a quick overview of the essential directories:

* **`.github/workflows/docker-publish.yml`**: Contains the CI/CD pipeline. Automatically builds and pushes the Docker images to GitHub Container Registry (GHCR) upon new commits.
* **`backend/`**: The main C# .NET 8 application.
* `/Controllers`: Defines the HTTP API endpoints.
* `/Models` & `/Dtos`: Data structures and payload definitions.
* `/Hubs`: SignalR WebSocket hubs for real-time frontend updates.
* `/Data` & `/Migrations`: Entity Framework database context and schema history.


* **`docker-compose.yml`**: The blueprint for deploying the entire stack (Frontend, Backend, MySQL Database, and Adminer) on the production virtual machine.

---

## API Endpoints & Routing

The system exposes several key endpoints used by the frontend dashboard and the AI/ML detection modules:

1. `**GET /events` & `POST /events**`
Handles SOS alerts. The frontend uses `GET` to load the historical data in the "Past Alerts" section. The AI module or frontend uses `POST` to register new detections.
2. **`POST /events/{id}/image`**
Used by the AI/ML module to upload a visual frame (snapshot) of the detected victim, linking it to a specific SOS event ID.
3. **`POST /rover/command`**
Receives manual movement commands (WASD) from the frontend operator. Currently configured to process and route these commands to the embedded hardware.
4. **`WS /dashboardHub` (WebSockets)**
The permanent tunnel for real-time data flow. When the backend registers a new SOS alert, it pushes the event through this channel to trigger the frontend UI/audio notification instantly, without requiring a page refresh.

---

## Deployment & Server Updates

The entire codebase is containerized. The GitHub Actions pipeline automatically builds the images. To update the production Virtual Machine with the latest code, SSH into the VM and run the following commands in the project directory:

```bash
# Pull the latest images from GitHub Container Registry
docker-compose pull

# Stop and remove the current running containers
docker-compose down

# Start the updated system in the background
docker-compose up -d

```

---

## Database Management & Migrations

The database structure is managed exclusively through code using Entity Framework Core.
On every startup (`docker-compose up`), `Program.cs` automatically runs pending migrations and updates the MySQL tables to match the C# classes.

If you modify the database schema (e.g., adding a new field to the `Event` class), generate a local migration before pushing:

```bash
# Generate the SQL modifications
dotnet ef migrations add YourNewMigrationName

```

Once pushed and the backend container is restarted on the server, the changes apply automatically.

### Database GUI (Adminer)

A visual interface is available for database administration.

1. Access `http://<YOUR_VM_IP>:8080` in your browser.
2. Log in using the following credentials:
* **System:** MySQL / MariaDB
* **Server:** `db` *(This must be exactly 'db', representing the Docker service name)*
* **Username:** `rover_admin`
* **Password:** `SuperSecretPassword123!`
* **Database:** `RoverSOSDb`



---

## Tutorial: Manual Database Inserts (SQL)

If you need to inject test data directly into the system without using the frontend or API clients like Postman, you can execute SQL queries directly inside the database container.

### 1. Access the MySQL Console

Connect to the database container running on the VM:

```bash
docker exec -it rover_database mysql -u rover_admin -p RoverSOSDb

```

*(Enter the password when prompted: `SuperSecretPassword123!`)*

### 2. Example: Insert an SOS Alert (Events Table)

To simulate a test alert, run this `INSERT` command. EF Core names the table `Events` by default.

```sql
INSERT INTO Events (Timestamp, RoverId, SessionId, AlertType, Source, DetectedAt, LocationX, LocationY, BoundingBoxWidth, BoundingBoxHeight, ConfidenceScore, MotorHaltRequested, InjuryClass, CameraId, Status)
VALUES (NOW(), 'ROVER-TEST', 'Test-Session', 'backend mysql test - manual', 'Console Insert', NOW(), 45.7, 21.2, 10.0, 10.0, 0.99, 0, 'none', 'cam-test', 'warning');

```

### 3. General Guide for Adding Data to Tables

If new tables are added in the future (e.g., `Devices`, `Users`), follow these steps:

**Step A: Inspect the table structure**
If you are unsure of the column names, check them using:

```sql
DESCRIBE TableName;

```

**Step B: Format the INSERT command**
Provide the table name, specify the required columns in parentheses, and append the corresponding values.

* Enclose text in single quotes: `'text'`
* Numbers can be written directly: `10`
* Use `NOW()` for the current timestamp.

```sql
INSERT INTO TableName (Column1, Column2, Column3)
VALUES ('sample_text', 99, NOW());

```

**Step C: Exit**
Once you have finished managing the database, exit the console cleanly:

```sql
exit

```