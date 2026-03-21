namespace Product.Template.Kernel.Application.Ai;

public interface ITool
{
    ToolDefinition Definition { get; }
    Task<string> ExecuteAsync(ToolCall toolCall, CancellationToken cancellationToken = default);
}
