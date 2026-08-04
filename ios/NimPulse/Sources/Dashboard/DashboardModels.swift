import Foundation

/// Spiegelt `ScoreComponent` (Backend) — `date`/`bucketStart` bleiben Strings statt Date,
/// da .NETs DateTimeOffset-Rundlaufformat und Swifts Codable-.iso8601-Strategie nicht
/// kompatibel sind (siehe ChatModels.swift).
struct ScoreComponentDto: Codable, Identifiable {
    let type: String
    let value: Double?
    let goal: Double
    let weight: Double
    let contribution: Double?

    var id: String { type }
}

/// Spiegelt `DailyScoreResult` (Backend).
struct DailyScoreDto: Codable {
    let date: String
    let score: Double?
    let components: [ScoreComponentDto]
}

/// Spiegelt `ReportBucket` (Backend).
struct ReportBucketDto: Codable, Identifiable {
    let bucketStart: String
    let count: Int
    let sum: Double
    let average: Double
    let min: Double
    let max: Double

    var id: String { bucketStart }
}

/// Spiegelt `Report` (Backend).
struct ReportDto: Codable {
    let type: String
    let period: String
    let buckets: [ReportBucketDto]
}
