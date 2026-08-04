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

/// Liest **alle** Quantity- und Category-Typen aus `HealthKitCatalog` für einen Rückblick-
/// Zeitraum — dieselbe Liste, für die `HealthKitManager` Lesezugriff anfragt, damit beide nie
/// auseinanderlaufen.
///
/// Bewusst (noch) nicht dabei: Workouts, EKG, Audiogramm, Aktivitätsringe, Serien und
/// Charakteristika (Geburtsdatum etc.) — das sind keine einfachen Quantity-/Category-Samples,
/// sondern eigene Objektmodelle mit mehreren Feldern, die nicht in die aktuelle
/// `HealthSampleUpload`-Form (ein numerischer/kategorialer Wert pro Sample) passen. Eigener
/// Sync-Pfad für später statt sie hier schlecht reinzuquetschen.
@available(iOS 15.0, *)
enum HealthDataReader {
    /// `days: nil` = "Alles" (kein Startdatum-Filter) — vom Nutzer explizit in den Sync-
    /// Einstellungen gewählt, siehe `User.SyncWindowDays` auf der API-Seite.
    static func readRecentSamples(days: Int?) async throws -> [HealthSampleUpload] {
        let store = HKHealthStore()
        let start = days.map { Calendar.current.date(byAdding: .day, value: -$0, to: Date()) ?? Date() }
        let predicate = HKQuery.predicateForSamples(withStart: start, end: Date())

        var quantitySpecs = HealthKitCatalog.quantitySpecs
        if #available(iOS 18.0, *) {
            quantitySpecs += HealthKitCatalog.quantitySpecsIOS18
        }

        var categorySpecs = HealthKitCatalog.categorySpecs
        if #available(iOS 18.0, *) {
            categorySpecs += HealthKitCatalog.categorySpecsIOS18
        }
        if #available(iOS 26.2, *) {
            categorySpecs += HealthKitCatalog.categorySpecsIOS262
        }

        var uploads: [HealthSampleUpload] = []

        for spec in quantitySpecs {
            guard let type = HKObjectType.quantityType(forIdentifier: spec.identifier) else { continue }
            let descriptor = HKSampleQueryDescriptor<HKQuantitySample>(
                predicates: [.quantitySample(type: type, predicate: predicate)],
                sortDescriptors: [SortDescriptor(\.startDate, order: .reverse)],
                limit: 1000
            )
            // Einzelne Typen können scheitern (z. B. wenn der Nutzer den Zugriff für genau
            // diesen Typ verweigert hat) — ein Fehlschlag soll nicht den ganzen Sync abbrechen.
            guard let samples = try? await descriptor.result(for: store) else { continue }
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
                limit: 1000
            )
            guard let samples = try? await descriptor.result(for: store) else { continue }
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
