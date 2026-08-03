import HealthKit

/// One HealthKit sample ready to upload — matches `HealthSampleDto` on the API side field for
/// field (Unix-Millisekunden statt Date, siehe Kommentar dort).
struct HealthSampleUpload: Encodable {
    let externalId: String
    let type: String
    let kind: String
    let value: Double?
    let unit: String?
    let categoryValue: Int?
    let startDateUnixMs: Int64
    let endDateUnixMs: Int64
    let sourceName: String?
}

/// Liest die MVP-Datentypen aus Phase 1 (Aktivität, Vitalwerte, Schlaf, Körpermaße) für einen
/// Rückblick-Zeitraum. Bewusst eine kleine, kuratierte Teilmenge der ~180 Typen, die
/// HealthKitManager für die Autorisierung anfragt — die Freigabe ist breit, der Sync fängt
/// klein an und wächst mit den echten Auswertungs-Features.
@available(iOS 15.0, *)
enum HealthDataReader {
    private struct QuantitySpec {
        let identifier: HKQuantityTypeIdentifier
        let typeName: String
        let unit: HKUnit
    }

    private static let quantitySpecs: [QuantitySpec] = [
        QuantitySpec(identifier: .stepCount, typeName: "stepCount", unit: .count()),
        QuantitySpec(identifier: .activeEnergyBurned, typeName: "activeEnergyBurned", unit: .kilocalorie()),
        QuantitySpec(identifier: .distanceWalkingRunning, typeName: "distanceWalkingRunning", unit: .meter()),
        QuantitySpec(identifier: .heartRate, typeName: "heartRate", unit: HKUnit.count().unitDivided(by: .minute())),
        QuantitySpec(identifier: .restingHeartRate, typeName: "restingHeartRate", unit: HKUnit.count().unitDivided(by: .minute())),
        QuantitySpec(identifier: .bodyMass, typeName: "bodyMass", unit: .gramUnit(with: .kilo)),
        QuantitySpec(identifier: .height, typeName: "height", unit: .meterUnit(with: .centi)),
    ]

    private static let categorySpecs: [(identifier: HKCategoryTypeIdentifier, typeName: String)] = [
        (.sleepAnalysis, "sleepAnalysis"),
    ]

    static func readRecentSamples(days: Int = 30) async throws -> [HealthSampleUpload] {
        let store = HKHealthStore()
        let start = Calendar.current.date(byAdding: .day, value: -days, to: Date()) ?? Date()
        let predicate = HKQuery.predicateForSamples(withStart: start, end: Date())

        var uploads: [HealthSampleUpload] = []

        for spec in quantitySpecs {
            guard let type = HKObjectType.quantityType(forIdentifier: spec.identifier) else { continue }
            let descriptor = HKSampleQueryDescriptor<HKQuantitySample>(
                predicates: [.quantitySample(type: type, predicate: predicate)],
                sortDescriptors: [SortDescriptor(\.startDate, order: .reverse)],
                limit: 500
            )
            let samples = try await descriptor.result(for: store)
            uploads += samples.map { sample in
                HealthSampleUpload(
                    externalId: sample.uuid.uuidString,
                    type: spec.typeName,
                    kind: "quantity",
                    value: sample.quantity.doubleValue(for: spec.unit),
                    unit: spec.unit.unitString,
                    categoryValue: nil,
                    startDateUnixMs: sample.startDate.unixMilliseconds,
                    endDateUnixMs: sample.endDate.unixMilliseconds,
                    sourceName: sample.sourceRevision.source.name
                )
            }
        }

        for spec in categorySpecs {
            guard let type = HKObjectType.categoryType(forIdentifier: spec.identifier) else { continue }
            let descriptor = HKSampleQueryDescriptor<HKCategorySample>(
                predicates: [.categorySample(type: type, predicate: predicate)],
                sortDescriptors: [SortDescriptor(\.startDate, order: .reverse)],
                limit: 500
            )
            let samples = try await descriptor.result(for: store)
            uploads += samples.map { sample in
                HealthSampleUpload(
                    externalId: sample.uuid.uuidString,
                    type: spec.typeName,
                    kind: "category",
                    value: nil,
                    unit: nil,
                    categoryValue: sample.value,
                    startDateUnixMs: sample.startDate.unixMilliseconds,
                    endDateUnixMs: sample.endDate.unixMilliseconds,
                    sourceName: sample.sourceRevision.source.name
                )
            }
        }

        return uploads
    }
}

private extension Date {
    var unixMilliseconds: Int64 { Int64(timeIntervalSince1970 * 1000) }
}
