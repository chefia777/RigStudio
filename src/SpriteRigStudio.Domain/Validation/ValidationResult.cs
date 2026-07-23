namespace SpriteRigStudio.Domain.Validation;

/// <summary>
/// Result of a validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>Whether validation passed without errors.</summary>
    public bool IsValid => !Errors.Any();

    /// <summary>Error-level issues.</summary>
    public List<ValidationIssue> Errors { get; init; } = new();

    /// <summary>Warning-level issues.</summary>
    public List<ValidationIssue> Warnings { get; init; } = new();

    /// <summary>Informational issues.</summary>
    public List<ValidationIssue> Info { get; init; } = new();

    /// <summary>All issues combined.</summary>
    public IEnumerable<ValidationIssue> All => Errors.Concat(Warnings).Concat(Info);

    public ValidationResult AddError(string code, string message, string? entityId = null, string? propertyPath = null, string? suggestedAction = null)
    {
        Errors.Add(new ValidationIssue
        {
            Code = code,
            Severity = ValidationSeverity.Error,
            Message = message,
            EntityId = entityId,
            PropertyPath = propertyPath,
            SuggestedAction = suggestedAction
        });
        return this;
    }

    public ValidationResult AddWarning(string code, string message, string? entityId = null, string? propertyPath = null, string? suggestedAction = null)
    {
        Warnings.Add(new ValidationIssue
        {
            Code = code,
            Severity = ValidationSeverity.Warning,
            Message = message,
            EntityId = entityId,
            PropertyPath = propertyPath,
            SuggestedAction = suggestedAction
        });
        return this;
    }

    public static ValidationResult Success() => new();
}
