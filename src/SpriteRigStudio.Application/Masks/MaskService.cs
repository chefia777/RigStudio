using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Masks;
using DomainValidation = SpriteRigStudio.Domain.Validation;

namespace SpriteRigStudio.Application.Masks;

/// <summary>
/// Application service for polygon mask management.
/// </summary>
public class MaskService
{
    /// <summary>
    /// Creates a new empty mask.
    /// </summary>
    public PolygonMaskDefinition CreateMask(string name)
    {
        return new PolygonMaskDefinition
        {
            MaskId = Guid.NewGuid(),
            Name = name
        };
    }

    /// <summary>
    /// Creates a rectangular mask.
    /// </summary>
    public PolygonMaskDefinition CreateRectangleMask(string name, double x, double y, double width, double height)
    {
        var mask = new PolygonMaskDefinition
        {
            MaskId = Guid.NewGuid(),
            Name = name
        };

        mask.OuterContour.Add(new Vector2D(x, y));
        mask.OuterContour.Add(new Vector2D(x + width, y));
        mask.OuterContour.Add(new Vector2D(x + width, y + height));
        mask.OuterContour.Add(new Vector2D(x, y + height));

        return mask;
    }

    /// <summary>
    /// Adds a vertex to the outer contour at the end.
    /// </summary>
    public Result AddVertex(PolygonMaskDefinition mask, Vector2D vertex)
    {
        mask.OuterContour.Add(vertex);
        return Result.Success();
    }

    /// <summary>
    /// Inserts a vertex on the edge between two existing vertices.
    /// </summary>
    public Result InsertVertex(PolygonMaskDefinition mask, int indexBefore, Vector2D vertex)
    {
        if (indexBefore < 0 || indexBefore >= mask.OuterContour.Count)
            return Result.Failure("INVALID_INDEX", "Vertex index out of range.");

        mask.OuterContour.Insert((indexBefore + 1) % mask.OuterContour.Count, vertex);
        return Result.Success();
    }

    /// <summary>
    /// Moves a vertex to a new position.
    /// </summary>
    public Result MoveVertex(PolygonMaskDefinition mask, int index, Vector2D newPosition)
    {
        if (index < 0 || index >= mask.OuterContour.Count)
            return Result.Failure("INVALID_INDEX", "Vertex index out of range.");

        mask.OuterContour[index] = newPosition;
        return Result.Success();
    }

    /// <summary>
    /// Removes a vertex from the outer contour.
    /// </summary>
    public Result RemoveVertex(PolygonMaskDefinition mask, int index)
    {
        if (mask.OuterContour.Count <= 3)
            return Result.Failure("TOO_FEW_VERTICES", "Cannot remove vertex: polygon must have at least 3 vertices.");

        if (index < 0 || index >= mask.OuterContour.Count)
            return Result.Failure("INVALID_INDEX", "Vertex index out of range.");

        mask.OuterContour.RemoveAt(index);
        return Result.Success();
    }

    /// <summary>
    /// Validates a mask polygon.
    /// </summary>
    public DomainValidation.ValidationResult ValidateMask(PolygonMaskDefinition mask)
    {
        var result = new DomainValidation.ValidationResult();

        if (mask.OuterContour.Count < 3)
            result.AddError("MASK_TOO_FEW_VERTICES", $"Mask '{mask.Name}' has fewer than 3 vertices.");

        if (mask.HasSelfIntersections())
            result.AddError("MASK_SELF_INTERSECTION", $"Mask '{mask.Name}' has self-intersecting edges.");

        if (mask.FeatherPixels < 0)
            result.AddWarning("MASK_NEGATIVE_FEATHER", $"Mask '{mask.Name}' has negative feather value.");

        return result;
    }

    /// <summary>
    /// Duplicates a mask.
    /// </summary>
    public PolygonMaskDefinition DuplicateMask(PolygonMaskDefinition source)
    {
        return source.Clone();
    }
}
