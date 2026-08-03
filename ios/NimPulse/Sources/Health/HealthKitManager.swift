import HealthKit

/// Requests read access to every HealthKit data type NimPulse can meaningfully use.
///
/// The identifier lists below are grouped by the iOS version each type was introduced in
/// (see HealthKit/HKTypeIdentifiers.h) so the request compiles cleanly against the app's
/// iOS 17 deployment target while still picking up newer types on devices that support them.
/// Clinical records (allergies, medications, lab results, ...) need Apple's separate
/// "Clinical Health Records" entitlement approval on top of the standard HealthKit
/// capability — see docs/HEALTHKIT.md before shipping that part.
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
        readTypes.formUnion(Self.correlationTypes())
        readTypes.formUnion(Self.documentAndAssessmentTypes())
        readTypes.formUnion(Self.singletonObjectTypes())
        readTypes.formUnion(Self.seriesTypes())
        readTypes.formUnion(Self.clinicalRecordTypes())

        try await healthStore.requestAuthorization(toShare: [], read: readTypes)
    }

    // MARK: - Quantity types (Körpermaße, Fitness, Vitalwerte, Ernährung, Atmung, Herz, ...)

    private static func quantityTypes() -> Set<HKObjectType> {
        var identifiers: [HKQuantityTypeIdentifier] = [
            // Körpermaße
            .appleSleepingWristTemperature, .bodyFatPercentage, .bodyMass, .bodyMassIndex,
            .electrodermalActivity, .height, .leanBodyMass, .waistCircumference,
            // Fitness
            .activeEnergyBurned, .appleExerciseTime, .appleMoveTime, .appleStandTime,
            .basalEnergyBurned, .cyclingCadence, .cyclingFunctionalThresholdPower,
            .cyclingPower, .cyclingSpeed, .distanceCycling, .distanceDownhillSnowSports,
            .distanceSwimming, .distanceWalkingRunning, .distanceWheelchair,
            .flightsClimbed, .nikeFuel, .physicalEffort, .pushCount, .runningPower,
            .runningSpeed, .stepCount, .swimmingStrokeCount, .underwaterDepth,
            // Hörgesundheit
            .environmentalAudioExposure, .environmentalSoundReduction, .headphoneAudioExposure,
            // Herz
            .atrialFibrillationBurden, .heartRate, .heartRateRecoveryOneMinute,
            .heartRateVariabilitySDNN, .peripheralPerfusionIndex, .restingHeartRate,
            .vo2Max, .walkingHeartRateAverage,
            // Mobilität
            .appleWalkingSteadiness, .runningGroundContactTime, .runningStrideLength,
            .runningVerticalOscillation, .sixMinuteWalkTestDistance, .stairAscentSpeed,
            .stairDescentSpeed, .walkingAsymmetryPercentage, .walkingDoubleSupportPercentage,
            .walkingSpeed, .walkingStepLength,
            // Ernährung
            .dietaryBiotin, .dietaryCaffeine, .dietaryCalcium, .dietaryCarbohydrates,
            .dietaryChloride, .dietaryCholesterol, .dietaryChromium, .dietaryCopper,
            .dietaryEnergyConsumed, .dietaryFatMonounsaturated, .dietaryFatPolyunsaturated,
            .dietaryFatSaturated, .dietaryFatTotal, .dietaryFiber, .dietaryFolate,
            .dietaryIodine, .dietaryIron, .dietaryMagnesium, .dietaryManganese,
            .dietaryMolybdenum, .dietaryNiacin, .dietaryPantothenicAcid, .dietaryPhosphorus,
            .dietaryPotassium, .dietaryProtein, .dietaryRiboflavin, .dietarySelenium,
            .dietarySodium, .dietarySugar, .dietaryThiamin, .dietaryVitaminA,
            .dietaryVitaminB12, .dietaryVitaminB6, .dietaryVitaminC, .dietaryVitaminD,
            .dietaryVitaminE, .dietaryVitaminK, .dietaryWater, .dietaryZinc,
            // Sonstiges
            .bloodAlcoholContent, .bloodPressureDiastolic, .bloodPressureSystolic,
            .insulinDelivery, .numberOfAlcoholicBeverages, .numberOfTimesFallen,
            .timeInDaylight, .uvExposure, .waterTemperature,
            // Fortpflanzungsgesundheit
            .basalBodyTemperature,
            // Atmung
            .forcedExpiratoryVolume1, .forcedVitalCapacity, .inhalerUsage,
            .oxygenSaturation, .peakExpiratoryFlowRate, .respiratoryRate,
            // Vitalwerte
            .bloodGlucose, .bodyTemperature,
        ]

        if #available(iOS 18.0, *) {
            identifiers += [
                .crossCountrySkiingSpeed, .distanceCrossCountrySkiing, .distancePaddleSports,
                .distanceRowing, .distanceSkatingSports, .estimatedWorkoutEffortScore,
                .paddleSportsSpeed, .rowingSpeed, .workoutEffortScore,
                .appleSleepingBreathingDisturbances,
            ]
        }

        return Set(identifiers.compactMap { HKObjectType.quantityType(forIdentifier: $0) })
    }

    // MARK: - Category types (Schlaf, Zyklus, Symptome, Achtsamkeit, ...)

    private static func categoryTypes() -> Set<HKObjectType> {
        var identifiers: [HKCategoryTypeIdentifier] = [
            .appleStandHour, .environmentalAudioExposureEvent, .headphoneAudioExposureEvent,
            .highHeartRateEvent, .irregularHeartRhythmEvent, .lowCardioFitnessEvent,
            .lowHeartRateEvent, .mindfulSession, .appleWalkingSteadinessEvent,
            .handwashingEvent, .toothbrushingEvent,
            // Zyklus & Fortpflanzungsgesundheit
            .cervicalMucusQuality, .contraceptive, .infrequentMenstrualCycles,
            .intermenstrualBleeding, .irregularMenstrualCycles, .lactation, .menstrualFlow,
            .ovulationTestResult, .persistentIntermenstrualBleeding, .pregnancy,
            .pregnancyTestResult, .progesteroneTestResult, .prolongedMenstrualPeriods,
            .sexualActivity,
            // Schlaf
            .sleepAnalysis,
            // Symptome
            .abdominalCramps, .acne, .appetiteChanges, .bladderIncontinence, .bloating,
            .breastPain, .chestTightnessOrPain, .chills, .constipation, .coughing,
            .diarrhea, .dizziness, .drySkin, .fainting, .fatigue, .fever,
            .generalizedBodyAche, .hairLoss, .headache, .heartburn, .hotFlashes,
            .lossOfSmell, .lossOfTaste, .lowerBackPain, .memoryLapse, .moodChanges,
            .nausea, .nightSweats, .pelvicPain, .rapidPoundingOrFlutteringHeartbeat,
            .runnyNose, .shortnessOfBreath, .sinusCongestion, .skippedHeartbeat,
            .sleepChanges, .soreThroat, .vaginalDryness, .vomiting, .wheezing,
        ]

        if #available(iOS 18.0, *) {
            identifiers += [.bleedingAfterPregnancy, .bleedingDuringPregnancy, .sleepApneaEvent]
        }
        if #available(iOS 26.2, *) {
            identifiers += [.hypertensionEvent]
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

    // MARK: - Korrelationen (Blutdruck, Mahlzeiten)

    private static func correlationTypes() -> Set<HKObjectType> {
        let identifiers: [HKCorrelationTypeIdentifier] = [.bloodPressure, .food]
        return Set(identifiers.compactMap { HKObjectType.correlationType(forIdentifier: $0) })
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

    private static func singletonObjectTypes() -> Set<HKObjectType> {
        var types: Set<HKObjectType> = [
            HKObjectType.workoutType(),
            HKObjectType.activitySummaryType(),
            HKObjectType.audiogramSampleType(),
            HKObjectType.electrocardiogramType(),
            HKObjectType.visionPrescriptionType(),
        ]

        if #available(iOS 18.0, *) {
            types.insert(HKObjectType.stateOfMindType())
        }
        if #available(iOS 26.0, *) {
            types.insert(HKObjectType.medicationDoseEventType())
            types.insert(HKObjectType.userAnnotatedMedicationType())
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
