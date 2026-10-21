# UI Contract: Language, Direction, and Theme

Extends [004 contracts/ui-contract.md](../../004-creative-clinical-ui/contracts/ui-contract.md).
All preserved hooks, test ids, roles, and live regions from 002/004 stay as they are. In English,
every existing text stays verbatim.

## Document attributes

| Attribute | Values | Set by |
|-----------|--------|--------|
| `html[lang]` | `ar` \| `en` | Inline boot script, then `I18nService` |
| `html[dir]` | `rtl` (ar) \| `ltr` (en) | Inline boot script, then `I18nService` |
| `html[data-theme]` | `light` \| `dark` | Inline boot script, then `ThemeService` |
| `meta[name=color-scheme]` | `light` \| `dark` | Inline boot script, then `ThemeService`. This replaces 004's `only light`. |
| `document.title` | `طلبات إعادة الصرف` \| `Refill requests` | `I18nService` |

Static defaults in `index.html`: `lang="ar" dir="rtl" data-theme="light"`. Theme and direction
are applied before first paint (spec FR-028).

## Header controls

Both controls sit in the header next to the connection indicator, on every screen. At widths
below 768 px they wrap to a second header row. Each control is at least 44 × 44 px.

| Control | Pattern | Accessible name (ar / en) | Options (ar / en) |
|---------|---------|---------------------------|-------------------|
| Language | Radio group of 2, one tab stop, arrow keys move between options (same pattern as 002 FR-014a) | `اللغة` / `Language` | `العربية` (`lang="ar"`) and `English` (`lang="en"`), **always in their own language** |
| Theme | Radio group of 2, same pattern | `المظهر` / `Theme` | `فاتح` / `Light` (sun icon) and `داكن` / `Dark` (moon icon). The text label stays visible and the icons are decorative. |

- Selecting an option applies it at once. Focus stays on the selected option (spec FR-035).
- A polite live region announces the change in the newly active language:
  `تم التبديل إلى العربية` / `Switched to English`, and `تم تفعيل المظهر الداكن` /
  `Dark theme on` (and their Light equivalents).
- Test hooks: `data-testid="language-switch"` and `data-testid="theme-switch"`, with options
  `data-value="ar|en"` and `data-value="light|dark"`.

## Language change behavior

| Screen | On language change |
|--------|--------------------|
| Queue | Reloads the current page from the API in the new language. Status, search text, page, and page size are kept. Only the latest response is shown. |
| Review | Reloads the detail and evaluation in the new language. The typed note is kept. A decision in flight completes, and the page then reloads. |
| Both | Notices, headings, buttons, hints, states, alerts, accessible names, and live-region text switch immediately from the dictionary. |

## Direction rules

- Layout uses logical properties only. A physical `left`/`right` declaration is a defect, except
  in the ambient light's pointer coordinates.
- Mirrored icons (`directional`): back arrow, chevrons, and pagination Previous/Next.
  Not mirrored: info, warning, blocked, check, pill, sun, moon, refresh, search.
- Recorded or mixed text uses `dir="auto"` or `<bdi>`: patient name, dosage, decision note (both
  input and read-only), medication names, and allergy items. Instants render inside an LTR isolate
  around the time and offset.
- Arabic text uses `letter-spacing: 0`.

## Glossary

Every Arabic string uses these terms consistently.

| English | Arabic |
|---------|--------|
| Refill request(s) | طلب (طلبات) إعادة الصرف |
| Pending / Approved / Denied (labels only; values stay English) | قيد الانتظار / موافق عليه / مرفوض |
| Approve / Deny | موافقة / رفض |
| Approval blocked | الموافقة محظورة |
| Warning(s): do not block approval | تحذير (تحذيرات): لا تمنع الموافقة |
| No issues found | لم يُعثر على مشكلات (neutral, and never "آمن" / safe; 004 FR-015) |
| Allergy / Allergies | حساسية / الحساسيات |
| Drug interaction | تداخل دوائي |
| Medication | الدواء |
| Dosage | الجرعة |
| Days supply | مدة الإمداد (بالأيام) |
| Remaining refills | إعادات الصرف المتبقية |
| Last filled | آخر صرف |
| Eligible from | مؤهل اعتبارًا من |
| Decision note | ملاحظة القرار |
| Refresh | تحديث |
| Search | بحث |
| Evaluated at … may be out of date; re-checked when approving | تم التقييم في … قد يكون قديمًا، ويُعاد التحقق عند الموافقة |
| Live updates reconnecting / unavailable | جارٍ إعادة الاتصال بالتحديثات المباشرة / التحديثات المباشرة غير متاحة |
| Fictional demo data — not clinical guidance. No sign-in: anyone with access can decide requests; not for real patient data. | بيانات تجريبية خيالية، وليست إرشادًا سريريًا. لا يوجد تسجيل دخول: يمكن لأي شخص لديه صلاحية الوصول اتخاذ قرارات بشأن الطلبات، ولا يصلح للاستخدام مع بيانات مرضى حقيقية. |

## Theme rules

- Every color comes from a token. In Dark, clinical content stays on opaque `--color-surface`,
  and no clinical text sits on a gradient or translucent layer (spec FR-030).
- Each safety state keeps its label and icon in both themes (spec FR-032).
- Print uses the Light token set (spec FR-033). Forced colors follow the 004 rules in both themes.
