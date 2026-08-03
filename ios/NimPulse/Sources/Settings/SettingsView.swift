import SwiftUI

struct SettingsView: View {
    @ObservedObject private var auth = AuthService.shared

    @State private var aiSettings = AiGatewaySettingsDto(
        defaultProvider: "claude",
        claudeModel: "claude-sonnet-5",
        hasClaudeApiKey: false,
        azureOpenAiDeploymentName: "",
        azureOpenAiEndpoint: "",
        hasAzureOpenAiApiKey: false
    )
    @State private var claudeApiKeyInput = ""
    @State private var azureOpenAiApiKeyInput = ""
    @State private var isLoadingAiSettings = false
    @State private var aiSettingsStatus: String?

    @State private var serverVersion: String?

    private var appVersion: String {
        let short = Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "?"
        let build = Bundle.main.infoDictionary?["CFBundleVersion"] as? String ?? "?"
        return "\(short) (\(build))"
    }

    var body: some View {
        Form {
            Section("Version") {
                LabeledContent("App", value: appVersion)
                LabeledContent("Server", value: serverVersion ?? "wird geladen…")
            }

            if let user = auth.currentUser {
                Section("Konto") {
                    LabeledContent("Name", value: user.displayName)
                    LabeledContent("E-Mail", value: user.email)
                    LabeledContent("Rolle", value: user.role)
                }
            }

            // KI-Gateway ist Admin-only — das Backend erzwingt das serverseitig
            // ([Authorize(Roles = "Admin")]), diese Bedingung blendet den Abschnitt nur aus.
            if auth.currentUser?.isAdmin == true {
                Section("KI-Gateway") {
                    Picker("Standard-Provider", selection: $aiSettings.defaultProvider) {
                        Text("Claude").tag("claude")
                        Text("Azure OpenAI").tag("azure-openai")
                    }

                    TextField("Claude-Modell", text: $aiSettings.claudeModel)
                        .textInputAutocapitalization(.never)
                        .autocorrectionDisabled()

                    SecureField(
                        aiSettings.hasClaudeApiKey ? "Claude API-Key (gesetzt — leer lassen zum Beibehalten)" : "Claude API-Key",
                        text: $claudeApiKeyInput
                    )
                    .textInputAutocapitalization(.never)
                    .autocorrectionDisabled()

                    TextField(
                        "Azure-OpenAI-Endpoint",
                        text: Binding(
                            get: { aiSettings.azureOpenAiEndpoint ?? "" },
                            set: { aiSettings.azureOpenAiEndpoint = $0 }
                        )
                    )
                    .textInputAutocapitalization(.never)
                    .autocorrectionDisabled()
                    .keyboardType(.URL)

                    TextField("Azure-OpenAI-Deployment-Name", text: $aiSettings.azureOpenAiDeploymentName)
                        .textInputAutocapitalization(.never)
                        .autocorrectionDisabled()

                    SecureField(
                        aiSettings.hasAzureOpenAiApiKey ? "Azure OpenAI API-Key (gesetzt — leer lassen zum Beibehalten)" : "Azure OpenAI API-Key",
                        text: $azureOpenAiApiKeyInput
                    )
                    .textInputAutocapitalization(.never)
                    .autocorrectionDisabled()

                    Button {
                        Task { await saveAiSettings() }
                    } label: {
                        if isLoadingAiSettings {
                            ProgressView()
                        } else {
                            Text("Speichern")
                        }
                    }
                    .disabled(isLoadingAiSettings)

                    if let aiSettingsStatus {
                        Text(aiSettingsStatus)
                            .font(.footnote)
                            .foregroundStyle(.secondary)
                    }
                }
            }

            Section {
                Button("Abmelden", role: .destructive) {
                    auth.logout()
                }
            }
        }
        .navigationTitle("Einstellungen")
        .task {
            await loadServerVersion()
            if auth.currentUser?.isAdmin == true {
                await loadAiSettings()
            }
        }
    }

    private func loadServerVersion() async {
        do {
            serverVersion = try await VersionService.get().version
        } catch {
            serverVersion = "nicht erreichbar"
        }
    }

    private func loadAiSettings() async {
        isLoadingAiSettings = true
        defer { isLoadingAiSettings = false }
        do {
            aiSettings = try await AiGatewaySettingsService.get()
        } catch {
            aiSettingsStatus = "Laden fehlgeschlagen: \(error.localizedDescription)"
        }
    }

    private func saveAiSettings() async {
        isLoadingAiSettings = true
        defer { isLoadingAiSettings = false }
        do {
            let request = UpdateAiGatewaySettingsRequest(
                defaultProvider: aiSettings.defaultProvider,
                claudeModel: aiSettings.claudeModel,
                claudeApiKey: claudeApiKeyInput.isEmpty ? nil : claudeApiKeyInput,
                azureOpenAiDeploymentName: aiSettings.azureOpenAiDeploymentName,
                azureOpenAiEndpoint: aiSettings.azureOpenAiEndpoint,
                azureOpenAiApiKey: azureOpenAiApiKeyInput.isEmpty ? nil : azureOpenAiApiKeyInput
            )
            aiSettings = try await AiGatewaySettingsService.update(request)
            claudeApiKeyInput = ""
            azureOpenAiApiKeyInput = ""
            aiSettingsStatus = "Gespeichert."
        } catch {
            aiSettingsStatus = "Speichern fehlgeschlagen: \(error.localizedDescription)"
        }
    }
}

#Preview {
    NavigationStack { SettingsView() }
}
