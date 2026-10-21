# HTTP Contract Delta: Language Negotiation and Localized Text

Extends [001 contracts/http-api.md](../../001-refill-request-review/contracts/http-api.md) and
[003 contracts/http-api.md](../../003-backend-pagination/contracts/http-api.md). Everything not
listed here is unchanged: endpoints, methods, status codes, `code` values, field names, field
order, enum and status values, paging, ordering, and live-update messages.

## Request language

| Header | Applies to | Rule |
|--------|-----------|------|
| `Accept-Language` (request) | every `/api/refill-requests` endpoint | Resolved to exactly one of `ar` or `en`. `ar` and its regional variants (`ar-SA`, `ar-EG`, …) → `ar`. `en` and its variants (`en-US`, `en-GB`, …) → `en`. With several entries, quality order applies and the first supported entry wins. Missing, empty, malformed, or only unsupported values (`fr`, `*`) → `ar`. |
| `Content-Language` (response) | every response, including errors | The resolved language: `ar` or `en`. |

The frontend always sends `Accept-Language: ar` or `en` explicitly (spec FR-010).

**Behavior change for existing clients (spec C7)**: a client that sends no `Accept-Language`
header now receives **Arabic** text and names. A client that needs today's output sends
`Accept-Language: en`, which returns today's English text byte for byte.

Request parsing is language-independent. `status` stays `Pending|Approved|Denied` and numbers use
digits 0–9 in every language.

## Localized fields

| Location | Field | Source |
|----------|-------|--------|
| List item | `medicationName` | `NameAr` or `NameEn` |
| Detail | `prescription.medicationName`, `activePrescriptions[].medicationName` | `NameAr` or `NameEn` |
| Detail | `patient.allergies[]` | `NameAr` or `NameEn`, ordered by the localized name (ordinal) |
| Evaluation | `blockingReasons[].message`, `warnings[].message` | Catalog (below) |
| Evaluation | `warnings[].medicationName` | `NameAr` or `NameEn` |
| Evaluation | `warnings[].description` | `DescriptionAr` or `DescriptionEn` (DRUG_INTERACTION). It stays `null` for ALLERGY_CONFLICT. |
| ProblemDetails | `title`, `detail`, `errors.{field}[]` | Catalog (below) |
| Search (`search` query parameter, 003) | matched against | Patient `fullName` and the medication name **in the resolved language only** |

**Never localized**: `code` values, `status` and `requestStatus` values, ids, instants, numbers,
`patient.fullName`, `dosage`, and `decisionNote` (spec FR-004).

No response contains both language variants of a name or description (spec FR-021).

## Message catalog

The English column is today's text, unchanged. `{n}`, `{med}`, `{other}`, and `{max}` are
parameters. Numbers are formatted with the invariant culture (Western digits).

### Evaluation messages

| Code (kind) | English | Arabic |
|-------------|---------|--------|
| `TOO_EARLY` | Less than 80% of the {n}-day supply has elapsed since the last fill. | انقضى أقل من 80% من مدة الإمداد البالغة {n} يومًا منذ آخر صرف. |
| `NO_REFILLS_REMAINING` | No refills remain on this prescription. | لا توجد إعادات صرف متبقية لهذه الوصفة. |
| `INVALID_PRESCRIPTION_DATA` (InvalidPrescriptionData.DaysSupplyNotPositive) | The prescription's days supply is not a positive number. | مدة الإمداد في الوصفة ليست عددًا موجبًا. |
| `INVALID_PRESCRIPTION_DATA` (InvalidPrescriptionData.LastFilledMissing) | The prescription has no last filled time. | لا يوجد وقت آخر صرف لهذه الوصفة. |
| `INVALID_PRESCRIPTION_DATA` (InvalidPrescriptionData.LastFilledAfterEvaluation) | The prescription's last filled time is later than the evaluation time. | وقت آخر صرف للوصفة لاحق لوقت التقييم. |
| `ALLERGY_CONFLICT` | The patient has a recorded allergy to {med}. | لدى المريض حساسية مسجّلة تجاه {med}. |
| `DRUG_INTERACTION` | {med} has a synthetic demo interaction with active medication {other}. | يوجد تداخل تجريبي مُصطنع بين {med} والدواء النشط {other}. |

### Error texts

| `code` / field | English | Arabic |
|----------------|---------|--------|
| `validation_failed` title | The request is not valid. | الطلب غير صالح. |
| `errors.note` (Required) | A decision note is required. | ملاحظة القرار مطلوبة. |
| `errors.note` (TooLong) | The decision note must be at most {max} characters. | يجب ألا تتجاوز ملاحظة القرار {max} حرف. |
| `errors.prescriptionId` (inactive) | Prescription is inactive. | الوصفة غير نشطة. |
| `errors.prescriptionId` (missing) | A prescription id is required. | معرّف الوصفة مطلوب. |
| `errors.status` | Status must be Pending, Approved, or Denied. | يجب أن تكون الحالة Pending أو Approved أو Denied. |
| `errors.page` | Page must be a whole number of at least 1. | يجب أن يكون رقم الصفحة عددًا صحيحًا لا يقل عن 1. |
| `errors.pageSize` | Page size must be a whole number from 1 to {max}. | يجب أن يكون حجم الصفحة عددًا صحيحًا من 1 إلى {max}. |
| `errors.search` | Search must be at most {max} characters. | يجب ألا يتجاوز نص البحث {max} حرف. |
| `not_found` title (refill request) | The refill request was not found. | طلب إعادة الصرف غير موجود. |
| `not_found` title (prescription) | The prescription was not found. | الوصفة غير موجودة. |
| `request_already_decided` title | The request has already been decided. | سبق اتخاذ قرار بشأن هذا الطلب. |
| `request_already_decided` detail | Decided requests cannot be changed. | لا يمكن تغيير الطلبات التي صدر بشأنها قرار. |
| `pending_request_exists` title | The prescription already has a pending refill request. | يوجد بالفعل طلب إعادة صرف قيد الانتظار لهذه الوصفة. |
| `pending_request_exists` detail | Decide the existing request before creating another. | اتخذ قرارًا بشأن الطلب الحالي قبل إنشاء طلب آخر. |
| `concurrency_conflict` title | The request changed while your decision was being saved. | تغيّر الطلب أثناء حفظ قرارك. |
| `concurrency_conflict` detail | No changes were saved. Reload the request and decide again. | لم يُحفظ أي تغيير. أعد تحميل الطلب واتخذ القرار مرة أخرى. |
| `approval_blocked` title | Approval is blocked. | الموافقة محظورة. |
| `approval_blocked` detail | The request cannot be approved based on current data. It can still be denied. | لا يمكن الموافقة على الطلب بناءً على البيانات الحالية، ولا يزال بالإمكان رفضه. |
| `unexpected_error` title | An unexpected error occurred. | حدث خطأ غير متوقع. |
| Framework-produced 400 or 404 (malformed body, unknown route) | Unchanged framework title | 400: الطلب غير صالح. 404: المورد غير موجود. |

The Arabic wording must be reviewed by an Arabic-speaking reviewer before delivery (quickstart
V13). Changes from that review update this table and the catalog together.

## Example: `422 approval_blocked`, Arabic (seed scenario 6)

```http
POST /api/refill-requests/6/approve
Accept-Language: ar
Content-Type: application/json

{ "note": "Patient asked for an early refill." }
```

```http
HTTP/1.1 422 Unprocessable Entity
Content-Language: ar
Content-Type: application/problem+json
```

```json
{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "الموافقة محظورة.",
  "status": 422,
  "detail": "لا يمكن الموافقة على الطلب بناءً على البيانات الحالية، ولا يزال بالإمكان رفضه.",
  "code": "approval_blocked",
  "requestId": 6,
  "evaluation": {
    "evaluatedAt": "2026-10-21T09:00:00.0000000+00:00",
    "canApprove": false,
    "eligibleAt": "2026-10-27T09:00:00.0000000+00:00",
    "blockingReasons": [
      {
        "code": "TOO_EARLY",
        "message": "انقضى أقل من 80% من مدة الإمداد البالغة 30 يومًا منذ آخر صرف.",
        "eligibleAt": "2026-10-27T09:00:00.0000000+00:00"
      }
    ],
    "warnings": [
      {
        "code": "DRUG_INTERACTION",
        "message": "يوجد تداخل تجريبي مُصطنع بين هالفيكسين والدواء النشط جورفان.",
        "medicationName": "جورفان",
        "description": "تداخل تجريبي مُصطنع (خيالي، وليس إرشادًا سريريًا)."
      }
    ]
  }
}
```

The decision note in the request is sent and stored exactly as typed (spec FR-004). The same
request with `Accept-Language: en` returns the 001 example unchanged.

## Live updates

Unchanged: `RequestUpdated { requestId, change }`. There is no language field, and screens reload
in their own language (spec FR-003).
