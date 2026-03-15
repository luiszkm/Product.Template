namespace Product.Template.Kernel.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entityName, Guid entityId)
        : base($"{entityName} with ID '{entityId}' was not found.")
    {
    }
}

