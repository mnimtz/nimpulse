# NimPulse

![NimPulse](assets/nimpulse-logo-banner.png)

Self-hosted Familien-Gesundheitsplattform: Apple-Health-Sync, Mehrbenutzer, Auswertung und ein KI-Assistent, der die eigenen Gesundheitsdaten erklärt — nicht diagnostiziert. Azure-basiert, .NET 8, iOS-App mit HealthKit.

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fmnimtz%2Fnimpulse%2Fmain%2Finfra%2Fazuredeploy.json)
[![Visualize](https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/visualizebutton.svg)](http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2Fmnimtz%2Fnimpulse%2Fmain%2Finfra%2Fazuredeploy.json)

## Stand

- [x] .NET-8-Solution mit `/api/v1`-Struktur
- [x] Web-Dashboard im selben Prozess (Blazor Server, static rendering) — Login/Registrierung, Health-Übersicht, Berichte, Benutzerverwaltung, KI-Gateway, alles auch im Browser nutzbar (siehe unten)
- [x] AI-Provider-Abstraktion: Claude (Sonnet 5, Anthropic API) + GPT-5.6 (Azure OpenAI Service), Standard admin-konfigurierbar über das KI-Gateway (siehe unten)
- [x] iOS-Grundgerüst (xcodegen, Team KQGPPH4S33, Bundle `email.nimtz.nimpulse`)
- [x] HealthKit: Autorisierungsanfrage für praktisch alle iOS-Gesundheitsdatentypen (siehe [docs/HEALTHKIT.md](docs/HEALTHKIT.md))
- [x] Benutzerverwaltung: Registrierung/Login (JWT für die API, Cookie fürs Web-Dashboard — dasselbe Backend), Rollen Admin/Member, erster registrierter Account wird automatisch Admin
- [x] Health-Daten-Sync: iOS liest **alle** Quantity-/Category-HealthKit-Typen und lädt sie userbezogen hoch (SQLite, Upsert nach HealthKit-UUID)
- [x] Reports: Aggregation (Summe/Durchschnitt/Min/Max) pro Typ, Tag/Woche/Monat
- [x] KI-Gateway: Admin wählt Standard-AI-Provider/-Modell/-Keys zur Laufzeit (Settings-Screen in App und Web-Dashboard) — keine Keys mehr im 1-Click-Deploy-Formular
- [x] KI-Coach: fortlaufender Chat mit Gesundheitsdaten-Kontext, `/coach` (Web) + Chat-Screen (iOS)
- [x] EF Core Migrations statt `EnsureCreated()` (Baseline-Bootstrap für Bestandsdaten)
- [x] 1-Click-Azure-Deploy-Template (App Service + Azure Files/SQLite + Blob für Berichte)
- [ ] PDF-Export der Reports (Phase 3)
- [ ] Invite-Links für Familienmitglieder statt Admin-legt-direkt-an (später, falls gewünscht)

Sprachumfang: **Deutsch + Englisch** (bewusst kein EFIGS+NL für dieses Projekt).

## Benutzerverwaltung

- `POST /api/v1/auth/register` — offene Selbstregistrierung; der erste Account im System wird automatisch **Admin**, alle danach **Member**.
- `POST /api/v1/auth/login` — E-Mail/Passwort, liefert ein JWT (Gültigkeit konfigurierbar über `Auth:TokenLifetimeHours`, Standard 30 Tage).
- `GET /api/v1/auth/me` — aktueller Nutzer (`[Authorize]`).
- `GET/POST/DELETE /api/v1/admin/users` — Admin-only: Familienmitglieder direkt anlegen (mit Initial-Passwort) statt auf Selbstregistrierung angewiesen zu sein.

Alle `/api/v1/health/*`- und `/api/v1/ai/*`-Endpoints brauchen ein `Authorization: Bearer <token>`-Header. Passwort-Hashing über `PasswordHasher<User>` (ASP.NET Core Identity Core, ohne den vollen Identity-Unterbau). Kein E-Mail-Versand/Passwort-Reset bisher — kein E-Mail-Gateway vorhanden.

## Web-Dashboard

Läuft im selben Prozess/Container wie die API (Blazor Server, static server rendering — kein SignalR-Circuit, klassische Formular-Posts/Redirects reichen für dieses Admin-/Family-Scale-UI). Kein separates Hosting/Deploy nötig.

| Route | Zugriff | Zweck |
|---|---|---|
| `/login`, `/register` | Öffentlich | Anmeldung/Erstanlage. `/login` leitet automatisch zu `/register` weiter, solange noch kein Benutzer existiert. |
| `/` | Angemeldet | Dashboard — Anzahl + letzter Wert pro Health-Datentyp. |
| `/reports` | Angemeldet | Dieselbe Aggregation wie `GET /api/v1/health/reports`, als Tabelle mit Typ-/Zeitraum-Filter. |
| `/admin` | Admin | Benutzerliste, neue Benutzer anlegen, löschen. |
| `/settings` | Admin | KI-Gateway-Konfiguration (siehe unten). |

Auth: Cookie fürs Web-Dashboard, Bearer/JWT für die iOS-App und andere API-Clients — beide Schemes laufen nebeneinander (`Program.cs`, "Smart"-Policy-Scheme wählt anhand des `Authorization`-Headers), teilen sich dieselben Claims/Rollen.

## KI-Gateway

`GET/PUT /api/v1/settings/ai` (Admin-only), auch als Formular unter `/settings` — legt fest, welcher Provider standardmäßig antwortet (`claude`, `azure-openai` oder `openai`), welches Modell/Deployment, und die API-Keys selbst. Drei Provider zur Wahl: Claude (Anthropic), Azure OpenAI (eigene Azure-Subscription, deployment-name-basiert) und OpenAI direkt (eigener OpenAI-Account, freies Modell-Textfeld z. B. `gpt-5` — kein Azure-Resource nötig). Alles DB-backed (SQLite, `AiGatewaySettings`-Tabelle) statt `appsettings`/Umgebungsvariablen — der 1-Click-Deploy fragt keine AI-Keys mehr ab, ein Admin setzt sie einmalig nach dem ersten Login. GET maskiert die Keys (nur `hasClaudeApiKey`/`hasAzureOpenAiApiKey`/`hasOpenAiApiKey`, nie der Wert selbst); ein leeres Key-Feld beim Speichern lässt den bestehenden Key unangetastet. In der iOS-App: Zahnrad-Symbol → Einstellungen (nur für Admins sichtbar).

## Reports

`GET /api/v1/health/reports?type=stepCount&period=day|week|month&days=30` — aggregiert Quantity-Samples in Zeit-Buckets (Anzahl, Summe, Durchschnitt, Min, Max). Dieselbe Basis trägt Tages-, Wochen- und Monatsübersichten; ein PDF-Export darauf ist ein späterer Schritt.

## Lokal entwickeln

### API

```bash
dotnet build
ASPNETCORE_URLS="http://0.0.0.0:5289" dotnet run --project src/NimPulse.Api
```

`0.0.0.0` binden (nicht `127.0.0.1`), sonst erreicht ein Gerät im selben WLAN (z. B. das iPhone beim Testen) den Server nicht.

AI-Keys lokal setzen, statt sie in `appsettings.json` einzutragen:

```bash
export Ai__Claude__ApiKey="sk-ant-..."
export Ai__AzureOpenAi__Endpoint="https://<resource>.openai.azure.com/"
export Ai__AzureOpenAi__ApiKey="..."
export Ai__AzureOpenAi__DeploymentName="<deployment-name>"
```

`Auth__JwtSigningKey` unbedingt für echte Deployments setzen — der appsettings-Default ist absichtlich ein erkennbar unsicherer Platzhalter.

### iOS

```bash
cd ios
xcodegen generate
open NimPulse.xcodeproj
```

Nach jeder neuen `.swift`-Datei erneut `xcodegen generate` ausführen. `Sources/Networking/APIConfig.swift` zeigt auf die LAN-IP des Rechners, auf dem die API läuft (`ipconfig getifaddr en0`) — bei Netzwerkwechsel anpassen.

## 1-Click Deploy

Klick den **Deploy to Azure**-Button oben. Pflichtparameter: `siteName`. AI-Provider/-Keys werden **nicht** beim Deploy abgefragt — nach dem ersten Login unter `/settings` (Web) oder Einstellungen (iOS) setzen. `jwtSigningKey` wird automatisch pro Deployment generiert, wenn leer gelassen.

## Bekannte Einschränkung: SQLite + DateTimeOffset

Der EF-Core-SQLite-Provider übersetzt `Where`/`OrderBy`/`Max`/`Min` auf `DateTimeOffset`-Spalten (z. B. `HealthSample.StartDate`) server-seitig **nicht zuverlässig** — teils mit `NotSupportedException`, teils mit `InvalidOperationException: ... could not be translated`, sobald ein Vergleich mit einem weiteren Prädikat kombiniert wird. Betroffene Stellen (`ReportService`, `HealthController.GetSamples`, `AdminUsersController.List`) filtern/sortieren deshalb bewusst erst nach `ToListAsync()` clientseitig. Bei neuen Queries auf `StartDate`/`CreatedAt`/`SyncedAt` denselben Zweischritt verwenden — direkt in der DB-Query vergleichen/sortieren bricht.

## Schema-Änderungen: EF Core Migrations

Seit v0.7.0 läuft das Schema über echte `dotnet ef migrations` (`src/NimPulse.Core/Migrations/`) statt `Database.EnsureCreated()` + manuellem `ALTER TABLE` (das hatte zuvor schon einmal einen echten Vorfall verursacht: nachträglich hinzugefügte `AiGatewaySettings`-Spalten fehlten auf dem laufenden Azure-Deployment). Beim Start entscheidet `BootstrapDatabase` in `Program.cs`: existieren bereits Tabellen, aber keine `__EFMigrationsHistory` (= eine alte `EnsureCreated()`-DB), wird die Baseline-Migration `20260804104900_InitialBaseline` als "bereits angewendet" markiert, ohne ihre `CREATE TABLE`s erneut auszuführen — danach läuft `Database.Migrate()` normal für alles Weitere. Bei einer komplett neuen DB läuft die Baseline-Migration regulär durch. Jede künftige Schema-Änderung ist ab jetzt eine echte Migration, kein manueller `ALTER TABLE`-Block mehr.

## KI-Coach

`/coach` (Web) bzw. das Sprechblasen-Symbol oben links in der iOS-App — ein fortlaufender Chat pro Nutzer (`ChatMessages`-Tabelle), nicht nur ein zustandsloser Einzel-Request. Vor jeder Antwort baut `ChatCoachService` (`src/NimPulse.Core/Ai/ChatCoachService.cs`) automatisch einen Kontext aus den eigenen Gesundheitsdaten der letzten 7 Tage (Schritte, aktive Energie, Herzfrequenz, Ruhepuls, Körpergewicht) in den System-Prompt ein, damit der gewählte AI-Provider (Claude/Azure OpenAI/OpenAI, siehe KI-Gateway oben) über echte Trends spricht statt nur auf getippten Text zu reagieren. `GET /api/v1/ai/chat/history` + `POST /api/v1/ai/chat` bedienen iOS und andere API-Clients; `Coach.razor` im Web ruft denselben `ChatCoachService` direkt auf (keine Zwischen-HTTP-Runde). `/coach` ist aktuell die einzige Seite mit `@rendermode InteractiveServer` — alle anderen Seiten bleiben bewusst Static SSR.

## Projektstruktur

```
src/NimPulse.Api/     # ASP.NET Core Web-API (/api/v1), Auth/Admin/Health/AI-Gateway-Controller
src/NimPulse.Core/    # Domänenlogik: Users, Auth (JWT), Health (EF Core/SQLite), Ai, Settings
ios/                  # SwiftUI-App, xcodegen-verwaltet (Login, Health-Sync, Settings)
infra/azuredeploy.json # 1-Click ARM-Template
docs/                 # HealthKit-Datenumfang, weitere Notizen
```
