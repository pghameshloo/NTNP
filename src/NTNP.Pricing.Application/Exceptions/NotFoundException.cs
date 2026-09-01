namespace NTNP.Pricing.Application.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityType, object key) : base($"{entityType} '{key}' was not found.")
    {
    }
}
