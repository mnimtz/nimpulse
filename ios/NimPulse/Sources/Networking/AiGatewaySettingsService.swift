import Foundation

/// Spiegelt `AiGatewaySettingsView` — GET maskiert die Keys (nur ob einer gesetzt ist, nie der
/// Wert selbst), damit ein bereits gesetzter Key niemals über die Leitung zurückkommt.
/// `updatedAt` wird hier absichtlich nicht dekodiert (ungenutzt im UI, spart den .NET-DateTimeOffset-
/// vs-Swift-Date-Formatabgleich) — überzählige Felder werden beim Decodieren einfach ignoriert.
struct AiGatewaySettingsDto: Codable {
    var defaultProvider: String
    var claudeModel: String
    var hasClaudeApiKey: Bool
    var azureOpenAiDeploymentName: String
    var azureOpenAiEndpoint: String?
    var hasAzureOpenAiApiKey: Bool
}

/// Spiegelt `UpdateAiGatewaySettingsRequest`. Die beiden Key-Felder werden nur gesendet, wenn der
/// Nutzer im UI tatsächlich etwas eingetippt hat — leer/nil lässt den bestehenden Key unangetastet
/// (Backend-Regel, siehe AiSettingsController.Update).
struct UpdateAiGatewaySettingsRequest: Codable {
    var defaultProvider: String
    var claudeModel: String
    var claudeApiKey: String?
    var azureOpenAiDeploymentName: String
    var azureOpenAiEndpoint: String?
    var azureOpenAiApiKey: String?
}

/// Admin-only — Backend prüft die Rolle server-seitig ([Authorize(Roles = "Admin")]), das hier
/// ist nur der Client-Aufruf.
enum AiGatewaySettingsService {
    static func get() async throws -> AiGatewaySettingsDto {
        try await APIClient.get("api/v1/settings/ai")
    }

    static func update(_ request: UpdateAiGatewaySettingsRequest) async throws -> AiGatewaySettingsDto {
        try await APIClient.put("api/v1/settings/ai", body: request)
    }
}
