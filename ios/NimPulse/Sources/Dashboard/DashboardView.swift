import SwiftUI
import Charts

struct DashboardView: View {
    @State private var selectedDate = Calendar.current.startOfDay(for: Date())
    @State private var score: DailyScoreDto?
    @State private var stepChart: ReportDto?
    @State private var isLoading = true
    @State private var errorMessage: String?
    @State private var pdfExportURL: URL?
    @State private var isExportingPdf = false
    @State private var pdfErrorMessage: String?

    private var isToday: Bool {
        Calendar.current.isDateInToday(selectedDate)
    }

    var body: some View {
        ScrollView {
            VStack(spacing: 20) {
                dayNav

                if isLoading {
                    ProgressView()
                        .padding(.top, 40)
                } else if let errorMessage {
                    Text(errorMessage)
                        .font(.footnote)
                        .foregroundStyle(.red)
                        .padding(.horizontal)
                } else {
                    scoreCard

                    if let stepChart, !stepChart.buckets.isEmpty {
                        stepChartCard(stepChart)
                    }
                }
            }
            .padding()
        }
        .navigationTitle("Dashboard")
        .task(id: selectedDate) {
            await load()
        }
    }

    private var dayNav: some View {
        HStack {
            Button {
                changeDay(by: -1)
            } label: {
                Image(systemName: "chevron.left")
            }

            Spacer()

            Text(dayLabel(for: selectedDate))
                .font(.headline)

            Spacer()

            Button {
                changeDay(by: 1)
            } label: {
                Image(systemName: "chevron.right")
            }
            .disabled(isToday)
        }
    }

    @ViewBuilder
    private var scoreCard: some View {
        if let score {
            VStack(spacing: 8) {
                if let value = score.score {
                    Text("\(Int(value.rounded()))")
                        .font(.system(size: 52, weight: .bold, design: .rounded))
                } else {
                    Text("–")
                        .font(.system(size: 52, weight: .bold, design: .rounded))
                        .opacity(0.6)
                }

                Text("TAGES-SCORE")
                    .font(.caption)
                    .fontWeight(.semibold)
                    .tracking(1)
                    .opacity(0.85)

                HStack(spacing: 16) {
                    ForEach(score.components) { component in
                        VStack(spacing: 2) {
                            Text(Self.componentLabel(component.type))
                                .font(.caption2)
                                .opacity(0.8)
                            Text(component.value.map { "\(Int($0.rounded())) / \(Int(component.goal))" } ?? "keine Daten")
                                .font(.caption)
                                .fontWeight(.bold)
                        }
                    }
                }
                .padding(.top, 4)

                Text("v1-Formel aus Schritten, aktiver Energie und Ruhepuls — ein Startpunkt, keine medizinische Bewertung.")
                    .font(.caption2)
                    .opacity(0.75)
                    .multilineTextAlignment(.center)
                    .padding(.top, 6)
            }
            .foregroundStyle(.white)
            .frame(maxWidth: .infinity)
            .padding(24)
            .background(
                LinearGradient(colors: [Color.accentColor, Color.accentColor.opacity(0.7)], startPoint: .topLeading, endPoint: .bottomTrailing)
            )
            .clipShape(RoundedRectangle(cornerRadius: 20))
        }
    }

    private func stepChartCard(_ report: ReportDto) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Schritte — 7 Tage bis \(shortDate(selectedDate))")
                .font(.headline)

            Chart(report.buckets) { bucket in
                BarMark(
                    x: .value("Tag", dayLabel(forBucketStart: bucket.bucketStart)),
                    y: .value("Schritte", bucket.sum)
                )
                .foregroundStyle(Color.accentColor)
            }
            .frame(height: 160)

            pdfExportControl

            if let pdfErrorMessage {
                Text(pdfErrorMessage)
                    .font(.caption2)
                    .foregroundStyle(.red)
            }
        }
        .padding()
        .background(Color(.secondarySystemGroupedBackground))
        .clipShape(RoundedRectangle(cornerRadius: 16))
    }

    @ViewBuilder
    private var pdfExportControl: some View {
        if let pdfExportURL {
            ShareLink(item: pdfExportURL) {
                Label("PDF exportieren", systemImage: "square.and.arrow.up")
            }
            .font(.footnote)
        } else {
            Button {
                Task { await exportPdf() }
            } label: {
                if isExportingPdf {
                    ProgressView()
                } else {
                    Label("PDF exportieren", systemImage: "square.and.arrow.up")
                }
            }
            .font(.footnote)
            .disabled(isExportingPdf)
        }
    }

    private func changeDay(by days: Int) {
        guard let newDate = Calendar.current.date(byAdding: .day, value: days, to: selectedDate) else { return }
        selectedDate = min(newDate, Calendar.current.startOfDay(for: Date()))
    }

    private func load() async {
        isLoading = true
        errorMessage = nil
        pdfExportURL = nil
        pdfErrorMessage = nil
        do {
            async let scoreResult = DashboardService.getScore(date: selectedDate)
            async let chartResult = DashboardService.getStepChart(date: selectedDate)
            score = try await scoreResult
            stepChart = try await chartResult
        } catch {
            errorMessage = "Laden fehlgeschlagen: \(error.localizedDescription)"
        }
        isLoading = false
    }

    private func exportPdf() async {
        isExportingPdf = true
        pdfErrorMessage = nil
        do {
            let data = try await ReportsService.getReportPdf(type: "stepCount", days: 7, date: selectedDate)
            let url = FileManager.default.temporaryDirectory.appendingPathComponent("nimpulse-schritte-\(shortDate(selectedDate)).pdf")
            try data.write(to: url, options: .atomic)
            pdfExportURL = url
        } catch {
            pdfErrorMessage = "PDF-Export fehlgeschlagen: \(error.localizedDescription)"
        }
        isExportingPdf = false
    }

    private func dayLabel(for date: Date) -> String {
        let calendar = Calendar.current
        if calendar.isDateInToday(date) {
            return "Heute"
        }
        if calendar.isDateInYesterday(date) {
            return "Gestern"
        }
        let formatter = DateFormatter()
        formatter.dateFormat = "EEEE, dd.MM.yyyy"
        formatter.locale = Locale(identifier: "de_DE")
        return formatter.string(from: date)
    }

    private func shortDate(_ date: Date) -> String {
        let formatter = DateFormatter()
        formatter.dateFormat = "dd.MM."
        return formatter.string(from: date)
    }

    // .NETs DateTimeOffset-JSON-Format ("...T00:00:00.0000000+02:00") ist mit Swifts
    // ISO8601DateFormatter nicht zuverlässig kompatibel (Bruchteilssekunden-Präzision) — statt
    // zu parsen wird einfach das Datumspräfix (erste 10 Zeichen, "yyyy-MM-dd") gelesen.
    private func dayLabel(forBucketStart bucketStart: String) -> String {
        let dateOnly = String(bucketStart.prefix(10))
        guard let date = Self.isoDayFormatter.date(from: dateOnly) else { return dateOnly }
        return Self.weekdayFormatter.string(from: date)
    }

    private static let isoDayFormatter: DateFormatter = {
        let formatter = DateFormatter()
        formatter.dateFormat = "yyyy-MM-dd"
        formatter.calendar = Calendar(identifier: .gregorian)
        formatter.timeZone = .current
        return formatter
    }()

    private static let weekdayFormatter: DateFormatter = {
        let formatter = DateFormatter()
        formatter.dateFormat = "EEE"
        formatter.locale = Locale(identifier: "de_DE")
        return formatter
    }()

    // Nur die drei Score-Komponenten-Typen — keine vollständige Katalog-Portierung nötig, da
    // DashboardView ausschließlich diese anzeigt (siehe DailyScoreService.cs, Backend).
    private static func componentLabel(_ type: String) -> String {
        switch type {
        case "stepCount": return "Schritte"
        case "activeEnergyBurned": return "Energie"
        case "restingHeartRate": return "Ruhepuls"
        default: return type
        }
    }
}

#Preview {
    NavigationStack { DashboardView() }
}
