import SwiftUI

struct SettingsView: View {
    @ObservedObject private var auth = AuthService.shared

    @State private var aiSettings = AiGatewaySettingsDto(
        defaultProvider: "claude",
        claudeModel: "claude-sonnet-5",
        hasClaudeApiKey: false,
        azureOpenAiDeploymentName: "",
        azureOpenAiEndpoint: "",
        hasAzureOpenAiApiKey: false,
        openAiModel: "",
        hasOpenAiApiKey: false
    )
    @State private var claudeApiKeyInput = ""
    @State private var azureOpenAiApiKeyInput = ""
    @State private var openAiApiKeyInput = ""
    @State private var isLoadingAiSettings = false
    @State private var aiSettingsStatus: String?

    @State private var serverVersion: String?

    @State private var syncWindowSelection = "182"
    @State private var isSavingSyncWindow = false
    @State private var syncWindowStatus: String?

    private static let syncWindowOptions: [(label: String, value: String)] = [
        ("6 Monate", "182"),
        ("1 Jahr", "365"),
        ("2 Jahre", "730"),
        ("3 Jahre", "1095"),
        ("Alles", "all"),
    ]

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

            Section("Sync") {
                Picker("Sync-Zeitraum", selection: $syncWindowSelection) {
                    ForEach(Self.syncWindowOptions, id: \.value) { option in
                        Text(option.label).tag(option.value)
                    }
                }

                Button {
                    Task { await saveSyncWindow() }
                } label: {
                    if isSavingSyncWindow {
                        ProgressView()
                    } else {
                        Text("Speichern")
                    }
                }
                .disabled(isSavingSyncWindow)

                if let syncWindowStatus {
                    Text(syncWindowStatus)
                        .font(.footnote)
                        .foregroundStyle(.secondary)
                }
            }

            // KI-Gateway ist Admin-only — das Backend erzwingt das serverseitig
            // ([Authorize(Roles = "Admin")]), diese Bedingung blendet den Abschnitt nur aus.
            if auth.currentUser?.isAdmin == true {
                Section("KI-Gateway") {
                    Picker("Standard-Provider", selection: $aiSettings.defaultProvider) {
                        Text("Claude").tag("claude")
                        Text("Azure OpenAI").tag("azure-openai")
                        Text("OpenAI").tag("openai")
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

                    TextField("OpenAI-Modell (z. B. gpt-5)", text: $aiSettings.openAiModel)
                        .textInputAutocapitalization(.never)
                        .autocorrectionDisabled()

                    SecureField(
                        aiSettings.hasOpenAiApiKey ? "OpenAI API-Key (gesetzt — leer lassen zum Beibehalten)" : "OpenAI API-Key",
                        text: $openAiApiKeyInput
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
            syncWindowSelection = Self.selectionFor(days: auth.currentUser?.syncWindowDays)
            if auth.currentUser?.isAdmin == true {
                await loadAiSettings()
            }
        }
    }

    /// Bildet einen gespeicherten Tage-Wert auf die nächstgrößere Picker-Option ab — betrifft vor
    /// allem den Alt-Default (30 Tage, aus der Zeit vor diesem Setting), der selbst keine der
    /// fünf Optionen ist. Speichert erst beim expliziten "Speichern"-Tap, ändert also nichts,
    /// bevor der Nutzer das wirklich will.
    private static func selectionFor(days: Int?) -> String {
        guard let days else { return "all" }
        switch days {
        case ...182: return "182"
        case ...365: return "365"
        case ...730: return "730"
        default: return "1095"
        }
    }

    private func saveSyncWindow() async {
        isSavingSyncWindow = true
        defer { isSavingSyncWindow = false }
        do {
            let days = syncWindowSelection == "all" ? nil : Int(syncWindowSelection)
            try await auth.updateSyncWindow(days: days)
            syncWindowStatus = "Gespeichert."
        } catch {
            syncWindowStatus = "Speichern fehlgeschlagen: \(error.localizedDescription)"
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
                azureOpenAiApiKey: azureOpenAiApiKeyInput.isEmpty ? nil : azureOpenAiApiKeyInput,
                openAiModel: aiSettings.openAiModel,
                openAiApiKey: openAiApiKeyInput.isEmpty ? nil : openAiApiKeyInput
            )
            aiSettings = try await AiGatewaySettingsService.update(request)
            claudeApiKeyInput = ""
            azureOpenAiApiKeyInput = ""
            openAiApiKeyInput = ""
            aiSettingsStatus = "Gespeichert."
        } catch {
            aiSettingsStatus = "Speichern fehlgeschlagen: \(error.localizedDescription)"
        }
    }
}

#Preview {
    NavigationStack { SettingsView() }
}
