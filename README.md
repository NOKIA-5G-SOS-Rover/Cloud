# NOKIA 5G SOS Rover - Backend API

Acesta este backend-ul pentru sistemul SOS Rover, dezvoltat cu ASP.NET Core. Aici se afla toata logica care leaga frontend-ul de baza de date, proceseaza alertele si ruteaza comenzile rover-ului.

## Tehnologii folosite

* **C# / .NET 8**
* **Entity Framework Core** (ORM pt lucrul cu baza de date)
* **MySQL** (Baza de date rulata in Docker)
* **SignalR** (WebSockets pt comunicare in timp real)
* **Docker & Docker Compose**

## Arhitectura Rute

Sistemul expune 3 endpoint-uri principale cu care comunica React-ul:

1. **`GET /events` & `POST /events**`
Aici se salveaza si se preiau alertele. Frontend-ul foloseste GET ca sa incarce istoricul din sectiunea "Past Alerts" si POST cand apesi "Simulate SOS" sau cand rover-ul detecteaza ceva pe bune.
2. **`POST /rover/command`**
Aici vin comenzile cand te joci cu tastele WASD pe camera. E setat sa logheze comenzile de miscare in consola.
3. **`WS /dashboardHub`**
Tunelul permanent prin care curg datele. Cand backend-ul salveaza un SOS nou, il arunca direct pe canalul asta ca sa dea trigger la notificarea cu sunet pe site fara sa dai refresh.

## Cum rulezi si actualizezi pe server

Codul este containerizat. Ca sa actualizezi masina virtuala cand faci modificari pe GitHub, trb doar sa intri prin SSH si sa dai comenzile astea:

```bash
docker-compose pull
docker-compose down
docker-compose up -d

```

Asta trage codul proaspat, opreste sistemul vechi si il ridica pe cel nou.

## Baza de date si Migratii

Structura bazei de date e gestionata exclusiv din cod prin Entity Framework.
La fiecare pornire (`docker-compose up`), fisierul `Program.cs` ruleaza automat ultimele migratii si creaza/updateaza tabelele din MySQL ca sa se potriveasca cu clasele de C#.

Daca faci modificari la tabele (de exemplu adaugi un camp nou in clasa `Event`), generezi migratia local asa:

```bash
# genereaza modificarile sql
dotnet ef migrations add NumeMigratieNoua

```

Dupa ce dai push si repornesti containerul de backend pe server, modificarile se aplica singure.

---

## Tutorial: Administrare Manuala (SQL Inserts)

Daca vrei sa bagi date la mana direct in sistem fara sa treci prin frontend sau Postman, o poti face direct din containerul de baza de date. E util pt testat.

### 1. Accesul in consola MySQL

Intra pe terminalul masinii virtuale si conecteaza-te in containerul de baza de date:

```bash
docker exec -it rover_database mysql -u rover_admin -p RoverSOSDb

```

*(Parola este: `SuperSecretPassword123!`)*

### 2. Exemplu: Inserare alerta SOS (Tabelul Events)

Ca sa adaugi o alerta de test (gen testul teohh), rulezi comanda asta de INSERT. EF Core numeste tabelul `Events` conform fisierului de DbContext.

```sql
INSERT INTO Events (Timestamp, RoverId, SessionId, AlertType, Source, DetectedAt, LocationX, LocationY, BoundingBoxWidth, BoundingBoxHeight, ConfidenceScore, MotorHaltRequested, InjuryClass, CameraId, Status)
VALUES (NOW(), 'ROVER-TEST', 'Test-Session', 'backend mysql test - teohh', 'Manual Insert', NOW(), 0, 0, 10, 10, 1.0, 0, 'none', 'cam-test', 'warning');

```

### 3. Ghid general pt adaugarea in alte tabele

Daca pe viitor ai mai multe tabele (ex: `Devices`, `Users`) si vrei sa bagi date, pasii sunt aceiasi:

**Pasul A: Afla exact cum arata tabelul**
Daca nu stii sigur cum se numesc coloanele, poti sa le inspectezi cu:

```sql
DESCRIBE NumeTabel;

```

**Pasul B: Formateaza comanda de INSERT**
Regula de baza e sa dai numele tabelului, apoi coloanele obligatorii in paranteze, urmate de valori.

* Textele le pui in ghilimele simple `'text'`
* Numerele merg direct `10`
* Pt data curenta folosesti `NOW()`

```sql
INSERT INTO NumeTabel (Coloana1, Coloana2, Coloana3)
VALUES ('valoare_text', 99, NOW());

```

**Pasul C: Iesire**
Dupa ce ai terminat de facut modificari in baza de date, iesi curat scriind:

```sql
exit

```