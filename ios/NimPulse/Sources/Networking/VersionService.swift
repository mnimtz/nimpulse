import Foundation

struct VersionResponse: Decodable {
    let version: String
}

/// Unauthentifiziert (kein Login nötig) — sonst könnte man den laufenden Server-Stand nicht
/// prüfen, ohne sich vorher einzuloggen.
enum VersionService {
    static func get() async throws -> VersionResponse {
        let url = APIConfig.baseURL.appendingPathComponent("api/v1/version")
        let (data, response) = try await URLSession.shared.data(from: url)
        guard let http = response as? HTTPURLResponse, (200..<300).contains(http.statusCode) else {
            throw APIError.server(status: (response as? HTTPURLResponse)?.statusCode ?? -1, body: "")
        }
        return try JSONDecoder().decode(VersionResponse.self, from: data)
    }
}
