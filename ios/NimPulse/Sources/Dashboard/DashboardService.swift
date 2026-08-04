import Foundation

enum DashboardService {
    static func getScore(date: Date) async throws -> DailyScoreDto {
        try await APIClient.get("api/v1/health/score?date=\(isoDate(date))")
    }

    static func getStepChart(date: Date, daysBack: Int = 7) async throws -> ReportDto {
        try await APIClient.get("api/v1/health/reports?type=stepCount&period=Day&days=\(daysBack)&date=\(isoDate(date))")
    }

    private static func isoDate(_ date: Date) -> String {
        let formatter = DateFormatter()
        formatter.dateFormat = "yyyy-MM-dd"
        formatter.calendar = Calendar(identifier: .gregorian)
        formatter.timeZone = .current
        return formatter.string(from: date)
    }
}
