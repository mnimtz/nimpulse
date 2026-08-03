import Foundation

struct AuthResponse: Codable {
    let token: String?
    let id: UUID
    let email: String
    let displayName: String
    let role: String

    var isAdmin: Bool { role == "Admin" }
}

struct RegisterRequest: Encodable {
    let email: String
    let password: String
    let displayName: String
}

struct LoginRequest: Encodable {
    let email: String
    let password: String
}

enum AuthError: LocalizedError {
    case missingToken
    case server(status: Int, message: String = "")

    var errorDescription: String? {
        switch self {
        case .missingToken:
            return "Server hat keinen Token zurückgegeben."
        case .server(let status, let message):
            return message.isEmpty ? "Fehler \(status)" : "Fehler \(status): \(message)"
        }
    }
}
