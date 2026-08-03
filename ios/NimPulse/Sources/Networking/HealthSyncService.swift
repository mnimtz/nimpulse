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
    /// Liest alle Quantity-/Category-HealthKit-Typen der letzten `days` Tage und lädt sie im
    /// Batch zur API hoch. Upload ist ein Upsert nach `externalId` (HealthKit-UUID) — mehrfaches
    /// Syncen desselben Zeitraums erzeugt keine Duplikate. Samples gehören zum eingeloggten
    /// Nutzer (Bearer-Token).
    static func syncRecentSamples(days: Int = 30) async throws -> UploadSamplesResponse {
        let samples = try await HealthDataReader.readRecentSamples(days: days)
        return try await APIClient.post("api/v1/health/samples", body: UploadSamplesRequest(samples: samples))
    }
}
