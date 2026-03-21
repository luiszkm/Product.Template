using System.Text.Json;
using System.Text.Json.Nodes;
using MediatR;
using Product.Template.Core.Tenants.Application.Queries;
using Product.Template.Kernel.Application.Ai;

namespace Product.Template.Core.Ai.Application.Agent.Tools;

public sealed class GetTenantInfoTool : ITool
{
    private readonly IMediator _mediator;

    public GetTenantInfoTool(IMediator mediator) => _mediator = mediator;

    public ToolDefinition Definition { get; } = new(
        Name: "get_tenant_info",
        Description: "Returns information about the tenants configured in the system.",
        InputSchema: new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["page_size"] = new JsonObject
                {
                    ["type"] = "integer",
                    ["description"] = "Number of tenants to retrieve (default: 20)"
                }
            },
            ["required"] = new JsonArray()
        }
    );

    public async Task<string> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default)
    {
        var pageSize = call.Parameters["page_size"]?.GetValue<int>() ?? 20;
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = new ListTenantsQuery(PageNumber: 1, PageSize: pageSize);
        var result = await _mediator.Send(query, cancellationToken);

        var summary = new
        {
            total_count = result.TotalCount,
            tenants = result.Data.Select(t => new
            {
                tenant_id = t.TenantId,
                tenant_key = t.TenantKey,
                isolation_mode = t.IsolationMode,
                is_active = t.IsActive
            })
        };

        return JsonSerializer.Serialize(summary);
    }
}
