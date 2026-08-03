import SwiftUI

struct LoginView: View {
    @ObservedObject private var auth = AuthService.shared

    @State private var email = ""
    @State private var password = ""
    @State private var displayName = ""
    @State private var isRegistering = false
    @State private var isSubmitting = false
    @State private var errorMessage: String?

    var body: some View {
        VStack(spacing: 16) {
            Text("NimPulse")
                .font(.largeTitle.bold())

            Picker("Modus", selection: $isRegistering) {
                Text("Anmelden").tag(false)
                Text("Registrieren").tag(true)
            }
            .pickerStyle(.segmented)

            if isRegistering {
                TextField("Name", text: $displayName)
                    .textContentType(.name)
                    .textFieldStyle(.roundedBorder)
            }

            TextField("E-Mail", text: $email)
                .textContentType(.emailAddress)
                .keyboardType(.emailAddress)
                .textInputAutocapitalization(.never)
                .autocorrectionDisabled()
                .textFieldStyle(.roundedBorder)

            SecureField("Passwort", text: $password)
                .textContentType(isRegistering ? .newPassword : .password)
                .textFieldStyle(.roundedBorder)

            if let errorMessage {
                Text(errorMessage)
                    .font(.footnote)
                    .foregroundStyle(.red)
                    .multilineTextAlignment(.center)
            }

            Button {
                Task { await submit() }
            } label: {
                if isSubmitting {
                    ProgressView()
                } else {
                    Text(isRegistering ? "Konto erstellen" : "Anmelden")
                        .frame(maxWidth: .infinity)
                }
            }
            .buttonStyle(.borderedProminent)
            .disabled(isSubmitting || email.isEmpty || password.isEmpty)

            if isRegistering {
                Text("Das erste registrierte Konto wird automatisch Admin.")
                    .font(.footnote)
                    .foregroundStyle(.tertiary)
                    .multilineTextAlignment(.center)
            }
        }
        .padding(32)
    }

    private func submit() async {
        isSubmitting = true
        errorMessage = nil
        defer { isSubmitting = false }

        do {
            if isRegistering {
                try await auth.register(email: email, password: password, displayName: displayName)
            } else {
                try await auth.login(email: email, password: password)
            }
        } catch {
            errorMessage = error.localizedDescription
        }
    }
}

#Preview {
    LoginView()
}
