# Data Model: Bilingual Localization and Theme Preferences

**Feature**: [spec.md](spec.md) | **Research**: [research.md](research.md)

Only the changes are listed. Patient, Prescription, Allergy, RefillRequest, and their `rowversion`
tokens, constraints, and relationships are unchanged (001 data-model).

## Persistent entities (SQL Server)

### Medication (changed)

| Field | Type | Rules |
|-------|------|-------|
| `Id` | int, PK | unchanged |
| `NameAr` | nvarchar(100), required | Trimmed, 1–100 characters, unique (`IX_Medications_NameAr`). New. |
| `NameEn` | nvarchar(100), required | Trimmed, 1–100 characters, unique (`IX_Medications_NameEn`). Renamed from `Name`, data kept. |

- Constructor: `Medication(string nameAr, string nameEn)`. Both are required, and each is validated
  as `Name` is today (`MaxNameLength = 100`).
- `IsSameAs` is unchanged (compares by key or reference). No rule reads a name.

### DrugInteraction (changed)

| Field | Type | Rules |
|-------|------|-------|
| `Id`, `MedicationAId`, `MedicationBId` | unchanged | The pair order check and the unique pair index are unchanged. |
| `DescriptionAr` | nvarchar(300), required | Trimmed, 1–300 characters. New. |
| `DescriptionEn` | nvarchar(300), required | Trimmed, 1–300 characters. Renamed from `Description`, data kept. |

- Constructor: `DrugInteraction(Medication first, Medication second, string descriptionAr, string descriptionEn)`.

### Migration `BilingualReferenceData` (research R7)

1. Rename `Medications.Name` → `NameEn`, and drop the old unique index on `Name`.
2. Rename `DrugInteractions.Description` → `DescriptionEn`.
3. Add `NameAr` and `DescriptionAr` as nullable columns. Fill them from the seed map below.
   Rows not in the map get a copy of the English value.
4. Alter both columns to NOT NULL. Create `IX_Medications_NameEn` (unique) and
   `IX_Medications_NameAr` (unique).
5. `Down` reverses steps 1–4.

### Seed names (authored, synthetic; 001 FR-019)

| NameEn (existing) | NameAr |
|-------------------|--------|
| Avelorin | أفيلورين |
| Brimexa | بريميكسا |
| Corvantil | كورفانتيل |
| Dexolune | ديكسولون |
| Elvadine | إلفادين |
| Fenquira | فينكيرا |
| Glimora | غليمورا |
| Halvexin | هالفيكسين |
| Jurvane | جورفان |
| Kestrolin | كيسترولين |
| Lumavex | لومافيكس |

The seeded interaction description:

- English (unchanged): `Synthetic demo interaction (fictional, not clinical guidance).`
- Arabic: `تداخل تجريبي مُصطنع (خيالي، وليس إرشادًا سريريًا).`

The migration writes these values as **literal values inside the migration file**, so it never
changes after it is committed. The seeder reads `Seeding/SeedNames.cs`. A later wording fix (for
example from the V13 review) updates `SeedNames.cs` and, if databases that are already migrated
must change too, a new data migration. The `BilingualReferenceData` migration is never edited. Test data builders
(`TestData`, `PagedListPerformanceTests`) generate unique names in both languages.

## Domain results (in memory; codes and facts, no text; research R6)

| Type | Before | After |
|------|--------|-------|
| `BlockingReason` | `(Code, Message, EligibleAt?)` | `(Code, EligibleAt?, InvalidData?)`, where `InvalidData` is of type `InvalidPrescriptionData` and is set only for `INVALID_PRESCRIPTION_DATA`, with values `DaysSupplyNotPositive`, `LastFilledMissing`, or `LastFilledAfterEvaluation`. |
| `Warning` | `(Code, Message, MedicationName, Description?)` | `(Code, Medication, Interaction?)`. `Medication` is the requested medication for `ALLERGY_CONFLICT` and the other active medication for `DRUG_INTERACTION`, the same choice as today's `MedicationName`. `Interaction` is set for `DRUG_INTERACTION`. |
| `DecisionResult.InvalidNote` | `(Message)` | `(NoteError Error)`, either `Required` or `TooLong`. |
| `CreateRequestResult.PrescriptionInactive` | `(Message)` | `()` |
| `RefillEvaluation` | unchanged | unchanged (`CanApprove`, order of reasons and warnings) |

Codes (`TOO_EARLY`, `NO_REFILLS_REMAINING`, `INVALID_PRESCRIPTION_DATA`, `ALLERGY_CONFLICT`,
`DRUG_INTERACTION`) and all rule logic are unchanged.

## Application types

| Type | Purpose |
|------|---------|
| `Language` enum | `Arabic`, `English`. Passed explicitly into queries and commands (research R4). |
| `IAppText`, `EnglishText`, `ArabicText`, `AppText.For(Language)` | Typed message catalog (research R5). The texts are in [contracts/http-api.md](contracts/http-api.md). |
| `LocalizedProjections` | Holds one AutoMapper `IConfigurationProvider` per `Language` (research R2). |
| `LocalizedNameDto(int Id, string Name)`, `LocalizedTextDto(int Id, string Description)` | Internal lookup projections for warning text (research R3). Not part of the API. |

The response DTOs keep their exact shape and field names (spec FR-021). The values of these
fields become language-dependent: `medicationName`, `patient.allergies[]`,
`activePrescriptions[].medicationName`, the `message` of each reason and warning, the warning's
`medicationName` and `description`, and the ProblemDetails `title`, `detail`, and `errors` texts.

## Browser-held state (frontend)

| Key | Values | Default | Owner |
|-----|--------|---------|-------|
| `localStorage['prx.language']` | `ar` \| `en` | `ar` | `PreferencesStore` → `I18nService` |
| `localStorage['prx.theme']` | `light` \| `dark` | `light` | `PreferencesStore` → `ThemeService` |

- Any other value, a missing key, or a storage exception falls back to the default and is not
  rewritten until the user chooses. The two keys are never written together (spec FR-009, FR-027).
- The document attributes the services own are `html[lang]`, `html[dir]` (`rtl` for `ar`, `ltr`
  for `en`), `html[data-theme]`, `meta[name=color-scheme]`, and `document.title`.

### Transitions

```text
Language: ar ⇄ en       (on user choice: write the key, set lang/dir/title, emit languageChanged$, pages reload)
Theme:    light ⇄ dark  (on user choice: write the key, set data-theme/color-scheme; no data reload)
```

The two machines are independent. Neither transition touches the other's key or attributes.

## Design tokens (theme)

The roles are the existing 002/004 token names, now backed by CSS custom properties with a value
per theme. The Dark values and their contrast are in [research.md](research.md) R12. New explicit
tokens replace the SCSS color functions: `--focus-ring`, `--shadow-color`, `--canvas-border`,
`--glow-opacity`, and the sheet border.
