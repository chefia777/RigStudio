namespace SpriteRigStudio.Domain.Validation;

/// <summary>
/// Severity level of a validation issue.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>Informational message.</summary>
    Info = 0,

    /// <summary>Non-blocking warning.</summary>
    Warning,

    /// <summary>Blocking error.</summary>
    Error
}
