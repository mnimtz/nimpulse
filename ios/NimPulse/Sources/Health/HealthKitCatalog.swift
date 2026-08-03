import HealthKit

/// Single source of truth for every HealthKit Quantity/Category type NimPulse knows about —
/// used both for the read-authorization request (`HealthKitManager`) and for actually reading
/// samples (`HealthDataReader`), so the two never drift apart.
///
/// Identifiers, availability, and units are taken directly from `HealthKit/HKTypeIdentifiers.h`
/// (iOS 26.5 SDK) — units use `HKUnit(from:)` with Apple's own documented unit strings from that
/// header rather than hand-picked fluent constructors, to avoid transcription mistakes. Where the
/// header's stated unit differs from the conventional display unit (e.g. heart rate is
/// technically "count/s" in the header but every app shows "count/min"), `count/min` is used —
/// `HKQuantity.doubleValue(for:)` converts between dimensionally-compatible units automatically,
/// so this is a display choice, not a correctness issue.
///
/// Excludes exactly the types that crashed `requestFullReadAuthorization()` on a real device —
/// see `HealthKitManager`'s class comment.
@available(iOS 15.0, *)
enum HealthKitCatalog {
    struct QuantitySpec {
        let identifier: HKQuantityTypeIdentifier
        let typeName: String
        let unit: HKUnit
    }

    /// Available since iOS 8–17 — always included, no availability check needed at this
    /// deployment target.
    static let quantitySpecs: [QuantitySpec] = [
        // Körpermaße
        spec(.appleSleepingWristTemperature, "appleSleepingWristTemperature", "degC"),
        spec(.bodyFatPercentage, "bodyFatPercentage", "%"),
        spec(.bodyMass, "bodyMass", "kg"),
        spec(.bodyMassIndex, "bodyMassIndex", "count"),
        spec(.electrodermalActivity, "electrodermalActivity", "S"),
        spec(.height, "height", "m"),
        spec(.leanBodyMass, "leanBodyMass", "kg"),
        spec(.waistCircumference, "waistCircumference", "m"),
        // Fitness
        spec(.activeEnergyBurned, "activeEnergyBurned", "kcal"),
        spec(.appleExerciseTime, "appleExerciseTime", "min"),
        spec(.appleMoveTime, "appleMoveTime", "min"),
        spec(.appleStandTime, "appleStandTime", "min"),
        spec(.basalEnergyBurned, "basalEnergyBurned", "kcal"),
        spec(.cyclingCadence, "cyclingCadence", "count/min"),
        spec(.cyclingFunctionalThresholdPower, "cyclingFunctionalThresholdPower", "W"),
        spec(.cyclingPower, "cyclingPower", "W"),
        spec(.cyclingSpeed, "cyclingSpeed", "m/s"),
        spec(.distanceCycling, "distanceCycling", "m"),
        spec(.distanceDownhillSnowSports, "distanceDownhillSnowSports", "m"),
        spec(.distanceSwimming, "distanceSwimming", "m"),
        spec(.distanceWalkingRunning, "distanceWalkingRunning", "m"),
        spec(.distanceWheelchair, "distanceWheelchair", "m"),
        spec(.flightsClimbed, "flightsClimbed", "count"),
        spec(.nikeFuel, "nikeFuel", "count"),
        spec(.physicalEffort, "physicalEffort", "kcal/(kg*hr)"),
        spec(.pushCount, "pushCount", "count"),
        spec(.runningPower, "runningPower", "W"),
        spec(.runningSpeed, "runningSpeed", "m/s"),
        spec(.stepCount, "stepCount", "count"),
        spec(.swimmingStrokeCount, "swimmingStrokeCount", "count"),
        spec(.underwaterDepth, "underwaterDepth", "m"),
        // Hörgesundheit
        spec(.environmentalAudioExposure, "environmentalAudioExposure", "dBASPL"),
        spec(.environmentalSoundReduction, "environmentalSoundReduction", "dBASPL"),
        spec(.headphoneAudioExposure, "headphoneAudioExposure", "dBASPL"),
        // Herz
        spec(.atrialFibrillationBurden, "atrialFibrillationBurden", "%"),
        spec(.heartRate, "heartRate", "count/min"),
        spec(.heartRateRecoveryOneMinute, "heartRateRecoveryOneMinute", "count/min"),
        spec(.heartRateVariabilitySDNN, "heartRateVariabilitySDNN", "ms"),
        spec(.peripheralPerfusionIndex, "peripheralPerfusionIndex", "%"),
        spec(.restingHeartRate, "restingHeartRate", "count/min"),
        spec(.vo2Max, "vo2Max", "ml/(kg*min)"),
        spec(.walkingHeartRateAverage, "walkingHeartRateAverage", "count/min"),
        // Mobilität
        spec(.appleWalkingSteadiness, "appleWalkingSteadiness", "%"),
        spec(.runningGroundContactTime, "runningGroundContactTime", "ms"),
        spec(.runningStrideLength, "runningStrideLength", "m"),
        spec(.runningVerticalOscillation, "runningVerticalOscillation", "cm"),
        spec(.sixMinuteWalkTestDistance, "sixMinuteWalkTestDistance", "m"),
        spec(.stairAscentSpeed, "stairAscentSpeed", "m/s"),
        spec(.stairDescentSpeed, "stairDescentSpeed", "m/s"),
        spec(.walkingAsymmetryPercentage, "walkingAsymmetryPercentage", "%"),
        spec(.walkingDoubleSupportPercentage, "walkingDoubleSupportPercentage", "%"),
        spec(.walkingSpeed, "walkingSpeed", "m/s"),
        spec(.walkingStepLength, "walkingStepLength", "m"),
        // Ernährung
        spec(.dietaryBiotin, "dietaryBiotin", "g"),
        spec(.dietaryCaffeine, "dietaryCaffeine", "g"),
        spec(.dietaryCalcium, "dietaryCalcium", "g"),
        spec(.dietaryCarbohydrates, "dietaryCarbohydrates", "g"),
        spec(.dietaryChloride, "dietaryChloride", "g"),
        spec(.dietaryCholesterol, "dietaryCholesterol", "g"),
        spec(.dietaryChromium, "dietaryChromium", "g"),
        spec(.dietaryCopper, "dietaryCopper", "g"),
        spec(.dietaryEnergyConsumed, "dietaryEnergyConsumed", "kcal"),
        spec(.dietaryFatMonounsaturated, "dietaryFatMonounsaturated", "g"),
        spec(.dietaryFatPolyunsaturated, "dietaryFatPolyunsaturated", "g"),
        spec(.dietaryFatSaturated, "dietaryFatSaturated", "g"),
        spec(.dietaryFatTotal, "dietaryFatTotal", "g"),
        spec(.dietaryFiber, "dietaryFiber", "g"),
        spec(.dietaryFolate, "dietaryFolate", "g"),
        spec(.dietaryIodine, "dietaryIodine", "g"),
        spec(.dietaryIron, "dietaryIron", "g"),
        spec(.dietaryMagnesium, "dietaryMagnesium", "g"),
        spec(.dietaryManganese, "dietaryManganese", "g"),
        spec(.dietaryMolybdenum, "dietaryMolybdenum", "g"),
        spec(.dietaryNiacin, "dietaryNiacin", "g"),
        spec(.dietaryPantothenicAcid, "dietaryPantothenicAcid", "g"),
        spec(.dietaryPhosphorus, "dietaryPhosphorus", "g"),
        spec(.dietaryPotassium, "dietaryPotassium", "g"),
        spec(.dietaryProtein, "dietaryProtein", "g"),
        spec(.dietaryRiboflavin, "dietaryRiboflavin", "g"),
        spec(.dietarySelenium, "dietarySelenium", "g"),
        spec(.dietarySodium, "dietarySodium", "g"),
        spec(.dietarySugar, "dietarySugar", "g"),
        spec(.dietaryThiamin, "dietaryThiamin", "g"),
        spec(.dietaryVitaminA, "dietaryVitaminA", "g"),
        spec(.dietaryVitaminB12, "dietaryVitaminB12", "g"),
        spec(.dietaryVitaminB6, "dietaryVitaminB6", "g"),
        spec(.dietaryVitaminC, "dietaryVitaminC", "g"),
        spec(.dietaryVitaminD, "dietaryVitaminD", "g"),
        spec(.dietaryVitaminE, "dietaryVitaminE", "g"),
        spec(.dietaryVitaminK, "dietaryVitaminK", "g"),
        spec(.dietaryWater, "dietaryWater", "mL"),
        spec(.dietaryZinc, "dietaryZinc", "g"),
        // Sonstiges
        spec(.bloodAlcoholContent, "bloodAlcoholContent", "%"),
        spec(.bloodPressureDiastolic, "bloodPressureDiastolic", "mmHg"),
        spec(.bloodPressureSystolic, "bloodPressureSystolic", "mmHg"),
        spec(.insulinDelivery, "insulinDelivery", "IU"),
        spec(.numberOfAlcoholicBeverages, "numberOfAlcoholicBeverages", "count"),
        spec(.numberOfTimesFallen, "numberOfTimesFallen", "count"),
        spec(.timeInDaylight, "timeInDaylight", "min"),
        spec(.uvExposure, "uvExposure", "count"),
        spec(.waterTemperature, "waterTemperature", "degC"),
        // Fortpflanzungsgesundheit
        spec(.basalBodyTemperature, "basalBodyTemperature", "degC"),
        // Atmung
        spec(.forcedExpiratoryVolume1, "forcedExpiratoryVolume1", "L"),
        spec(.forcedVitalCapacity, "forcedVitalCapacity", "L"),
        spec(.inhalerUsage, "inhalerUsage", "count"),
        spec(.oxygenSaturation, "oxygenSaturation", "%"),
        spec(.peakExpiratoryFlowRate, "peakExpiratoryFlowRate", "L/min"),
        spec(.respiratoryRate, "respiratoryRate", "count/min"),
        // Vitalwerte
        spec(.bloodGlucose, "bloodGlucose", "mg/dL"),
        spec(.bodyTemperature, "bodyTemperature", "degC"),
    ]

    /// iOS 18+ only — see HealthKitManager's class comment for the excluded (crash-causing) types.
    @available(iOS 18.0, *)
    static let quantitySpecsIOS18: [QuantitySpec] = [
        spec(.crossCountrySkiingSpeed, "crossCountrySkiingSpeed", "m/s"),
        spec(.distanceCrossCountrySkiing, "distanceCrossCountrySkiing", "m"),
        spec(.distancePaddleSports, "distancePaddleSports", "m"),
        spec(.distanceRowing, "distanceRowing", "m"),
        spec(.distanceSkatingSports, "distanceSkatingSports", "m"),
        spec(.estimatedWorkoutEffortScore, "estimatedWorkoutEffortScore", "appleEffortScore"),
        spec(.paddleSportsSpeed, "paddleSportsSpeed", "m/s"),
        spec(.rowingSpeed, "rowingSpeed", "m/s"),
        spec(.workoutEffortScore, "workoutEffortScore", "appleEffortScore"),
        spec(.appleSleepingBreathingDisturbances, "appleSleepingBreathingDisturbances", "count"),
    ]

    static let categorySpecs: [(identifier: HKCategoryTypeIdentifier, typeName: String)] = [
        (.appleStandHour, "appleStandHour"),
        (.environmentalAudioExposureEvent, "environmentalAudioExposureEvent"),
        (.headphoneAudioExposureEvent, "headphoneAudioExposureEvent"),
        (.highHeartRateEvent, "highHeartRateEvent"),
        (.irregularHeartRhythmEvent, "irregularHeartRhythmEvent"),
        (.lowCardioFitnessEvent, "lowCardioFitnessEvent"),
        (.lowHeartRateEvent, "lowHeartRateEvent"),
        (.mindfulSession, "mindfulSession"),
        (.appleWalkingSteadinessEvent, "appleWalkingSteadinessEvent"),
        (.handwashingEvent, "handwashingEvent"),
        (.toothbrushingEvent, "toothbrushingEvent"),
        (.cervicalMucusQuality, "cervicalMucusQuality"),
        (.contraceptive, "contraceptive"),
        (.infrequentMenstrualCycles, "infrequentMenstrualCycles"),
        (.intermenstrualBleeding, "intermenstrualBleeding"),
        (.irregularMenstrualCycles, "irregularMenstrualCycles"),
        (.lactation, "lactation"),
        (.menstrualFlow, "menstrualFlow"),
        (.ovulationTestResult, "ovulationTestResult"),
        (.persistentIntermenstrualBleeding, "persistentIntermenstrualBleeding"),
        (.pregnancy, "pregnancy"),
        (.pregnancyTestResult, "pregnancyTestResult"),
        (.progesteroneTestResult, "progesteroneTestResult"),
        (.prolongedMenstrualPeriods, "prolongedMenstrualPeriods"),
        (.sexualActivity, "sexualActivity"),
        (.sleepAnalysis, "sleepAnalysis"),
        (.abdominalCramps, "abdominalCramps"),
        (.acne, "acne"),
        (.appetiteChanges, "appetiteChanges"),
        (.bladderIncontinence, "bladderIncontinence"),
        (.bloating, "bloating"),
        (.breastPain, "breastPain"),
        (.chestTightnessOrPain, "chestTightnessOrPain"),
        (.chills, "chills"),
        (.constipation, "constipation"),
        (.coughing, "coughing"),
        (.diarrhea, "diarrhea"),
        (.dizziness, "dizziness"),
        (.drySkin, "drySkin"),
        (.fainting, "fainting"),
        (.fatigue, "fatigue"),
        (.fever, "fever"),
        (.generalizedBodyAche, "generalizedBodyAche"),
        (.hairLoss, "hairLoss"),
        (.headache, "headache"),
        (.heartburn, "heartburn"),
        (.hotFlashes, "hotFlashes"),
        (.lossOfSmell, "lossOfSmell"),
        (.lossOfTaste, "lossOfTaste"),
        (.lowerBackPain, "lowerBackPain"),
        (.memoryLapse, "memoryLapse"),
        (.moodChanges, "moodChanges"),
        (.nausea, "nausea"),
        (.nightSweats, "nightSweats"),
        (.pelvicPain, "pelvicPain"),
        (.rapidPoundingOrFlutteringHeartbeat, "rapidPoundingOrFlutteringHeartbeat"),
        (.runnyNose, "runnyNose"),
        (.shortnessOfBreath, "shortnessOfBreath"),
        (.sinusCongestion, "sinusCongestion"),
        (.skippedHeartbeat, "skippedHeartbeat"),
        (.sleepChanges, "sleepChanges"),
        (.soreThroat, "soreThroat"),
        (.vaginalDryness, "vaginalDryness"),
        (.vomiting, "vomiting"),
        (.wheezing, "wheezing"),
    ]

    @available(iOS 18.0, *)
    static let categorySpecsIOS18: [(identifier: HKCategoryTypeIdentifier, typeName: String)] = [
        (.bleedingAfterPregnancy, "bleedingAfterPregnancy"),
        (.bleedingDuringPregnancy, "bleedingDuringPregnancy"),
        (.sleepApneaEvent, "sleepApneaEvent"),
    ]

    @available(iOS 26.2, *)
    static let categorySpecsIOS262: [(identifier: HKCategoryTypeIdentifier, typeName: String)] = [
        (.hypertensionEvent, "hypertensionEvent"),
    ]

    private static func spec(_ identifier: HKQuantityTypeIdentifier, _ typeName: String, _ unitString: String) -> QuantitySpec {
        QuantitySpec(identifier: identifier, typeName: typeName, unit: HKUnit(from: unitString))
    }
}
