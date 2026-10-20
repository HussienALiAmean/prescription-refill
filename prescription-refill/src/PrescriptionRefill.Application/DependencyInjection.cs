using Microsoft.Extensions.DependencyInjection;
using PrescriptionRefill.Application.RefillRequests;
using PrescriptionRefill.Application.RefillRequests.Commands;
using PrescriptionRefill.Application.RefillRequests.Queries;

namespace PrescriptionRefill.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RefillDecisionContextLoader>();
        services.AddScoped<GetRefillRequests>();
        services.AddScoped<GetRefillRequestDetail>();
        services.AddScoped<ApproveRefillRequest>();
        services.AddScoped<DenyRefillRequest>();
        services.AddScoped<CreateRefillRequest>();
        return services;
    }
}
