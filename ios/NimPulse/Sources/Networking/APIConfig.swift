import Foundation

/// Dev-only config. Kein Login/Account-System bisher (Phase 2), also auch keine per-User-URL —
/// nur die Adresse, unter der `dotnet run --project src/NimPulse.Api` gerade erreichbar ist.
///
/// Lokal auf dem Mac testen, App auf echtem iPhone im selben WLAN:
///   ASPNETCORE_URLS="http://0.0.0.0:5289" dotnet run --project src/NimPulse.Api
/// (0.0.0.0 binden, sonst ist der Server nur vom Mac selbst erreichbar — 127.0.0.1 reicht nicht.)
/// Dann hier die LAN-IP des Macs eintragen (`ipconfig getifaddr en0`).
enum APIConfig {
    static let baseURL = URL(string: "http://192.168.1.155:5289")!
}
