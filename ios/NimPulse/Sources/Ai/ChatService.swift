import Foundation

enum ChatService {
    static func getHistory() async throws -> [ChatMessageDto] {
        try await APIClient.get("api/v1/ai/chat/history")
    }

    static func sendMessage(_ message: String) async throws -> ChatResponse {
        try await APIClient.post("api/v1/ai/chat", body: ChatRequest(message: message, provider: nil))
    }
}
