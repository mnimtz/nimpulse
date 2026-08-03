# HealthKit-Datenumfang

`HealthKitManager.requestFullReadAuthorization()` fragt Lesezugriff auf praktisch alle nutzbaren HealthKit-Objekttypen an: Quantity-, Category- und Characteristic-Types, CDA-Dokumente, GAD-7/PHQ-9-Assessments, Workouts, Aktivitätsringe, EKG, Audiogramm, State-of-Mind und Workout-/Herzschlag-Serien.

Die Typ-Listen in `ios/NimPulse/Sources/Health/HealthKitManager.swift` sind direkt aus `HealthKit/HKTypeIdentifiers.h` des iOS-26.5-SDK erzeugt und gegen den echten Swift-Compiler typgeprüft (`xcrun -sdk iphonesimulator swiftc -typecheck`) — nicht aus dem Gedächtnis geraten. Neue Identifier, die Apple in künftigen iOS-Versionen hinzufügt, tauchen dort nicht automatisch auf; bei jedem großen iOS-Release lohnt ein Diff gegen den aktuellen Header.

## Bewusst ausgeschlossene Typen (Absturz bestätigt, 2026-08-03)

Ein Testlauf auf einem echten iPhone 17 Pro hat gezeigt, dass die ursprüngliche Annahme unten falsch war: HealthKit **ignoriert nicht-genehmigte Typen nicht still**, sondern wirft eine uncaught `NSInvalidArgumentException` ("Authorization to read the following types is disallowed: ...") und beendet die App sofort, sobald auch nur einer der folgenden Typen in der Anfrage steckt:

- **Klinische Aufzeichnungen** (`HKClinicalTypeIdentifier.*` — Allergien, Medikamente, Diagnosen, Laborwerte, Immunisierungen, Prozeduren, Vitalwerte-Aufzeichnungen, Versicherungsdaten, klinische Notizen)
- **Sehstärken-Rezept** (`HKObjectType.visionPrescriptionType()`)
- **Medikations-Tracking** (`medicationDoseEventType()`, `userAnnotatedMedicationType()`, beide iOS 26+)
- **Blutdruck- und Mahlzeiten-Korrelationen** (`HKCorrelationTypeIdentifier.bloodPressure` / `.food`) — hier geht aber nichts an Daten verloren: die zugrunde liegenden Quantity-Types (`.bloodPressureSystolic`/`.bloodPressureDiastolic`, alle `.dietary*`) werden ohnehin einzeln angefragt und liefern dieselben Werte, nur die Korrelations-Ebene selbst ist gesperrt.

`requestFullReadAuthorization()` fragt diese Typen deshalb nicht mehr an.

## Klinische Aufzeichnungen (Gesundheitsakte) — für später vorbereitet

`clinicalRecordTypes()` existiert weiterhin (privat) und `requestClinicalRecordsAuthorization()` ist als separate, **nicht automatisch aufgerufene** Methode angelegt. Diese Kategorie benötigt zusätzlich zur normalen HealthKit-Capability das gesonderte **Clinical Health Records**-Entitlement (`com.apple.developer.healthkit.access: ["health-records"]`), das Apple manuell pro Bundle-ID freigeben muss (Antrag über den Apple-Developer-Support, keine Selbstbedienung im Portal).

Bis diese Freigabe vorliegt:
- Das Entitlement in `ios/project.yml` bleibt `com.apple.developer.healthkit.access: []`.
- `requestClinicalRecordsAuthorization()` bewusst noch nicht in der UI verdrahtet — ein Aufruf davor reproduziert denselben Absturz wie oben.
- Alle anderen Datentypen funktionieren unabhängig davon normal.

Sobald Apple zustimmt: `health-records` in `com.apple.developer.healthkit.access` eintragen, `xcodegen generate`, neu signieren, `requestClinicalRecordsAuthorization()` an die UI anbinden.
