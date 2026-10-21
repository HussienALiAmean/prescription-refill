import type { RequestStatus } from '../../features/refill-requests/data/models';
import type { Messages } from './messages';

/**
 * Arabic counted phrase with Western digits (spec 005 FR-016): 1 and 2 have their own forms,
 * 3–10 take the plural, 11–99 the singular accusative, and 0 and 100+ the plain singular.
 */
function count(n: number, forms: { one: string; two: string; few: string; many: string; other: string }): string {
  if (n === 1) {
    return forms.one;
  }
  if (n === 2) {
    return forms.two;
  }
  const rest = n % 100;
  if (rest >= 3 && rest <= 10) {
    return `${n} ${forms.few}`;
  }
  if (rest >= 11 && rest <= 99) {
    return `${n} ${forms.many}`;
  }
  return `${n} ${forms.other}`;
}

const requests = { one: 'طلب واحد', two: 'طلبان', few: 'طلبات', many: 'طلبًا', other: 'طلب' };

const STATUS_PHRASE: Record<RequestStatus, string> = {
  Pending: 'قيد الانتظار',
  Approved: 'موافق عليها',
  Denied: 'مرفوضة',
};

/**
 * Arabic interface text, using the glossary in specs/005-bilingual-localization-themes/contracts/
 * ui-contract.md. Warnings never read as blocking, and "no issues found" never claims safety
 * (004 FR-015). Reviewed wording replaces these values (quickstart V13).
 */
export const ar: Messages = {
  app: {
    documentTitle: 'طلبات إعادة الصرف',
    pageTitles: { queue: 'طلبات إعادة الصرف', review: 'مراجعة طلب إعادة الصرف' },
    brandName: 'طلبات إعادة الصرف',
    brandTagline: 'مراجعة الطبيب',
    demoNotice:
      'بيانات تجريبية خيالية، وليست إرشادًا سريريًا. لا يوجد تسجيل دخول: يمكن لأي شخص لديه صلاحية الوصول اتخاذ قرارات بشأن الطلبات، ولا يصلح للاستخدام مع بيانات مرضى حقيقية.',
    preferences: 'تفضيلات العرض',
    language: 'اللغة',
    theme: 'المظهر',
    themeLight: 'فاتح',
    themeDark: 'داكن',
    languageChanged: 'تم التبديل إلى العربية',
    themeChanged: { light: 'تم تفعيل المظهر الفاتح', dark: 'تم تفعيل المظهر الداكن' },
  },
  connection: {
    offline: 'التحديثات المباشرة غير متاحة — استخدم «تحديث».',
    reconnecting: 'جارٍ إعادة الاتصال بالتحديثات المباشرة…',
  },
  status: { Pending: 'قيد الانتظار', Approved: 'موافق عليه', Denied: 'مرفوض' },
  common: {
    refresh: 'تحديث',
    refreshing: 'جارٍ التحديث…',
    requested: 'طُلب في',
    decided: 'صدر القرار في',
    approvalBlocked: 'الموافقة محظورة',
    warnings: 'تحذيرات',
    noIssuesFound: 'لم يُعثر على مشكلات',
    tryAgain: 'حاول مرة أخرى',
    somethingWentWrong: 'حدث خطأ ما',
  },
  statusFilter: {
    legend: 'عرض',
    requestsSuffix: 'طلبات',
  },
  queue: {
    eyebrowQueue: 'قائمة العمل',
    eyebrowHistory: 'السجل',
    headings: {
      Pending: 'طلبات إعادة الصرف قيد الانتظار',
      Approved: 'الطلبات الموافق عليها',
      Denied: 'الطلبات المرفوضة',
    },
    descriptions: {
      Pending: 'طلبات بانتظار قرار، الأقدم أولًا. افتح طلبًا للاطلاع على فحوصات السلامة واتخاذ القرار.',
      Approved: 'الطلبات الموافق عليها، الأحدث قرارًا أولًا. القرارات نهائية وللقراءة فقط.',
      Denied: 'الطلبات المرفوضة، الأحدث قرارًا أولًا. القرارات نهائية وللقراءة فقط.',
    },
    search: 'ابحث باسم المريض أو الدواء',
    loading: 'جارٍ تحميل الطلبات…',
    loadError: 'تعذّر تحميل الطلبات.',
    matches: (matching: number, total: number) => `${matching} من أصل ${count(total, requests)} مطابقة`,
    count: (total: number) => count(total, requests),
    checkedPrefix: 'تم التحقق مقابل البيانات الحالية في',
    checkedSuffix: '. افتح الطلب للاطلاع على أحدث تقييم.',
    summaryLabel: 'الطلبات قيد الانتظار حسب حالة السلامة',
    summaryBlocked: 'الموافقة محظورة',
    summaryWarnings: 'مع تحذيرات',
    summaryClear: 'لم يُعثر على مشكلات',
    emptyHeading: 'لا يوجد شيء هنا بعد',
    empty: (status: RequestStatus) => `لا توجد طلبات ${STATUS_PHRASE[status]}.`,
    noMatchHeading: 'لا توجد طلبات مطابقة',
    noMatch: (query: string) => `لا توجد طلبات تطابق «${query}».`,
    clearSearch: 'مسح البحث',
  },
  pagination: {
    label: 'ترقيم الصفحات',
    showing: 'عرض',
    of: 'من',
    rowsPerPage: 'عدد الصفوف في الصفحة',
    previous: 'السابق',
    next: 'التالي',
    position: (page: number, pageCount: number) => `الصفحة ${page} من ${pageCount}`,
  },
  review: {
    back: 'العودة إلى الطلبات',
    loading: 'جارٍ تحميل الطلب…',
    notFoundHeading: 'الطلب غير موجود',
    notFoundMessage: 'لم يُعثر على طلب إعادة الصرف هذا.',
    loadError: 'تعذّر تحميل الطلب.',
    eyebrow: (id: number) => `طلب إعادة الصرف رقم ${id}`,
    titlePrefix: 'طلب إعادة الصرف ·',
    notices: {
      approved: 'تمت الموافقة على الطلب.',
      denied: 'تم رفض الطلب.',
      blocked: 'الموافقة محظورة بناءً على البيانات الحالية. تم تحديث فحوصات السلامة أدناه. لا يزال بإمكانك رفض الطلب.',
      conflict:
        'تغيّر هذا الطلب أثناء حفظ قرارك. لم يُحفظ أي شيء. تم تحميل أحدث البيانات؛ راجعها واتخذ القرار مرة أخرى.',
      alreadyDecided: 'سبق اتخاذ قرار بشأن هذا الطلب. لم يتغير شيء.',
      notFound: 'هذا الطلب لم يعد موجودًا.',
      unconfirmed: 'تعذّر تأكيد القرار. حدّث الصفحة للاطلاع على الحالة الحالية قبل المحاولة مرة أخرى.',
      decidedElsewhere: 'صدر قرار بشأن هذا الطلب من مكان آخر. أصبح الآن للقراءة فقط.',
      refreshFailed: 'تعذّر تحميل أحدث البيانات. حاول التحديث مرة أخرى.',
    },
    invalidDecision: 'القرار غير صالح.',
  },
  evaluation: {
    heading: 'فحوصات السلامة',
    blocking: {
      TOO_EARLY: 'مبكر جدًا',
      NO_REFILLS_REMAINING: 'لا توجد إعادات صرف متبقية',
      INVALID_PRESCRIPTION_DATA: 'بيانات وصفة غير صالحة',
    },
    warning: {
      ALLERGY_CONFLICT: 'تعارض مع حساسية',
      DRUG_INTERACTION: 'تداخل دوائي',
    },
    preventsApproval: 'تمنع الموافقة. يبقى الرفض متاحًا.',
    eligibleFrom: 'مؤهل اعتبارًا من',
    warningsHeading: 'تحذيرات — تتطلب مراجعة سريرية، ولا تمنع الموافقة.',
    clearBody: 'لا تنطبق أي قاعدة مانعة. لا توجد تحذيرات حساسية أو تداخل. راجع التفاصيل قبل اتخاذ القرار.',
    noBlockingRules: 'لا تنطبق أي قاعدة مانعة.',
    noWarnings: 'لا توجد تحذيرات حساسية أو تداخل.',
    evaluatedPrefix: 'تم التقييم في',
    evaluatedSuffix: '. قد يكون قديمًا، ويُعاد التحقق من كل قاعدة على الخادم عند الموافقة.',
  },
  decision: {
    heading: 'القرار',
    noteLabel: 'ملاحظة القرار',
    noteHint: 'مطلوبة للموافقة والرفض، وتُحفظ مع القرار.',
    charactersSuffix: 'حرف',
    approveUnavailable: 'الموافقة غير متاحة لأن قاعدة مانعة تنطبق. لا يزال بإمكانك رفض الطلب.',
    approve: 'موافقة',
    approving: 'جارٍ الموافقة…',
    deny: 'رفض',
    denying: 'جارٍ الرفض…',
  },
  patient: {
    heading: 'المريض',
    dateOfBirth: 'تاريخ الميلاد',
    allergies: 'الحساسيات المسجلة',
    noAllergies: 'لا يوجد شيء مسجل',
    requestedPrescription: 'الوصفة المطلوبة',
    daysSupply: 'مدة الإمداد (بالأيام)',
    remainingRefills: 'إعادات الصرف المتبقية',
    lastFilled: 'آخر صرف',
    otherActive: 'الوصفات النشطة الأخرى',
    refills: (n: number) =>
      count(n, { one: 'إعادة صرف واحدة', two: 'إعادتا صرف', few: 'إعادات صرف', many: 'إعادة صرف', other: 'إعادة صرف' }),
    noOtherActive: 'لا توجد وصفات نشطة أخرى.',
  },
  decided: {
    heading: 'القرار',
    readOnly: 'للقراءة فقط',
    decided: 'تاريخ القرار',
    note: 'الملاحظة',
    readOnlyNotice: 'صدر قرار بشأن هذا الطلب وهو للقراءة فقط.',
  },
  time: {
    months: ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'],
  },
};
