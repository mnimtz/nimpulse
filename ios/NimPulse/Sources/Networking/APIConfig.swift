import Foundation

/// Server-Adresse — nutzerkonfigurierbar über ein Feld auf dem Login-Screen, nicht mehr fest im
/// Code verdrahtet. Default ist die echte Azure-Deployment-URL; lokale Entwicklung (LAN-IP des
/// Macs) überschreibt das Feld temporär und wird per UserDefaults gemerkt.
enum APIConfig {
    private static let userDefaultsKey = "NimPulse.ServerURL"
    static let defaultURLString = "https://nimpulse.azurewebsites.net"

    static var baseURLString: String {
        get { UserDefaults.standard.string(forKey: userDefaultsKey) ?? defaultURLString }
        set {
            let trimmed = newValue.trimmingCharacters(in: .whitespacesAndNewlines)
            UserDefaults.standard.set(trimmed.isEmpty ? nil : trimmed, forKey: userDefaultsKey)
        }
    }

    static var baseURL: URL {
        URL(string: baseURLString) ?? URL(string: defaultURLString)!
    }
}
