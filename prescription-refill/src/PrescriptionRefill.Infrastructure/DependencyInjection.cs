using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrescriptionRefill.Application.Abstractions;
using PrescriptionRefill.Infrastructure.Persistence;
using PrescriptionRefill.Infrastructure.Seeding;

namespace PrescriptionRefill.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // No EnableRetryOnFailure: conflicts must surface unchanged (research R5).
        services.AddDbContext<RefillDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IRefillDbContext>(provider => provider.GetRequiredService<RefillDbContext>());
        services.AddScoped<DemoDataSeeder>();

        return services;
    }
}
