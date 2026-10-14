# Health Companion – Backend

ASP.NET Core Web API (.NET 10) für die Health-Companion-App. Verwaltet Benutzer (MySQL) und holt die Daten der Polar-Uhr über die Polar AccessLink API.

## Projekte

```
backend/
├── Api/      Web API: Program.cs, appsettings.json
│   ├── Controllers/  AuthController (Login/Registrierung), PolarController (OAuth), PolarDataController (alle Daten)
│   └── Services/     PolarClient (Aufrufe an Polar), ExerciseDetailBuilder (Höhenmeter, Verläufe)
├── Models/   User + DTOs für die Polar-Antworten
└── ORM/      DbManager (EF Core DbContext) + Migrationen
```

## Starten

Voraussetzungen: .NET 10 SDK und MySQL auf `localhost` mit der Datenbank `HealthCompanion`.
Die Verbindung steht in `Api/appsettings.json` unter `ConnectionStrings:DefaultConnection`.

```bash
dotnet run --project Api/Api.csproj --launch-profile https
```

- API: `https://localhost:7262`
- Swagger: `https://localhost:7262/swagger`

## Endpoints

| Methode | Route | Zweck |
|---|---|---|
| POST | `/api/Registration` | User anlegen (Passwort wird gehasht) |
| POST | `/api/Login` | Login prüfen |
| GET | `/api/polar/status?email=` | Ist der User mit Polar verbunden? |
| GET | `/api/polar/connect?email=` | Weiterleitung zum Polar-Login (OAuth) |
| GET | `/api/polar/callback` | Polar-Rückruf: Token in der DB speichern |
| GET | `/api/activities/{email}` | Tagesaktivität (letzte 28 Tage) |
| GET | `/api/physicalinfo/{email}` | Körperdaten |
| GET | `/api/sleep/{email}` | Schlaf (letzte 5 Nächte) |
| GET | `/api/nightlyrecharge/{email}` | Nightly Recharge |
| GET | `/api/exercises/{email}` | Trainings der letzten 30 Tage |
| GET | `/api/exercises/{email}/{exerciseId}` | Trainingsdetails mit Höhenmetern, Verläufen, Pulszonen und Route |
| POST | `/api/polar/disconnect?email=` | User bei Polar abmelden und Token löschen |

Alle Daten-Endpoints liegen im `PolarDataController`: Sie laden den Polar-Token des Users aus der Datenbank und rufen damit über den `PolarClient` die Polar API auf.

Migrationen werden mit `Api` als Startprojekt erstellt, z. B. `dotnet ef migrations add Name --project ORM --startup-project Api`.

## Polar-Konfiguration

Client-ID und Client-Secret stehen **nicht** im Code, sondern in den .NET User-Secrets (pro Rechner, nicht im Repo).
Die Werte findest du unter [admin.polaraccesslink.com](https://admin.polaraccesslink.com).

```bash
dotnet user-secrets set "Polar:ClientId" "<client-id>" --project Api
dotnet user-secrets set "Polar:ClientSecret" "<client-secret>" --project Api
```

Im Polar-Admin muss die Redirect-URL `https://localhost:7262/api/polar/callback` registriert sein.

### OAuth-Ablauf

1. Frontend leitet auf `/api/polar/connect?email=` weiter.
2. Backend leitet auf `https://flow.polar.com/oauth2/authorization` (V3) weiter, die E-Mail steckt Base64Url-kodiert im `state`.
3. Polar ruft nach dem Login `/api/polar/callback` mit `code` auf.
4. Backend tauscht den Code bei `https://polarremote.com/v2/oauth2/token` gegen einen Access-Token, speichert ihn beim User und registriert den User bei AccessLink (`POST /v3/users`).

Alle `HttpClient`s schicken den Header `User-Agent: PolarHealthCompanion/1.0` mit (verlangt Polar, siehe `Program.cs`).

### Warum V3 und nicht V4?

Laut Polar-Support funktionieren V4-Tokens (`auth.polar.com`) **nicht** mit den `/v3`-Datenendpoints, und die V4-Token-Antwort enthält keine `x_user_id`. Deshalb nutzt das Backend den V3-Flow.
