using System.Text;

namespace NimPulse.Core.Health;

/// <summary>
/// Deutsche Anzeige-Namen für HealthKit-Typen (Dashboard/Reports) — der rohe Identifier
/// (z. B. "stepCount") bleibt der Sync-Schlüssel in der DB, das hier ist nur eine Anzeige-Schicht
/// obendrauf. Deckt die Typen ab, die in einem typischen Sync tatsächlich auftauchen (Fitness,
/// Herz, Körpermaße, Schlaf, verbreitete Ernährungswerte, Atmung, Vitalwerte) — für alles andere
/// liefert <see cref="GetDisplayName"/> einen lesbaren Fallback (aus camelCase Wörter getrennt),
/// statt den Identifier roh anzuzeigen oder die Liste auf ~190 Einträge aufzublähen.
/// </summary>
public static class HealthTypeCatalog
{
    private static readonly Dictionary<string, string> DisplayNames = new()
    {
        // Fitness
        ["stepCount"] = "Schritte",
        ["distanceWalkingRunning"] = "Gelaufene/gejoggte Distanz",
        ["distanceCycling"] = "Radstrecke",
        ["distanceSwimming"] = "Schwimmstrecke",
        ["flightsClimbed"] = "Stockwerke",
        ["activeEnergyBurned"] = "Aktive Energie",
        ["basalEnergyBurned"] = "Grundumsatz",
        ["appleExerciseTime"] = "Trainingszeit",
        ["appleStandTime"] = "Stehzeit",
        ["appleMoveTime"] = "Bewegungszeit",
        ["pushCount"] = "Schübe (Rollstuhl)",
        ["swimmingStrokeCount"] = "Schwimmzüge",
        ["cyclingCadence"] = "Trittfrequenz",
        ["cyclingPower"] = "Rad-Leistung",
        ["cyclingSpeed"] = "Rad-Geschwindigkeit",
        ["runningPower"] = "Lauf-Leistung",
        ["runningSpeed"] = "Lauf-Geschwindigkeit",
        ["runningStrideLength"] = "Schrittlänge (Laufen)",
        ["walkingSpeed"] = "Gehgeschwindigkeit",
        ["walkingStepLength"] = "Schrittlänge (Gehen)",
        ["walkingAsymmetryPercentage"] = "Gang-Asymmetrie",
        ["walkingDoubleSupportPercentage"] = "Doppel-Standphase",
        ["appleWalkingSteadiness"] = "Gangsicherheit",
        ["sixMinuteWalkTestDistance"] = "6-Minuten-Gehtest",
        ["stairAscentSpeed"] = "Treppensteig-Tempo (aufwärts)",
        ["stairDescentSpeed"] = "Treppensteig-Tempo (abwärts)",
        ["vo2Max"] = "VO2max",
        ["physicalEffort"] = "Körperliche Anstrengung",

        // Herz
        ["heartRate"] = "Herzfrequenz",
        ["restingHeartRate"] = "Ruhe-Herzfrequenz",
        ["walkingHeartRateAverage"] = "Herzfrequenz beim Gehen",
        ["heartRateRecoveryOneMinute"] = "Herzfrequenz-Erholung (1 Min.)",
        ["heartRateVariabilitySDNN"] = "Herzfrequenzvariabilität",
        ["atrialFibrillationBurden"] = "Vorhofflimmern-Anteil",
        ["highHeartRateEvent"] = "Hohe Herzfrequenz",
        ["lowHeartRateEvent"] = "Niedrige Herzfrequenz",
        ["irregularHeartRhythmEvent"] = "Unregelmäßiger Herzrhythmus",
        ["lowCardioFitnessEvent"] = "Niedrige Herz-Kreislauf-Fitness",

        // Körpermaße
        ["bodyMass"] = "Körpergewicht",
        ["bodyMassIndex"] = "BMI",
        ["bodyFatPercentage"] = "Körperfettanteil",
        ["leanBodyMass"] = "Magermasse",
        ["height"] = "Größe",
        ["waistCircumference"] = "Taillenumfang",

        // Schlaf & Achtsamkeit
        ["sleepAnalysis"] = "Schlaf",
        ["mindfulSession"] = "Achtsamkeit",
        ["appleStandHour"] = "Steh-Stunde",

        // Atmung
        ["respiratoryRate"] = "Atemfrequenz",
        ["oxygenSaturation"] = "Sauerstoffsättigung",
        ["forcedVitalCapacity"] = "Forcierte Vitalkapazität",
        ["forcedExpiratoryVolume1"] = "Ausatemvolumen (1 Sek.)",
        ["peakExpiratoryFlowRate"] = "Peak-Flow",
        ["inhalerUsage"] = "Inhalator-Nutzung",

        // Vitalwerte
        ["bloodGlucose"] = "Blutzucker",
        ["bloodPressureSystolic"] = "Blutdruck (systolisch)",
        ["bloodPressureDiastolic"] = "Blutdruck (diastolisch)",
        ["bodyTemperature"] = "Körpertemperatur",
        ["basalBodyTemperature"] = "Basaltemperatur",

        // Ernährung (häufigste)
        ["dietaryEnergyConsumed"] = "Kalorien (Nahrung)",
        ["dietaryWater"] = "Wasser",
        ["dietaryProtein"] = "Protein",
        ["dietaryCarbohydrates"] = "Kohlenhydrate",
        ["dietaryFatTotal"] = "Fett gesamt",
        ["dietaryFiber"] = "Ballaststoffe",
        ["dietarySugar"] = "Zucker",
        ["dietaryCaffeine"] = "Koffein",
        ["dietarySodium"] = "Natrium",
        ["numberOfAlcoholicBeverages"] = "Alkoholische Getränke",

        // Sonstiges
        ["timeInDaylight"] = "Zeit im Tageslicht",
        ["uvExposure"] = "UV-Belastung",
        ["environmentalAudioExposure"] = "Umgebungslärm-Belastung",
        ["headphoneAudioExposure"] = "Kopfhörer-Lautstärke",
        ["numberOfTimesFallen"] = "Stürze",
        ["handwashingEvent"] = "Händewaschen",
        ["toothbrushingEvent"] = "Zähneputzen",
    };

    public static string GetDisplayName(string type)
    {
        if (DisplayNames.TryGetValue(type, out var name))
        {
            return name;
        }

        // Fallback für alle ~190 HealthKit-Typen, die (noch) keine eigene Übersetzung haben:
        // camelCase in getrennte, großgeschriebene Wörter umwandeln statt roh anzuzeigen.
        var builder = new StringBuilder();
        foreach (var c in type)
        {
            if (char.IsUpper(c) && builder.Length > 0)
            {
                builder.Append(' ');
            }
            builder.Append(builder.Length == 0 ? char.ToUpperInvariant(c) : c);
        }
        return builder.ToString();
    }
}
