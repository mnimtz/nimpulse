import Foundation

/// Spiegelt `ChatMessageView` (Backend) — `role` ist "user" oder "assistant".
struct ChatMessageDto: Codable, Identifiable {
    let role: String
    let content: String
    let createdAt: String

    var id: String { createdAt + role }
    var isUser: Bool { role == "user" }
}

struct ChatRequest: Encodable {
    let message: String
    let provider: String?
}

struct ChatResponse: Decodable {
    let answer: String
    let createdAt: String
}
