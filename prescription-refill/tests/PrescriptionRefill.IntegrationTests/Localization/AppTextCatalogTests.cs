using System.Reflection;
using PrescriptionRefill.Application.Localization;
using PrescriptionRefill.Domain.Evaluation;

namespace PrescriptionRefill.IntegrationTests.Localization;

/// <summary>
/// The server message catalog (spec 005 FR-017, FR-018, research R5): every message exists in both
/// languages, English is byte-identical to the texts before feature 005, and numbers use Western
/// digits in both languages (FR-016).
/// </summary>
public sealed class AppTextCatalogTests
{
    private static readonly IAppText English = AppText.For(Language.English);
    private static readonly IAppText Arabic = AppText.For(Language.Arabic);

    /// <summary>Copied from the code before feature 005 (RefillEvaluator, RefillRequest, ProblemResults, endpoints, handler).</summary>
    public static TheoryData<string, string> EnglishTexts => new()
    {
        { English.TooEarly(30), "Less than 80% of the 30-day supply has elapsed since the last fill." },
        { English.NoRefillsRemaining, "No refills remain on this prescription." },
        { English.InvalidData(InvalidPrescriptionData.DaysSupplyNotPositive), "The prescription's days supply is not a positive number." },
        { English.InvalidData(InvalidPrescriptionData.LastFilledMissing), "The prescription has no last filled time." },
        { English.InvalidData(InvalidPrescriptionData.LastFilledAfterEvaluation), "The prescription's last filled time is later than the evaluation time." },
        { English.AllergyConflict("Elvadine"), "The patient has a recorded allergy to Elvadine." },
        { English.DrugInteraction("Halvexin", "Jurvane"), "Halvexin has a synthetic demo interaction with active medication Jurvane." },
        { English.ValidationTitle, "The request is not valid." },
        { English.NoteRequired, "A decision note is required." },
        { English.NoteTooLong(1000), "The decision note must be at most 1000 characters." },
        { English.PrescriptionInactive, "Prescription is inactive." },
        { English.PrescriptionIdRequired, "A prescription id is required." },
        { English.StatusInvalid, "Status must be Pending, Approved, or Denied." },
        { English.PageInvalid, "Page must be a whole number of at least 1." },
        { English.PageSizeInvalid(100), "Page size must be a whole number from 1 to 100." },
        { English.SearchTooLong(100), "Search must be at most 100 characters." },
        { English.RefillRequestNotFound, "The refill request was not found." },
        { English.PrescriptionNotFound, "The prescription was not found." },
        { English.AlreadyDecidedTitle, "The request has already been decided." },
        { English.AlreadyDecidedDetail, "Decided requests cannot be changed." },
        { English.PendingRequestExistsTitle, "The prescription already has a pending refill request." },
        { English.PendingRequestExistsDetail, "Decide the existing request before creating another." },
        { English.ConcurrencyConflictTitle, "The request changed while your decision was being saved." },
        { English.ConcurrencyConflictDetail, "No changes were saved. Reload the request and decide again." },
        { English.ApprovalBlockedTitle, "Approval is blocked." },
        { English.ApprovalBlockedDetail, "The request cannot be approved based on current data. It can still be denied." },
        { English.UnexpectedErrorTitle, "An unexpected error occurred." },
    };

    [Theory]
    [MemberData(nameof(EnglishTexts))]
    public void English_texts_are_unchanged(string actual, string expected) => Assert.Equal(expected, actual);

    [Fact]
    public void English_keeps_the_framework_titles()
    {
        Assert.Null(English.FrameworkProblemTitle(400));
        Assert.Null(English.FrameworkProblemTitle(404));
    }

    [Fact]
    public void Arabic_titles_framework_problems()
    {
        Assert.Equal("الطلب غير صالح.", Arabic.FrameworkProblemTitle(400));
        Assert.Equal("المورد غير موجود.", Arabic.FrameworkProblemTitle(404));
    }

    [Theory]
    [InlineData(Language.Arabic)]
    [InlineData(Language.English)]
    public void Every_member_returns_text(Language language)
    {
        var text = AppText.For(language);
        var members = typeof(IAppText).GetMembers().Where(m => m.MemberType is MemberTypes.Property or MemberTypes.Method);

        foreach (var member in members)
        {
            if (member is MethodInfo { IsSpecialName: true })
            {
                continue; // property getters are covered by their property
            }

            var value = member switch
            {
                PropertyInfo property => property.GetValue(text) as string,
                MethodInfo method when method.Name == nameof(IAppText.FrameworkProblemTitle) => text.FrameworkProblemTitle(400) ?? "framework",
                MethodInfo method => method.Invoke(text, method.GetParameters().Select(SampleArgument).ToArray()) as string,
                _ => null,
            };

            Assert.False(string.IsNullOrWhiteSpace(value), $"{language} {member.Name} is empty.");
        }
    }

    [Fact]
    public void Arabic_differs_from_English_and_uses_western_digits()
    {
        Assert.NotEqual(English.TooEarly(30), Arabic.TooEarly(30));
        Assert.Equal("انقضى أقل من 80% من مدة الإمداد البالغة 30 يومًا منذ آخر صرف.", Arabic.TooEarly(30));
        Assert.Contains("1000", Arabic.NoteTooLong(1000));
        Assert.Contains("100", Arabic.PageSizeInvalid(100));

        // Arabic-Indic (U+0660–U+0669) and extended (U+06F0–U+06F9) digits never appear.
        foreach (var value in new[] { Arabic.TooEarly(30), Arabic.NoteTooLong(1000), Arabic.PageSizeInvalid(100), Arabic.SearchTooLong(100) })
        {
            Assert.DoesNotContain(value, c => c is >= '٠' and <= '٩' or >= '۰' and <= '۹');
        }
    }

    private static object SampleArgument(ParameterInfo parameter) =>
        parameter.ParameterType == typeof(int) ? 30
        : parameter.ParameterType == typeof(string) ? "Avelorin"
        : parameter.ParameterType == typeof(InvalidPrescriptionData) ? InvalidPrescriptionData.LastFilledMissing
        : throw new InvalidOperationException($"No sample for {parameter.ParameterType}.");
}
