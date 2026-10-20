using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace PrescriptionRefill.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the real API against the fixture's SQL Server database with a controllable clock.
/// </summary>
public sealed class RefillApiFactory(SqlServerDatabaseFixture database) : WebApplicationFactory<Program>
{
    public static readonly DateTimeOffset StartTime = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    public FakeTimeProvider Clock { get; } = new(StartTime);

    /// <summary>Optional extra service registrations (for example a recording notifier).</summary>
    public Action<IServiceCollection>? ConfigureServices { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" has no appsettings file, so the fixture's connection string is the only one.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:RefillDb", database.ConnectionString);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            ConfigureServices?.Invoke(services);
        });
    }
}
