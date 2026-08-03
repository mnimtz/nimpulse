import Foundation

struct UploadSamplesRequest: Encodable {
    let samples: [HealthSampleUpload]
}

struct UploadSamplesResponse: Decodable {
    let received: Int
    let inserted: Int
    let updated: Int
}

enum HealthSyncError: LocalizedError {
    case server(status: Int, body: String)

    var errorDescription: String? {
        switch self {
        case .server(let status, let body):
            return "Server antwortete mit \(status): \(body)"
        }
    }
}

@available(iOS 15.0, *)
enum HealthSyncService {
    /// Liest die MVP-HealthKit-Typen der letzten `days` Tage und lädt sie im Batch zur API hoch.
    /// Upload ist ein Upsert nach `externalId` (HealthKit-UUID) — mehrfaches Syncen desselben
    /// Zeitraums erzeugt keine Duplikate.
    static func syncRecentSamples(days: Int = 30) async throws -> UploadSamplesResponse {
        let samples = try await HealthDataReader.readRecentSamples(days: days)

        var request = URLRequest(url: APIConfig.baseURL.appendingPathComponent("api/v1/health/samples"))
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.httpBody = try JSONEncoder().encode(UploadSamplesRequest(samples: samples))

        let (data, response) = try await URLSession.shared.data(for: request)

        guard let httpResponse = response as? HTTPURLResponse, (200..<300).contains(httpResponse.statusCode) else {
            let status = (response as? HTTPURLResponse)?.statusCode ?? -1
            throw HealthSyncError.server(status: status, body: String(data: data, encoding: .utf8) ?? "")
        }

        return try JSONDecoder().decode(UploadSamplesResponse.self, from: data)
    }
}
