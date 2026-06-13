namespace AxiomRPG.Core.Interfaces;

public interface IDataValidator<T>
{
    ValidationResult Validate(T data);
}

public record ValidationResult(bool IsValid, IReadOnlyList<ValidationError> Errors);

public record ValidationError(string Path, string Message, string? ExpectedType = null);
