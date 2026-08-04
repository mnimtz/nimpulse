import Foundation

struct UploadSamplesRequest: Encodable {
    let samples: [HealthSampleUpload]
}

struct UploadSamplesResponse: Decodable {
    let received: Int
    let inserted: Int
    let updated: Int
}

@available(iOS 15.0, *)
enum HealthSyncService {
    /// Liest alle Quantity-/Category-HealthKit-Typen im persönlichen Sync-Zeitraum des
    /// eingeloggten Nutzers (siehe SettingsView, `nil` = "Alles") und lädt sie im Batch zur API
    /// hoch. Upload ist ein Upsert nach `externalId` (HealthKit-UUID) — mehrfaches Syncen
    /// desselben Zeitraums erzeugt keine Duplikate.
    static func syncRecentSamples() async throws -> UploadSamplesResponse {
        // `currentUser?.syncWindowDays` mit `??` zusammenzufassen würde "Alles" (nil-Wert INNERHALB
        // eines vorhandenen currentUser) nicht von "gar kein currentUser" unterscheiden können —
        // deshalb explizit in zwei Schritten statt eines einzelnen Optional-Chains.
        let days: Int?
        if let user = await AuthService.shared.currentUser {
            days = user.syncWindowDays
        } else {
            days = 30
        }

        let samples = try await HealthDataReader.readRecentSamples(days: days)
        return try await APIClient.post("api/v1/health/samples", body: UploadSamplesRequest(samples: samples))
    }
}
