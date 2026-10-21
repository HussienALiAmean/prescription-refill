using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace PrescriptionRefill.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the real API against the fixture's SQL Server database with a controllable clock.
/// </summary>
public sealed class RefillApiFactory(SqlServerDatabaseFixture database) : WebApplicationFactory<Program>
{
    public static readonly DateTimeOffset StartTime = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    /// <summary>AutoMapper's missing-license warnings; tests run without a key (license 4.4).</summary>
    public const string AutoMapperLicenseCategory = "LuckyPennySoftware.AutoMapper.License";

    public FakeTimeProvider Clock { get; } = new(StartTime);

    /// <summary>Optional extra service registrations (for example a recording notifier).</summary>
    public Action<IServiceCollection>? ConfigureServices { get; init; }

    /// <summary>
    /// <c>Accept-Language</c> sent by every client this factory creates. It defaults to English
    /// so the assertions written before feature 005 keep their meaning, because the API's default
    /// language is now Arabic (spec 005 SC-010). Set it to <c>null</c> to send no header, as the
    /// language-resolution tests do.
    /// </summary>
    public string? DefaultLanguage { get; init; } = "en";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" has no appsettings file, so the fixture's connection string is the only one.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:RefillDb", database.ConnectionString);
        builder.ConfigureLogging(logging => logging.AddFilter(AutoMapperLicenseCategory, LogLevel.None));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            ConfigureServices?.Invoke(services);
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        if (DefaultLanguage is not null)
        {
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(DefaultLanguage));
        }
    }
}
