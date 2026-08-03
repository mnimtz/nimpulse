# NimPulse

![NimPulse](assets/nimpulse-logo-banner.png)

Self-hosted Familien-Gesundheitsplattform: Apple-Health-Sync, Mehrbenutzer, Auswertung und ein KI-Assistent, der die eigenen Gesundheitsdaten erklärt — nicht diagnostiziert. Azure-basiert, .NET 8, iOS-App mit HealthKit.

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fmnimtz%2Fnimpulse%2Fmain%2Finfra%2Fazuredeploy.json)
[![Visualize](https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/visualizebutton.svg)](http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2Fmnimtz%2Fnimpulse%2Fmain%2Finfra%2Fazuredeploy.json)

## Stand

- [x] .NET-8-Solution mit `/api/v1`-Struktur
- [x] AI-Provider-Abstraktion: Claude (Sonnet 5, Anthropic API) + GPT-5.6 (Azure OpenAI Service), Standard admin-konfigurierbar über das KI-Gateway (siehe unten)
- [x] iOS-Grundgerüst (xcodegen, Team KQGPPH4S33, Bundle `email.nimtz.nimpulse`)
- [x] HealthKit: Autorisierungsanfrage für praktisch alle iOS-Gesundheitsdatentypen (siehe [docs/HEALTHKIT.md](docs/HEALTHKIT.md))
- [x] Benutzerverwaltung: Registrierung/Login (JWT), Rollen Admin/Member, erster registrierter Account wird automatisch Admin
- [x] Health-Daten-Sync: iOS liest **alle** Quantity-/Category-HealthKit-Typen und lädt sie userbezogen hoch (SQLite, Upsert nach HealthKit-UUID)
- [x] Reports: Aggregation (Summe/Durchschnitt/Min/Max) pro Typ, Tag/Woche/Monat
- [x] KI-Gateway: Admin wählt Standard-AI-Provider/-Modell zur Laufzeit (Settings-Screen in der App)
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

## KI-Gateway

`GET/PUT /api/v1/settings/ai` (Admin-only) — legt fest, welcher Provider standardmäßig antwortet (`claude` oder `azure-openai`) und welches Modell/Deployment. API-Keys bleiben bewusst in `appsettings`/Umgebungsvariablen (Secrets, nicht über diese Einstellung änderbar). In der iOS-App: Zahnrad-Symbol → Einstellungen (nur für Admins sichtbar).

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

Klick den **Deploy to Azure**-Button oben. Pflichtparameter: `siteName`. AI-Keys (`anthropicApiKey`, `azureOpenAiApiKey`, ...) können beim Deploy gesetzt oder später in der App-Service-Configuration nachgetragen werden. `jwtSigningKey` wird automatisch pro Deployment generiert, wenn leer gelassen.

## Projektstruktur

```
src/NimPulse.Api/     # ASP.NET Core Web-API (/api/v1), Auth/Admin/Health/AI-Gateway-Controller
src/NimPulse.Core/    # Domänenlogik: Users, Auth (JWT), Health (EF Core/SQLite), Ai, Settings
ios/                  # SwiftUI-App, xcodegen-verwaltet (Login, Health-Sync, Settings)
infra/azuredeploy.json # 1-Click ARM-Template
docs/                 # HealthKit-Datenumfang, weitere Notizen
```
