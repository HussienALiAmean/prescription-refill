using System.Globalization;
using PrescriptionRefill.Domain.Evaluation;

namespace PrescriptionRefill.Application.Localization;

/// <summary>
/// Arabic texts from the 005 HTTP contract's message catalog. Numbers use Western digits through
/// the invariant culture (FR-016). Changes after the Arabic wording review (quickstart V13) update
/// the contract table and this class together.
/// </summary>
internal sealed class ArabicText : IAppText
{
    public string TooEarly(int daysSupply) =>
        $"انقضى أقل من 80% من مدة الإمداد البالغة {Number(daysSupply)} يومًا منذ آخر صرف.";

    public string NoRefillsRemaining => "لا توجد إعادات صرف متبقية لهذه الوصفة.";

    public string InvalidData(InvalidPrescriptionData kind) => kind switch
    {
        InvalidPrescriptionData.DaysSupplyNotPositive => "مدة الإمداد في الوصفة ليست عددًا موجبًا.",
        InvalidPrescriptionData.LastFilledMissing => "لا يوجد وقت آخر صرف لهذه الوصفة.",
        InvalidPrescriptionData.LastFilledAfterEvaluation => "وقت آخر صرف للوصفة لاحق لوقت التقييم.",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    public string AllergyConflict(string medicationName) =>
        $"لدى المريض حساسية مسجّلة تجاه {medicationName}.";

    public string DrugInteraction(string medicationName, string otherMedicationName) =>
        $"يوجد تداخل تجريبي مُصطنع بين {medicationName} والدواء النشط {otherMedicationName}.";

    public string ValidationTitle => "الطلب غير صالح.";

    public string NoteRequired => "ملاحظة القرار مطلوبة.";

    public string NoteTooLong(int maxLength) => $"يجب ألا تتجاوز ملاحظة القرار {Number(maxLength)} حرف.";

    public string PrescriptionInactive => "الوصفة غير نشطة.";

    public string PrescriptionIdRequired => "معرّف الوصفة مطلوب.";

    public string StatusInvalid => "يجب أن تكون الحالة Pending أو Approved أو Denied.";

    public string PageInvalid => "يجب أن يكون رقم الصفحة عددًا صحيحًا لا يقل عن 1.";

    public string PageSizeInvalid(int maxPageSize) => $"يجب أن يكون حجم الصفحة عددًا صحيحًا من 1 إلى {Number(maxPageSize)}.";

    public string SearchTooLong(int maxLength) => $"يجب ألا يتجاوز نص البحث {Number(maxLength)} حرف.";

    public string RefillRequestNotFound => "طلب إعادة الصرف غير موجود.";

    public string PrescriptionNotFound => "الوصفة غير موجودة.";

    public string AlreadyDecidedTitle => "سبق اتخاذ قرار بشأن هذا الطلب.";

    public string AlreadyDecidedDetail => "لا يمكن تغيير الطلبات التي صدر بشأنها قرار.";

    public string PendingRequestExistsTitle => "يوجد بالفعل طلب إعادة صرف قيد الانتظار لهذه الوصفة.";

    public string PendingRequestExistsDetail => "اتخذ قرارًا بشأن الطلب الحالي قبل إنشاء طلب آخر.";

    public string ConcurrencyConflictTitle => "تغيّر الطلب أثناء حفظ قرارك.";

    public string ConcurrencyConflictDetail => "لم يُحفظ أي تغيير. أعد تحميل الطلب واتخذ القرار مرة أخرى.";

    public string ApprovalBlockedTitle => "الموافقة محظورة.";

    public string ApprovalBlockedDetail => "لا يمكن الموافقة على الطلب بناءً على البيانات الحالية، ولا يزال بالإمكان رفضه.";

    public string UnexpectedErrorTitle => "حدث خطأ غير متوقع.";

    public string? FrameworkProblemTitle(int statusCode) => statusCode switch
    {
        400 => "الطلب غير صالح.",
        404 => "المورد غير موجود.",
        _ => null,
    };

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
