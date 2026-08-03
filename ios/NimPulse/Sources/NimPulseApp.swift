import SwiftUI

@main
struct NimPulseApp: App {
    @StateObject private var auth = AuthService.shared

    var body: some Scene {
        WindowGroup {
            Group {
                if auth.isBootstrapping {
                    ProgressView()
                } else if auth.isLoggedIn {
                    NavigationStack {
                        ContentView()
                    }
                } else {
                    LoginView()
                }
            }
            .task {
                await auth.bootstrap()
            }
        }
    }
}
