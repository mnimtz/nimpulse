import SwiftUI

struct SettingsView: View {
    @ObservedObject private var auth = AuthService.shared

    @State private var aiSettings = AiGatewaySettingsDto(
        defaultProvider: "claude",
        claudeModel: "claude-sonnet-5",
        azureOpenAiDeploymentName: ""
    )
    @State private var isLoadingAiSettings = false
    @State private var aiSettingsStatus: String?

    var body: some View {
        Form {
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

                    TextField("Azure-OpenAI-Deployment-Name", text: $aiSettings.azureOpenAiDeploymentName)
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
            if auth.currentUser?.isAdmin == true {
                await loadAiSettings()
            }
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
            aiSettings = try await AiGatewaySettingsService.update(aiSettings)
            aiSettingsStatus = "Gespeichert."
        } catch {
            aiSettingsStatus = "Speichern fehlgeschlagen: \(error.localizedDescription)"
        }
    }
}

#Preview {
    NavigationStack { SettingsView() }
}
