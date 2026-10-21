using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrescriptionRefill.Application.Mapping;
using PrescriptionRefill.Application.RefillRequests;
using PrescriptionRefill.Application.RefillRequests.Dtos;
using PrescriptionRefill.Application.RefillRequests.Commands;
using PrescriptionRefill.Application.RefillRequests.Queries;

namespace PrescriptionRefill.Application;

public static class DependencyInjection
{
    /// <param name="autoMapperLicenseKey">
    /// Configuration <see cref="LocalizedProjections.LicenseKeySetting"/>, or null (spec 005 research R1).
    /// </param>
    public static IServiceCollection AddApplication(this IServiceCollection services, string? autoMapperLicenseKey)
    {
        services.AddSingleton(sp => new LocalizedProjections(autoMapperLicenseKey, sp.GetRequiredService<ILoggerFactory>()));
        services.AddScoped<LocalizedReferenceData>();
        services.AddScoped<LocalizedDtoBuilder>();
        services.AddScoped<RefillDecisionContextLoader>();
        services.AddScoped<GetRefillRequests>();
        services.AddScoped<GetRefillRequestDetail>();
        services.AddScoped<ApproveRefillRequest>();
        services.AddScoped<DenyRefillRequest>();
        services.AddScoped<CreateRefillRequest>();
        return services;
    }
}
