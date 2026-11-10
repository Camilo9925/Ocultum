namespace Ocultum.Core.Errors;

public abstract class OcultumException : Exception
{
    protected OcultumException(string errorCode, string message, string? detail = null, Exception? inner = null)
        : base(message, inner)
    {
        ErrorCode = errorCode;
        Detail = detail;
    }

    public string ErrorCode { get; }

    public string? Detail { get; }

    public virtual IReadOnlyDictionary<string, object?> Extensions { get; } = new Dictionary<string, object?>();
}
