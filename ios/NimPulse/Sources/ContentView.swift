import SwiftUI

struct ContentView: View {
    @State private var status: String = "Noch nicht angefragt."
    @State private var isRequesting = false

    var body: some View {
        VStack(spacing: 20) {
            Text("NimPulse")
                .font(.largeTitle.bold())

            Text(status)
                .font(.callout)
                .foregroundStyle(.secondary)
                .multilineTextAlignment(.center)
                .padding(.horizontal)

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
        }
        .padding()
    }

    private func requestAuthorization() async {
        isRequesting = true
        defer { isRequesting = false }

        do {
            try await HealthKitManager.shared.requestFullReadAuthorization()
            status = "Autorisierung angefragt. iOS zeigt dem Nutzer den vollständigen Freigabe-Dialog."
        } catch {
            status = "Fehlgeschlagen: \(error.localizedDescription)"
        }
    }
}

#Preview {
    ContentView()
}
