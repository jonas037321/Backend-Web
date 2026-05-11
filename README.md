# Feature 6 Backend - ASP.NET Core WebAPI

## Übersicht

Feature 6 Backend ist eine vollständige ASP.NET Core WebAPI mit:
- **Api**: WebAPI mit Swagger/OpenAPI-Dokumentation
- **ORM**: Entity Framework Core mit MySQL (Pomelo)
- **Models**: Gemeinsame Datenmodelle (User, Gender Enum)

## Projektstruktur

```
backend/
├── Api/                    # ASP.NET Core WebAPI
│   ├── Controllers/
│   │   └── UsersController.cs    # REST-Endpoints für User
│   ├── Program.cs                # Konfiguration + Swagger
│   └── Api.csproj
├── ORM/                    # Entity Framework Core Projekt
│   ├── DbManager.cs              # DbContext + MySQL-Connection
│   └── ORM.csproj
├── Models/                 # Gemeinsame Modelle
│   ├── User.cs                   # User Model + Gender Enum
│   └── Models.csproj
└── backend.slnx            # Solution File
```

## Installation & Konfiguration

### 1. Abhängigkeiten
- **.NET 10.0**
- **MySQL 5.7+** (Verbindung konfiguriert für `localhost`, User: `root`, Passwort: `244466666`)

### 2. Datenbank erstellen
```sql
CREATE DATABASE swp_maui;
```

### 3. EF Core Migrations ausführen
```bash
cd backend\ORM
dotnet ef migrations add InitialCreate --project ORM.csproj --startup-project ..\Api\Api.csproj
dotnet ef database update --project ORM.csproj --startup-project ..\Api\Api.csproj
```

### 4. API starten
```bash
cd backend\Api
dotnet run
```

Die API ist dann erreichbar unter:
- **Swagger UI**: `https://localhost:5001/swagger`
- **API Root**: `https://localhost:5001`

## API Endpoints

### Users
- **GET** `/api/users` - Alle User abrufen
- **GET** `/api/users/{id}` - User nach ID abrufen
- **POST** `/api/users` - Neuen User erstellen
- **PUT** `/api/users/{id}` - User aktualisieren
- **DELETE** `/api/users/{id}` - User löschen

### Request Body Beispiel (POST/PUT)
```json
{
  "username": "john_doe",
  "email": "john@example.com",
  "birthdate": "1990-05-15T00:00:00",
  "gender": 0,
  "password": "securePassword123"
}
```

**Gender Enum:**
- `0` = Male
- `1` = Female
- `2` = Other

## CORS

Die API erlaubt Anfragen von:
- `http://localhost:5173` (Vite Dev Server)
- `http://localhost:3000` (Alternative)

Für Production: `appsettings.json` oder `Program.cs` anpassen.

## NuGet Pakete

- **Microsoft.EntityFrameworkCore**: 9.0.0
- **Microsoft.EntityFrameworkCore.Tools**: 9.0.0
- **Pomelo.EntityFrameworkCore.MySql**: 9.0.0
- **Swashbuckle.AspNetCore**: 6.4.0

## Fehlerbehebung

### Datenbankverbindung schlägt fehl
- Stelle sicher, dass MySQL läuft
- Verifiziere die Verbindungszeichenkette in `DbManager.cs`
- Prüfe Firewall-Einstellungen

### Migrations-Fehler
```bash
dotnet ef database drop --project ORM.csproj --startup-project ..\Api\Api.csproj
dotnet ef database update --project ORM.csproj --startup-project ..\Api\Api.csproj
```

## Integration mit Vue Frontend

Das Vue Frontend kann über `APIService` mit der API kommunizieren:

```typescript
import apiService from '@/Services/APIService';

// Beispiel: User erstellen
const newUser = {
  username: 'test_user',
  email: 'test@example.com',
  birthdate: new Date('1995-01-01'),
  gender: 0,
  password: 'password123'
};

apiService.post('/api/users', newUser)
  .then(response => console.log('User erstellt:', response))
  .catch(error => console.error('Fehler:', error));
```

## Entwicklung

### Hot Reload
```bash
cd backend\Api
dotnet watch
```

### Tests ausführen
```bash
dotnet test
```

---

**Stand**: 28.04.2026 | **Feature**: 6 | **Autor**: Development Team
