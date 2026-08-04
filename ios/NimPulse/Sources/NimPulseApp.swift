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
                        TabView {
                            DashboardView()
                                .tabItem { Label("Dashboard", systemImage: "chart.bar.fill") }
                            ContentView()
                                .tabItem { Label("Sync", systemImage: "arrow.triangle.2.circlepath") }
                        }
                        .toolbar {
                            ToolbarItem(placement: .topBarLeading) {
                                NavigationLink {
                                    ChatView()
                                } label: {
                                    Image(systemName: "bubble.left.and.bubble.right")
                                }
                            }
                            ToolbarItem(placement: .topBarTrailing) {
                                NavigationLink {
                                    SettingsView()
                                } label: {
                                    Image(systemName: "gearshape")
                                }
                            }
                        }
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
