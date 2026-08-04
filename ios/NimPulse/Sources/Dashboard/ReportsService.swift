import Foundation

enum ReportsService {
    static func getReportPdf(type: String, period: String = "Day", days: Int, date: Date) async throws -> Data {
        let formatter = DateFormatter()
        formatter.dateFormat = "yyyy-MM-dd"
        formatter.timeZone = .current
        let dateString = formatter.string(from: date)

        return try await APIClient.getData("api/v1/health/reports/pdf?type=\(type)&period=\(period)&days=\(days)&date=\(dateString)")
    }
}
