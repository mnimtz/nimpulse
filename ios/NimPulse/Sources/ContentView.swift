import SwiftUI

struct ContentView: View {
    @State private var status: String = "Noch nicht angefragt."
    @State private var lastRequestedAt: Date?
    @State private var isRequesting = false

    @State private var syncStatus: String = "Noch nicht synchronisiert."
    @State private var isSyncing = false

    private static let timeFormatter: DateFormatter = {
        let formatter = DateFormatter()
        formatter.timeStyle = .medium
        return formatter
    }()

    var body: some View {
        ScrollView {
            VStack(spacing: 20) {
                Text("NimPulse")
                    .font(.largeTitle.bold())

                Text(status)
                    .font(.callout)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
                    .padding(.horizontal)

                if let lastRequestedAt {
                    Text("Zuletzt angefragt: \(Self.timeFormatter.string(from: lastRequestedAt))")
                        .font(.caption)
                        .foregroundStyle(.tertiary)
                }

                Button {
                    Task { await requestAuthorization() }
                } label: {
                    if isRequesting {
                        ProgressView()
                    } else {
                        Text("Health-Zugriff anfragen")
                    }
                }
                .buttonStyle(.borderedProminent)
                .disabled(isRequesting)

                // iOS zeigt den Freigabe-Dialog nur einmal pro Typ — bei einem erneuten Tap läuft
                // die Anfrage zwar durch, aber ohne sichtbaren Dialog. Aus Datenschutzgründen
                // verrät HealthKit einer App bei Lesezugriff grundsätzlich nie, was tatsächlich
                // erlaubt wurde; die einzige verlässliche Quelle ist die iOS-Einstellungen-App.
                Text("Einstellungen → Datenschutz & Sicherheit → Health → NimPulse zeigt den tatsächlichen Freigabe-Status — die App selbst kann das bei Lesezugriff nicht zuverlässig auslesen.")
                    .font(.footnote)
                    .foregroundStyle(.tertiary)
                    .multilineTextAlignment(.center)
                    .padding(.horizontal, 32)

                Divider()
                    .padding(.vertical, 8)

                Text(syncStatus)
                    .font(.callout)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
                    .padding(.horizontal)

                Button {
                    Task { await syncNow() }
                } label: {
                    if isSyncing {
                        ProgressView()
                    } else {
                        Text("Jetzt synchronisieren")
                    }
                }
                .buttonStyle(.bordered)
                .disabled(isSyncing)

                Text("Liest Schritte, aktive Kalorien, Gehen/Laufen-Distanz, Herzfrequenz, Ruheherzfrequenz, Gewicht, Größe und Schlaf der letzten 30 Tage und lädt sie zur API hoch (\(APIConfig.baseURL.absoluteString)).")
                    .font(.footnote)
                    .foregroundStyle(.tertiary)
                    .multilineTextAlignment(.center)
                    .padding(.horizontal, 32)
            }
            .padding()
        }
    }

    private func requestAuthorization() async {
        isRequesting = true
        defer { isRequesting = false }

        do {
            try await HealthKitManager.shared.requestFullReadAuthorization()
            status = "Anfrage abgeschlossen. Falls schon einmal beantwortet, zeigt iOS den Dialog nicht erneut — Status siehe unten."
        } catch {
            status = "Fehlgeschlagen: \(error.localizedDescription)"
        }

        lastRequestedAt = Date()
    }

    private func syncNow() async {
        isSyncing = true
        defer { isSyncing = false }

        do {
            let result = try await HealthSyncService.syncRecentSamples()
            syncStatus = "\(result.received) Samples gelesen — \(result.inserted) neu, \(result.updated) aktualisiert."
        } catch {
            syncStatus = "Sync fehlgeschlagen: \(error.localizedDescription)"
        }
    }
}

#Preview {
    ContentView()
}
