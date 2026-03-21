namespace Product.Template.Kernel.Application.Ai;

public interface ITool
{
    ToolDefinition Definition { get; }
    Task<string> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default);
}
