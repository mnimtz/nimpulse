import Foundation

/// Spiegelt `NimPulse.Core.Settings.AiGatewaySettings` — nur die Felder, die dieses UI setzt.
/// Extra Felder aus der Server-Antwort (id, updatedAt) werden beim Decodieren einfach ignoriert.
struct AiGatewaySettingsDto: Codable {
    var defaultProvider: String
    var claudeModel: String
    var azureOpenAiDeploymentName: String
}

/// Admin-only — Backend prüft die Rolle server-seitig ([Authorize(Roles = "Admin")]), das hier
/// ist nur der Client-Aufruf.
enum AiGatewaySettingsService {
    static func get() async throws -> AiGatewaySettingsDto {
        try await APIClient.get("api/v1/settings/ai")
    }

    static func update(_ settings: AiGatewaySettingsDto) async throws -> AiGatewaySettingsDto {
        try await APIClient.put("api/v1/settings/ai", body: settings)
    }
}
