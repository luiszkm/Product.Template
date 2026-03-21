using System.Text.Json;
using System.Text.Json.Nodes;
using MediatR;
using Product.Template.Core.Identity.Application.Queries.User;
using Product.Template.Kernel.Application.Ai;

namespace Product.Template.Core.Ai.Application.Agent.Tools;

public sealed class GetUsersSummaryTool : ITool
{
    private readonly IMediator _mediator;

    public GetUsersSummaryTool(IMediator mediator) => _mediator = mediator;

    public ToolDefinition Definition { get; } = new(
        Name: "get_users_summary",
        Description: "Returns a summary of registered users in the system, including total count and recent registrations.",
        InputSchema: new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["page_size"] = new JsonObject
                {
                    ["type"] = "integer",
                    ["description"] = "Number of users to retrieve (default: 10, max: 50)"
                }
            },
            ["required"] = new JsonArray()
        }
    );

    public async Task<string> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default)
    {
        var pageSize = call.Parameters["page_size"]?.GetValue<int>() ?? 10;
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = new ListUserQuery { PageSize = pageSize, PageNumber = 1 };
        var result = await _mediator.Send(query, cancellationToken);

        var summary = new
        {
            total_count = result.TotalCount,
            page_size = result.PageSize,
            users = result.Data.Select(u => new
            {
                id = u.Id,
                email = u.Email,
                name = $"{u.FirstName} {u.LastName}".Trim(),
                email_confirmed = u.EmailConfirmed,
                created_at = u.CreatedAt
            })
        };

        return JsonSerializer.Serialize(summary);
    }
}
