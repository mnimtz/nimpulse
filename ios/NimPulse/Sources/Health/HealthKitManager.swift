import HealthKit

/// Requests read access to every HealthKit data type NimPulse can meaningfully use *today*.
///
/// The identifier lists below are grouped by the iOS version each type was introduced in
/// (see HealthKit/HKTypeIdentifiers.h) so the request compiles cleanly against the app's
/// iOS 17 deployment target while still picking up newer types on devices that support them.
///
/// A handful of type groups are deliberately **excluded** from `requestFullReadAuthorization()`
/// — confirmed via a real-device crash log on 2026-08-03 (iPhone 17 Pro), where including them
/// threw an uncaught `NSInvalidArgumentException` ("Authorization to read the following types
/// is disallowed: ...") and terminated the app outright, rather than the silent per-type denial
/// this codebase originally assumed:
///   - `HKClinicalTypeIdentifier.*` (allergies, medications, lab results, ...) — needs Apple's
///     separate "Clinical Health Records" entitlement approval. See `requestClinicalRecordsAuthorization()`.
///   - `HKObjectType.visionPrescriptionType()`, `.medicationDoseEventType()` (iOS 26+),
///     `.userAnnotatedMedicationType()` (iOS 26+) — same story, no documented entitlement to
///     request them with today.
///   - `HKCorrelationTypeIdentifier.bloodPressure` / `.food` — these aren't lost data: the
///     constituent quantity types (`.bloodPressureSystolic/Diastolic`, all `.dietary*`) are
///     already requested directly in `quantityTypes()` and cover the same samples; only the
///     correlation-level read grant is disallowed.
final class HealthKitManager {
    static let shared = HealthKitManager()

    private let healthStore = HKHealthStore()

    enum HealthKitError: Error {
        case notAvailableOnThisDevice
    }

    private init() {}

    func requestFullReadAuthorization() async throws {
        guard HKHealthStore.isHealthDataAvailable() else {
            throw HealthKitError.notAvailableOnThisDevice
        }

        var readTypes = Set<HKObjectType>()
        readTypes.formUnion(Self.quantityTypes())
        readTypes.formUnion(Self.categoryTypes())
        readTypes.formUnion(Self.characteristicTypes())
        readTypes.formUnion(Self.documentAndAssessmentTypes())
        readTypes.formUnion(Self.singletonObjectTypes())
        readTypes.formUnion(Self.seriesTypes())

        try await healthStore.requestAuthorization(toShare: [], read: readTypes)
    }

    /// Not called automatically — wire this in once Apple has approved the Clinical Health
    /// Records entitlement for this bundle ID (`com.apple.developer.healthkit.access` containing
    /// `"health-records"`; see docs/HEALTHKIT.md). Calling it before that approval reproduces the
    /// same crash `requestFullReadAuthorization()` used to have.
    func requestClinicalRecordsAuthorization() async throws {
        try await healthStore.requestAuthorization(toShare: [], read: Self.clinicalRecordTypes())
    }

    // MARK: - Quantity/Category types — aus HealthKitCatalog, der einzigen Quelle der Wahrheit
    // (dieselben Listen liest HealthDataReader zum tatsächlichen Sample-Abruf).

    private static func quantityTypes() -> Set<HKObjectType> {
        var identifiers = HealthKitCatalog.quantitySpecs.map(\.identifier)
        if #available(iOS 18.0, *) {
            identifiers += HealthKitCatalog.quantitySpecsIOS18.map(\.identifier)
        }
        return Set(identifiers.compactMap { HKObjectType.quantityType(forIdentifier: $0) })
    }

    private static func categoryTypes() -> Set<HKObjectType> {
        var identifiers = HealthKitCatalog.categorySpecs.map(\.identifier)
        if #available(iOS 18.0, *) {
            identifiers += HealthKitCatalog.categorySpecsIOS18.map(\.identifier)
        }
        if #available(iOS 26.2, *) {
            identifiers += HealthKitCatalog.categorySpecsIOS262.map(\.identifier)
        }
        return Set(identifiers.compactMap { HKObjectType.categoryType(forIdentifier: $0) })
    }

    // MARK: - Charakteristika (Stammdaten: Geburtsdatum, Blutgruppe, ...)

    private static func characteristicTypes() -> Set<HKObjectType> {
        let identifiers: [HKCharacteristicTypeIdentifier] = [
            .activityMoveMode, .biologicalSex, .bloodType, .dateOfBirth,
            .fitzpatrickSkinType, .wheelchairUse,
        ]
        return Set(identifiers.compactMap { HKObjectType.characteristicType(forIdentifier: $0) })
    }

    // MARK: - Dokumente & Assessments (CDA, GAD-7/PHQ-9-Fragebögen)

    private static func documentAndAssessmentTypes() -> Set<HKObjectType> {
        var types = Set<HKObjectType>()
        if let cda = HKObjectType.documentType(forIdentifier: .CDA) {
            types.insert(cda)
        }
        if #available(iOS 18.0, *) {
            let assessments: [HKScoredAssessmentTypeIdentifier] = [.GAD7, .PHQ9]
            types.formUnion(assessments.map { HKScoredAssessmentType($0) })
        }
        return types
    }

    // MARK: - Einzeltypen ohne Identifier-String (Workouts, Aktivitätsringe, EKG, Audiogramm, ...)
    //
    // visionPrescriptionType(), medicationDoseEventType() und userAnnotatedMedicationType()
    // bewusst nicht dabei — siehe Klassenkommentar (realer Absturz ohne passendes Entitlement).

    private static func singletonObjectTypes() -> Set<HKObjectType> {
        var types: Set<HKObjectType> = [
            HKObjectType.workoutType(),
            HKObjectType.activitySummaryType(),
            HKObjectType.audiogramSampleType(),
            HKObjectType.electrocardiogramType(),
        ]

        if #available(iOS 18.0, *) {
            types.insert(HKObjectType.stateOfMindType())
        }

        return types
    }

    // MARK: - Serien (Workout-Route, Herzschlag-Serie)

    private static func seriesTypes() -> Set<HKObjectType> {
        var types = Set<HKObjectType>()
        if let route = HKObjectType.seriesType(forIdentifier: HKWorkoutRouteTypeIdentifier) {
            types.insert(route)
        }
        if let heartbeat = HKObjectType.seriesType(forIdentifier: HKDataTypeIdentifierHeartbeatSeries) {
            types.insert(heartbeat)
        }
        return types
    }

    // MARK: - Klinische Aufzeichnungen (Gesundheitsakte)
    //
    // Benötigen zusätzlich zur normalen HealthKit-Capability das gesonderte
    // "Clinical Health Records"-Entitlement (com.apple.developer.healthkit.access
    // mit "health-records"), das Apple separat genehmigen muss. Ohne dieses
    // Entitlement ignoriert HealthKit diese Typen in der Autorisierungsanfrage
    // kommentarlos — die Anfrage selbst schlägt dadurch nicht fehl.

    private static func clinicalRecordTypes() -> Set<HKObjectType> {
        let identifiers: [HKClinicalTypeIdentifier] = [
            .allergyRecord, .clinicalNoteRecord, .conditionRecord, .immunizationRecord,
            .labResultRecord, .medicationRecord, .procedureRecord, .vitalSignRecord,
            .coverageRecord,
        ]
        return Set(identifiers.compactMap { HKObjectType.clinicalType(forIdentifier: $0) })
    }
}
