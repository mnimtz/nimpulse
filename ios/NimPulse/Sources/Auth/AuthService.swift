import Foundation

@MainActor
final class AuthService: ObservableObject {
    static let shared = AuthService()

    @Published private(set) var currentUser: AuthResponse?
    @Published private(set) var isBootstrapping = true

    private let keychain = KeychainStore(service: "email.nimtz.nimpulse.auth")
    private let tokenKey = "jwt"

    private init() {}

    var token: String? { keychain.read(tokenKey) }

    var isLoggedIn: Bool { currentUser != nil }

    /// Beim App-Start aufrufen: prüft, ob ein gespeicherter Token noch gültig ist.
    func bootstrap() async {
        defer { isBootstrapping = false }

        guard let token else { return }
        do {
            currentUser = try await fetchMe(token: token)
        } catch {
            // Token abgelaufen oder ungültig — sauber ausloggen statt einer App, die endlos lädt.
            logout()
        }
    }

    func register(email: String, password: String, displayName: String) async throws {
        let response: AuthResponse = try await post(
            "api/v1/auth/register",
            body: RegisterRequest(email: email, password: password, displayName: displayName)
        )
        try storeAndSetCurrentUser(response)
    }

    func login(email: String, password: String) async throws {
        let response: AuthResponse = try await post(
            "api/v1/auth/login",
            body: LoginRequest(email: email, password: password)
        )
        try storeAndSetCurrentUser(response)
    }

    func logout() {
        keychain.delete(tokenKey)
        currentUser = nil
    }

    /// Persönliche Sync-Zeitraum-Präferenz — jeder Nutzer setzt seine eigene, kein Admin-only.
    func updateSyncWindow(days: Int?) async throws {
        let response: AuthResponse = try await APIClient.put(
            "api/v1/auth/me/preferences",
            body: UpdatePreferencesRequest(syncWindowDays: days)
        )
        currentUser = response
    }

    private func storeAndSetCurrentUser(_ response: AuthResponse) throws {
        guard let token = response.token else {
            throw AuthError.missingToken
        }
        keychain.write(tokenKey, value: token)
        currentUser = response
    }

    private func fetchMe(token: String) async throws -> AuthResponse {
        var request = URLRequest(url: APIConfig.baseURL.appendingPathComponent("api/v1/auth/me"))
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")

        let (data, response) = try await URLSession.shared.data(for: request)
        guard let http = response as? HTTPURLResponse, (200..<300).contains(http.statusCode) else {
            throw AuthError.server(status: (response as? HTTPURLResponse)?.statusCode ?? -1)
        }
        return try JSONDecoder().decode(AuthResponse.self, from: data)
    }

    private func post<Body: Encodable, Result: Decodable>(_ path: String, body: Body) async throws -> Result {
        var request = URLRequest(url: APIConfig.baseURL.appendingPathComponent(path))
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.httpBody = try JSONEncoder().encode(body)

        let (data, response) = try await URLSession.shared.data(for: request)
        guard let http = response as? HTTPURLResponse else {
            throw AuthError.server(status: -1)
        }
        guard (200..<300).contains(http.statusCode) else {
            throw AuthError.server(status: http.statusCode, message: String(data: data, encoding: .utf8) ?? "")
        }
        return try JSONDecoder().decode(Result.self, from: data)
    }
}
