namespace Domain.Shared.Kernel;

internal sealed class DomainException(string message, string? code = null) : Exception(message)
{
    public string? Code { get; } = code;
}
