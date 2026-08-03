import Foundation

enum APIError: LocalizedError {
    case notAuthenticated
    case server(status: Int, body: String)

    var errorDescription: String? {
        switch self {
        case .notAuthenticated:
            return "Nicht angemeldet."
        case .server(let status, let body):
            return body.isEmpty ? "Server antwortete mit \(status)." : "Server antwortete mit \(status): \(body)"
        }
    }
}

/// Kleiner gemeinsamer Unterbau für authentifizierte Requests — Bearer-Token anhängen,
/// Statuscode/Fehlerbody einheitlich prüfen, JSON de-/encodieren.
enum APIClient {
    static func authorizedRequest(path: String, method: String = "GET") async throws -> URLRequest {
        guard let token = await AuthService.shared.token else {
            throw APIError.notAuthenticated
        }
        var request = URLRequest(url: APIConfig.baseURL.appendingPathComponent(path))
        request.httpMethod = method
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        return request
    }

    static func get<Result: Decodable>(_ path: String) async throws -> Result {
        let request = try await authorizedRequest(path: path)
        return try await send(request)
    }

    static func put<Body: Encodable, Result: Decodable>(_ path: String, body: Body) async throws -> Result {
        var request = try await authorizedRequest(path: path, method: "PUT")
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.httpBody = try JSONEncoder().encode(body)
        return try await send(request)
    }

    static func post<Body: Encodable, Result: Decodable>(_ path: String, body: Body) async throws -> Result {
        var request = try await authorizedRequest(path: path, method: "POST")
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.httpBody = try JSONEncoder().encode(body)
        return try await send(request)
    }

    static func send<Result: Decodable>(_ request: URLRequest) async throws -> Result {
        let (data, response) = try await URLSession.shared.data(for: request)
        guard let http = response as? HTTPURLResponse, (200..<300).contains(http.statusCode) else {
            let status = (response as? HTTPURLResponse)?.statusCode ?? -1
            throw APIError.server(status: status, body: String(data: data, encoding: .utf8) ?? "")
        }
        return try JSONDecoder().decode(Result.self, from: data)
    }
}
