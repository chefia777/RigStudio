using SpriteRigStudio.Domain.Common;

namespace SpriteRigStudio.Domain.Validation;

/// <summary>
/// A single validation issue found during project, entity, or export validation.
/// </summary>
public class ValidationIssue
{
    /// <summary>Unique code identifying the issue type.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Severity level.</summary>
    public ValidationSeverity Severity { get; set; }

    /// <summary>Human-readable message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>ID of the entity with the issue (if applicable).</summary>
    public string? EntityId { get; set; }

    /// <summary>Property path within the entity (if applicable).</summary>
    public string? PropertyPath { get; set; }

    /// <summary>Suggested corrective action.</summary>
    public string? SuggestedAction { get; set; }

    public override string ToString() =>
        $"[{Severity}] {Code}: {Message}";
}
