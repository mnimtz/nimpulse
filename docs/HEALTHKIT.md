# HealthKit-Datenumfang

`HealthKitManager.requestFullReadAuthorization()` fragt Lesezugriff auf praktisch alle HealthKit-Objekttypen an: Quantity-, Category-, Characteristic- und Correlation-Types, Dokumente, GAD-7/PHQ-9-Assessments, Workouts, Aktivitätsringe, EKG, Audiogramm, Sehstärken-Rezept, State-of-Mind, Medikations-Tracking (iOS 26+) und Workout-/Herzschlag-Serien.

Die Typ-Listen in `ios/NimPulse/Sources/Health/HealthKitManager.swift` sind direkt aus `HealthKit/HKTypeIdentifiers.h` des iOS-26.5-SDK erzeugt und gegen den echten Swift-Compiler typgeprüft (`xcrun -sdk iphonesimulator swiftc -typecheck`) — nicht aus dem Gedächtnis geraten. Neue Identifier, die Apple in künftigen iOS-Versionen hinzufügt, tauchen dort nicht automatisch auf; bei jedem großen iOS-Release lohnt ein Diff gegen den aktuellen Header.

## Klinische Aufzeichnungen (Gesundheitsakte)

`clinicalRecordTypes()` fragt zusätzlich Allergien, Medikamente, Diagnosen, Laborwerte, Immunisierungen, Prozeduren, Vitalwerte-Aufzeichnungen und Versicherungsdaten an (`HKClinicalTypeIdentifier`). Diese Kategorie benötigt zusätzlich zur normalen HealthKit-Capability das gesonderte **Clinical Health Records**-Entitlement (`com.apple.developer.healthkit.access: ["health-records"]`), das Apple manuell pro Bundle-ID freigeben muss (Antrag über den Apple-Developer-Support, keine Selbstbedienung im Portal).

Bis diese Freigabe vorliegt:
- Das Entitlement in `ios/project.yml` bleibt `com.apple.developer.healthkit.access: []`.
- HealthKit ignoriert die Clinical-Record-Typen in der Autorisierungsanfrage kommentarlos — kein Fehler, aber auch keine Freigabe für diese Daten.
- Alle anderen Datentypen (Quantity/Category/Characteristic/Correlation/...) funktionieren unabhängig davon normal.

Sobald Apple zustimmt: `health-records` in `com.apple.developer.healthkit.access` eintragen, `xcodegen generate`, neu signieren.
