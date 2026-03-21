using Microsoft.Extensions.DependencyInjection;
using Product.Template.Core.Ai.Application.Agent;
using Product.Template.Core.Ai.Application.Agent.Tools;
using Product.Template.Kernel.Application.Ai;

namespace Product.Template.Core.Ai.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAiModule(this IServiceCollection services)
    {
        services.AddScoped<ToolRegistry>();
        services.AddScoped<AgentLoop>();

        services.AddScoped<ITool, GetUsersSummaryTool>();
        services.AddScoped<ITool, GetTenantInfoTool>();

        return services;
    }
}
