using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrescriptionRefill.Domain.RefillRequests;
using PrescriptionRefill.Infrastructure.Persistence;
using PrescriptionRefill.Infrastructure.Seeding;
using PrescriptionRefill.IntegrationTests.Infrastructure;
using static PrescriptionRefill.IntegrationTests.Infrastructure.Json;

namespace PrescriptionRefill.IntegrationTests.Api;

/// <summary>
/// Request language resolution and localized server text (spec 005 FR-002, FR-010, FR-011,
/// FR-021–FR-024; quickstart V1, V4, V5). Clients here send no default header, so each test
/// states its language.
/// </summary>
public sealed class LocalizationTests(SqlServerDatabaseFixture database)
    : IClassFixture<SqlServerDatabaseFixture>, IDisposable
{
    private readonly BeforeSaveInterceptor _interceptor = new();
    private readonly RefillApiFactory _api = new(database) { DefaultLanguage = null };
    private readonly TestData _data = new(database);

    private DateTimeOffset Now => _api.Clock.GetUtcNow();

    public void Dispose() => _api.Dispose();

    // ---- FR-010: resolution and Content-Language (V1) ---------------------------------------

    [Theory]
    [InlineData(null, "ar")]
    [InlineData("ar", "ar")]
    [InlineData("ar-SA", "ar")]
    [InlineData("en", "en")]
    [InlineData("en-GB", "en")]
    [InlineData("fr", "ar")]
    [InlineData("x;;", "ar")]
    [InlineData("*", "ar")]
    [InlineData("", "ar")]
    [InlineData("fr, en;q=0.8", "en")]
    [InlineData("en;q=0.5, ar;q=0.9", "ar")]
    public async Task Each_request_resolves_to_exactly_one_supported_language(string? header, string expected)
    {
        var arranged = await _data.ArrangeAsync(Now);

        var response = await SendAsync(HttpMethod.Get, $"/api/refill-requests/{arranged.RequestId}", header);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([expected], response.Content.Headers.ContentLanguage);
        var name = (await ReadAsync(response))["prescription"]!["medicationName"]!.GetValue<string>();
        Assert.Equal(expected == "ar" ? arranged.MedicationNameAr : arranged.MedicationName, name);
    }

    [Fact]
    public async Task Errors_carry_Content_Language_too()
    {
        var response = await SendAsync(HttpMethod.Get, "/api/refill-requests/999999", "en-GB");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(["en"], response.Content.Headers.ContentLanguage);
    }

    [Fact]
    public async Task List_ids_codes_order_and_flags_are_identical_in_both_languages()
    {
        await _data.ArrangeAsync(Now, remainingRefills: 0, interactingActive: true, requestedAgo: TimeSpan.FromHours(3));
        await _data.ArrangeAsync(Now, allergicToRequested: true, requestedAgo: TimeSpan.FromHours(2));

        var arabic = await ReadAsync(await SendAsync(HttpMethod.Get, "/api/refill-requests?page=1&pageSize=100", "ar"));
        var english = await ReadAsync(await SendAsync(HttpMethod.Get, "/api/refill-requests?page=1&pageSize=100", "en"));

        Assert.Equal(WithoutText(english).ToJsonString(), WithoutText(arabic).ToJsonString());
        Assert.NotEqual(english.ToJsonString(), arabic.ToJsonString());
    }

    // ---- V4: blocked approval in Arabic ------------------------------------------------------

    [Fact]
    public async Task Blocked_approval_is_worded_in_Arabic_with_the_same_codes_and_no_change()
    {
        var arranged = await _data.ArrangeAsync(Now, daysSupply: 30, sinceLastFill: TimeSpan.FromDays(10), interactingActive: true);
        var requestId = arranged.RequestId!.Value;

        var arabicResponse = await SendJsonAsync($"/api/refill-requests/{requestId}/approve", new { note = "Patient asked for an early refill." }, "ar");
        var englishResponse = await SendJsonAsync($"/api/refill-requests/{requestId}/approve", new { note = "Patient asked for an early refill." }, "en");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, arabicResponse.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, englishResponse.StatusCode);
        var arabic = await ReadAsync(arabicResponse);
        var english = await ReadAsync(englishResponse);

        Assert.Equal("approval_blocked", arabic["code"]!.GetValue<string>());
        Assert.Equal("الموافقة محظورة.", arabic["title"]!.GetValue<string>());
        Assert.Equal("لا يمكن الموافقة على الطلب بناءً على البيانات الحالية، ولا يزال بالإمكان رفضه.", arabic["detail"]!.GetValue<string>());

        var reason = arabic["evaluation"]!["blockingReasons"]![0]!;
        Assert.Equal("TOO_EARLY", reason["code"]!.GetValue<string>());
        Assert.Equal("انقضى أقل من 80% من مدة الإمداد البالغة 30 يومًا منذ آخر صرف.", reason["message"]!.GetValue<string>());

        var warning = arabic["evaluation"]!["warnings"]![0]!;
        Assert.Equal("DRUG_INTERACTION", warning["code"]!.GetValue<string>());
        var other = warning["medicationName"]!.GetValue<string>();
        Assert.StartsWith("دواء ", other);
        Assert.Equal($"يوجد تداخل تجريبي مُصطنع بين {arranged.MedicationNameAr} والدواء النشط {other}.", warning["message"]!.GetValue<string>());
        Assert.Equal(TestData.ArabicInteractionDescription, warning["description"]!.GetValue<string>());

        // Same machine-readable content; only text differs (FR-002).
        Assert.Equal(WithoutText(english).ToJsonString(), WithoutText(arabic).ToJsonString());

        var request = await _data.GetRequestAsync(requestId);
        Assert.Equal(RefillRequestStatus.Pending, request.Status);
        Assert.Equal(arranged.PrescriptionId, request.PrescriptionId);
    }

    // ---- V5: every error code in Arabic ------------------------------------------------------

    [Theory]
    [InlineData(null, "ملاحظة القرار مطلوبة.")]
    [InlineData("   ", "ملاحظة القرار مطلوبة.")]
    public async Task Invalid_note_is_worded_in_Arabic(string? note, string expected)
    {
        var arranged = await _data.ArrangeAsync(Now);

        var body = await AssertProblemAsync(
            await SendJsonAsync($"/api/refill-requests/{arranged.RequestId}/deny", new { note }, "ar"),
            HttpStatusCode.BadRequest, "validation_failed", "الطلب غير صالح.");

        Assert.Equal(expected, body["errors"]!["note"]![0]!.GetValue<string>());
    }

    [Fact]
    public async Task Too_long_note_is_worded_in_Arabic()
    {
        var arranged = await _data.ArrangeAsync(Now);

        var body = await AssertProblemAsync(
            await SendJsonAsync($"/api/refill-requests/{arranged.RequestId}/deny", new { note = new string('a', 1001) }, "ar"),
            HttpStatusCode.BadRequest, "validation_failed", "الطلب غير صالح.");

        Assert.Equal("يجب ألا تتجاوز ملاحظة القرار 1000 حرف.", body["errors"]!["note"]![0]!.GetValue<string>());
    }

    [Fact]
    public async Task Inactive_prescription_and_missing_id_are_worded_in_Arabic()
    {
        var inactive = await _data.ArrangeAsync(Now, isActive: false, withPendingRequest: false);

        var inactiveBody = await AssertProblemAsync(
            await SendJsonAsync("/api/refill-requests", new { prescriptionId = inactive.PrescriptionId }, "ar"),
            HttpStatusCode.BadRequest, "validation_failed", "الطلب غير صالح.");
        Assert.Equal("الوصفة غير نشطة.", inactiveBody["errors"]!["prescriptionId"]![0]!.GetValue<string>());

        var missingBody = await AssertProblemAsync(
            await SendJsonAsync("/api/refill-requests", new { }, "ar"),
            HttpStatusCode.BadRequest, "validation_failed", "الطلب غير صالح.");
        Assert.Equal("معرّف الوصفة مطلوب.", missingBody["errors"]!["prescriptionId"]![0]!.GetValue<string>());
    }

    [Theory]
    [InlineData("status=Open", "status", "يجب أن تكون الحالة Pending أو Approved أو Denied.")]
    [InlineData("page=0", "page", "يجب أن يكون رقم الصفحة عددًا صحيحًا لا يقل عن 1.")]
    [InlineData("pageSize=101", "pageSize", "يجب أن يكون حجم الصفحة عددًا صحيحًا من 1 إلى 100.")]
    public async Task List_validation_is_worded_in_Arabic(string queryString, string field, string expected)
    {
        var body = await AssertProblemAsync(
            await SendAsync(HttpMethod.Get, $"/api/refill-requests?{queryString}", "ar"),
            HttpStatusCode.BadRequest, "validation_failed", "الطلب غير صالح.");

        Assert.Equal(expected, body["errors"]![field]![0]!.GetValue<string>());
    }

    [Fact]
    public async Task Too_long_search_is_worded_in_Arabic()
    {
        var body = await AssertProblemAsync(
            await SendAsync(HttpMethod.Get, $"/api/refill-requests?search={new string('a', 101)}", "ar"),
            HttpStatusCode.BadRequest, "validation_failed", "الطلب غير صالح.");

        Assert.Equal("يجب ألا يتجاوز نص البحث 100 حرف.", body["errors"]!["search"]![0]!.GetValue<string>());
    }

    [Fact]
    public async Task Not_found_is_worded_in_Arabic()
    {
        await AssertProblemAsync(await SendAsync(HttpMethod.Get, "/api/refill-requests/999999", "ar"), HttpStatusCode.NotFound, "not_found", "طلب إعادة الصرف غير موجود.");
        await AssertProblemAsync(await SendJsonAsync("/api/refill-requests", new { prescriptionId = 999999 }, "ar"), HttpStatusCode.NotFound, "not_found", "الوصفة غير موجودة.");
    }

    [Fact]
    public async Task Already_decided_and_pending_exists_are_worded_in_Arabic()
    {
        var arranged = await _data.ArrangeAsync(Now);
        var requestId = arranged.RequestId!.Value;
        Assert.Equal(HttpStatusCode.OK, (await SendJsonAsync($"/api/refill-requests/{requestId}/deny", new { note = "Denied." }, "en")).StatusCode);

        var decided = await AssertProblemAsync(
            await SendJsonAsync($"/api/refill-requests/{requestId}/approve", new { note = "Again." }, "ar"),
            HttpStatusCode.Conflict, "request_already_decided", "سبق اتخاذ قرار بشأن هذا الطلب.");
        Assert.Equal("لا يمكن تغيير الطلبات التي صدر بشأنها قرار.", decided["detail"]!.GetValue<string>());
        Assert.Equal("Denied", decided["requestStatus"]!.GetValue<string>());

        var pending = await _data.ArrangeAsync(Now);
        var exists = await AssertProblemAsync(
            await SendJsonAsync("/api/refill-requests", new { prescriptionId = pending.PrescriptionId }, "ar"),
            HttpStatusCode.Conflict, "pending_request_exists", "يوجد بالفعل طلب إعادة صرف قيد الانتظار لهذه الوصفة.");
        Assert.Equal("اتخذ قرارًا بشأن الطلب الحالي قبل إنشاء طلب آخر.", exists["detail"]!.GetValue<string>());
    }

    [Fact]
    public async Task Concurrency_conflict_is_worded_in_Arabic_and_saves_nothing()
    {
        using var api = new RefillApiFactory(database)
        {
            DefaultLanguage = null,
            ConfigureServices = services =>
                services.ConfigureDbContext<RefillDbContext>(options => options.AddInterceptors(_interceptor)),
        };
        var arranged = await _data.ArrangeAsync(api.Clock.GetUtcNow(), remainingRefills: 2);
        _interceptor.RunOnceBeforeNextSave(() => _data.ExecuteSqlAsync(
            $"UPDATE Prescriptions SET RemainingRefills = 5 WHERE Id = {arranged.PrescriptionId}"));

        var response = await Send(api.CreateClient(), new HttpRequestMessage(HttpMethod.Post, $"/api/refill-requests/{arranged.RequestId}/approve")
        {
            Content = JsonContent.Create(new { note = "Approve." }),
        }, "ar");

        var body = await AssertProblemAsync(response, HttpStatusCode.Conflict, "concurrency_conflict", "تغيّر الطلب أثناء حفظ قرارك.");
        Assert.Equal("لم يُحفظ أي تغيير. أعد تحميل الطلب واتخذ القرار مرة أخرى.", body["detail"]!.GetValue<string>());
        Assert.Equal(RefillRequestStatus.Pending, (await _data.GetRequestAsync(arranged.RequestId!.Value)).Status);
        Assert.Equal(5, (await _data.GetPrescriptionAsync(arranged.PrescriptionId)).RemainingRefills);
    }

    [Fact]
    public async Task Framework_problems_are_titled_in_Arabic_and_unchanged_in_English()
    {
        var arranged = await _data.ArrangeAsync(Now);
        HttpRequestMessage Malformed() => new(HttpMethod.Post, $"/api/refill-requests/{arranged.RequestId}/deny")
        {
            Content = new StringContent("{ not json", Encoding.UTF8, "application/json"),
        };

        var arabic = await AssertProblemAsync(await Send(_api.CreateClient(), Malformed(), "ar"), HttpStatusCode.BadRequest, "validation_failed", "الطلب غير صالح.");
        var english = await ReadAsync(await Send(_api.CreateClient(), Malformed(), "en"));

        Assert.Equal(arabic["code"]!.GetValue<string>(), english["code"]!.GetValue<string>());
        Assert.NotEqual("الطلب غير صالح.", english["title"]!.GetValue<string>());

        await AssertProblemAsync(await SendAsync(HttpMethod.Get, "/api/not-a-route", "ar"), HttpStatusCode.NotFound, "not_found", "المورد غير موجود.");
    }

    // ---- FR-021, FR-023, FR-024: one language per response (SC-005) ---------------------------

    [Theory]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task Detail_carries_one_name_per_medication_and_one_description_in_the_requested_language(string language)
    {
        var arranged = await _data.ArrangeAsync(Now, allergicToRequested: true, interactingActive: true);

        var response = await SendAsync(HttpMethod.Get, $"/api/refill-requests/{arranged.RequestId}", language);
        var raw = await response.Content.ReadAsStringAsync();
        var body = JsonNode.Parse(raw)!;

        var own = language == "ar" ? arranged.MedicationNameAr : arranged.MedicationName;
        var other = language == "ar" ? arranged.MedicationName : arranged.MedicationNameAr;
        Assert.Equal(own, body["prescription"]!["medicationName"]!.GetValue<string>());
        Assert.Equal([own], body["patient"]!["allergies"]!.AsArray().Select(a => a!.GetValue<string>()));
        // The test's Arabic names embed the English name, so compare whole JSON string values.
        Assert.DoesNotContain($"\"{other}\"", raw);

        var warnings = body["evaluation"]!["warnings"]!.AsArray();
        var allergy = warnings.Single(w => w!["code"]!.GetValue<string>() == "ALLERGY_CONFLICT")!;
        Assert.Equal(own, allergy["medicationName"]!.GetValue<string>());
        Assert.Null(allergy["description"]);

        var interaction = warnings.Single(w => w!["code"]!.GetValue<string>() == "DRUG_INTERACTION")!;
        var activeName = body["activePrescriptions"]![0]!["medicationName"]!.GetValue<string>();
        Assert.Equal(activeName, interaction["medicationName"]!.GetValue<string>());
        Assert.Equal(language == "ar" ? TestData.ArabicInteractionDescription : "Synthetic test interaction.", interaction["description"]!.GetValue<string>());
        Assert.DoesNotContain(language == "ar" ? "Synthetic test interaction." : TestData.ArabicInteractionDescription, raw);
    }

    [Fact]
    public async Task Allergies_are_ordered_by_the_localized_name()
    {
        var arranged = await _data.ArrangeAsync(Now);
        await using (var db = _data.NewContext())
        {
            // Arabic order (ب before ج) is the reverse of the English order (Zeta after Alpha).
            var first = new Domain.Medications.Medication("ج-" + Guid.NewGuid().ToString("N")[..8], "Alpha-" + Guid.NewGuid().ToString("N")[..8]);
            var second = new Domain.Medications.Medication("ب-" + Guid.NewGuid().ToString("N")[..8], "Zeta-" + Guid.NewGuid().ToString("N")[..8]);
            db.Medications.AddRange(first, second);
            var patient = await db.Patients.Include(p => p.Allergies).SingleAsync(p => p.Id == arranged.PatientId);
            patient.AddAllergy(first);
            patient.AddAllergy(second);
            await db.SaveChangesAsync();
        }

        var arabic = (await ReadAsync(await SendAsync(HttpMethod.Get, $"/api/refill-requests/{arranged.RequestId}", "ar")))["patient"]!["allergies"]!.AsArray().Select(a => a!.GetValue<string>()).ToList();
        var english = (await ReadAsync(await SendAsync(HttpMethod.Get, $"/api/refill-requests/{arranged.RequestId}", "en")))["patient"]!["allergies"]!.AsArray().Select(a => a!.GetValue<string>()).ToList();

        Assert.Equal(arabic.Order(StringComparer.Ordinal), arabic);
        Assert.Equal(english.Order(StringComparer.Ordinal), english);
        Assert.StartsWith("ب-", arabic[0]);
        Assert.StartsWith("Alpha-", english[0]);
    }

    // ---- helpers -----------------------------------------------------------------------------

    // Human-readable fields, plus the per-request traceId, which differs between any two calls.
    private static readonly HashSet<string> TextFields = ["message", "title", "detail", "errors", "medicationName", "allergies", "description", "traceId"];

    /// <summary>A copy with every human-readable field removed (spec 005 SC-003).</summary>
    internal static JsonNode WithoutText(JsonNode node)
    {
        var copy = node.DeepClone();
        Strip(copy);
        return copy;

        static void Strip(JsonNode? current)
        {
            switch (current)
            {
                case JsonObject obj:
                    foreach (var key in obj.Select(p => p.Key).Where(TextFields.Contains).ToList())
                    {
                        obj.Remove(key);
                    }

                    foreach (var (_, child) in obj)
                    {
                        Strip(child);
                    }

                    break;
                case JsonArray array:
                    foreach (var child in array)
                    {
                        Strip(child);
                    }

                    break;
            }
        }
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? language) =>
        Send(_api.CreateClient(), new HttpRequestMessage(method, url), language);

    private Task<HttpResponseMessage> SendJsonAsync<T>(string url, T body, string? language) =>
        Send(_api.CreateClient(), new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) }, language);

    private static Task<HttpResponseMessage> Send(HttpClient client, HttpRequestMessage request, string? language)
    {
        if (language is not null)
        {
            // Unvalidated, so malformed values reach the server as sent.
            request.Headers.TryAddWithoutValidation("Accept-Language", language);
        }

        return client.SendAsync(request);
    }

    private static async Task<JsonNode> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code, string title)
    {
        Assert.Equal(status, response.StatusCode);
        var body = await ReadAsync(response);
        Assert.Equal(code, body["code"]!.GetValue<string>());
        Assert.Equal(title, body["title"]!.GetValue<string>());
        return body;
    }
}

/// <summary>
/// The seeded scenarios in both languages (spec 005 SC-003, quickstart V2): identical outcomes,
/// codes, statuses, and counts once human-readable text is removed; recorded text untouched.
/// </summary>
public sealed class LocalizationParityTests(SqlServerDatabaseFixture database)
    : IClassFixture<SqlServerDatabaseFixture>, IAsyncLifetime
{
    private readonly RefillApiFactory _api = new(database) { DefaultLanguage = null };

    public async Task InitializeAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
    }

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Every_seeded_request_is_identical_in_Arabic_and_English_apart_from_text()
    {
        var ids = new List<int>();
        foreach (var status in new[] { "Pending", "Approved", "Denied" })
        {
            var list = await GetAsync($"/api/refill-requests?status={status}", "en");
            ids.AddRange(list["items"]!.AsArray().Select(i => i!["id"]!.GetValue<int>()));
        }

        Assert.Equal(8, ids.Count);
        foreach (var id in ids)
        {
            var arabic = await GetAsync($"/api/refill-requests/{id}", "ar");
            var english = await GetAsync($"/api/refill-requests/{id}", "en");

            Assert.Equal(LocalizationTests.WithoutText(english).ToJsonString(), LocalizationTests.WithoutText(arabic).ToJsonString());
            Assert.Equal(english["patient"]!["fullName"]!.ToJsonString(), arabic["patient"]!["fullName"]!.ToJsonString());
            Assert.Equal(english["prescription"]!["dosage"]!.ToJsonString(), arabic["prescription"]!["dosage"]!.ToJsonString());
            Assert.Equal(english["decisionNote"]?.ToJsonString(), arabic["decisionNote"]?.ToJsonString());
            Assert.Equal(
                SeedNames.Medications.Single(n => n.En == english["prescription"]!["medicationName"]!.GetValue<string>()).Ar,
                arabic["prescription"]!["medicationName"]!.GetValue<string>());
        }
    }

    private async Task<JsonNode> GetAsync(string url, string language)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Accept-Language", language);
        var response = await _api.CreateClient().SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadAsync(response);
    }
}
