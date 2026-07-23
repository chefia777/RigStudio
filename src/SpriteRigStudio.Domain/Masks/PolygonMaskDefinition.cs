using SpriteRigStudio.Domain.Geometry;

namespace SpriteRigStudio.Domain.Masks;

/// <summary>
/// Defines a polygon mask used to cut a sprite part from flattened artwork.
/// </summary>
public class PolygonMaskDefinition
{
    /// <summary>Stable identifier for this mask.</summary>
    public Guid MaskId { get; init; } = Guid.NewGuid();

    /// <summary>Display name for this mask.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The outer contour vertices in source-image pixel space.</summary>
    public List<Vector2D> OuterContour { get; init; } = new();

    /// <summary>Inner contours (holes) in the mask.</summary>
    public List<List<Vector2D>> InnerContours { get; init; } = new();

    /// <summary>Expansion in pixels (positive = expand, negative = contract).</summary>
    public double ExpansionPixels { get; set; }

    /// <summary>Feather/blur radius in pixels (0 = hard edge).</summary>
    public double FeatherPixels { get; set; }

    /// <summary>Whether this mask is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Whether this mask is visible in the editor.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Whether this mask is locked.</summary>
    public bool Locked { get; set; }

    /// <summary>Vertex count of the outer contour.</summary>
    public int VertexCount => OuterContour.Count;

    /// <summary>
    /// Validates that the polygon has at least 3 vertices and is not self-intersecting.
    /// </summary>
    public bool IsValid()
    {
        if (OuterContour.Count < 3) return false;
        return !HasSelfIntersections();
    }

    /// <summary>
    /// Returns true if the outer contour has self-intersecting edges.
    /// </summary>
    public bool HasSelfIntersections()
    {
        // Simple O(n^2) segment intersection check
        for (int i = 0; i < OuterContour.Count; i++)
        {
            var a1 = OuterContour[i];
            var a2 = OuterContour[(i + 1) % OuterContour.Count];

            for (int j = i + 2; j < OuterContour.Count; j++)
            {
                if (i == 0 && j == OuterContour.Count - 1) continue;
                var b1 = OuterContour[j];
                var b2 = OuterContour[(j + 1) % OuterContour.Count];

                if (SegmentsIntersect(a1, a2, b1, b2))
                    return true;
            }
        }
        return false;
    }

    private static bool SegmentsIntersect(Vector2D p1, Vector2D p2, Vector2D q1, Vector2D q2)
    {
        var d1 = (q2 - q1).Cross(p1 - q1);
        var d2 = (q2 - q1).Cross(p2 - q1);
        if ((d1 > 0 && d2 > 0) || (d1 < 0 && d2 < 0)) return false;

        var d3 = (p2 - p1).Cross(q1 - p1);
        var d4 = (p2 - p1).Cross(q2 - p1);
        if ((d3 > 0 && d4 > 0) || (d3 < 0 && d4 < 0)) return false;

        return true;
    }

    /// <summary>
    /// Creates a copy of this mask.
    /// </summary>
    public PolygonMaskDefinition Clone() => new()
    {
        MaskId = Guid.NewGuid(),
        Name = Name + " (copy)",
        OuterContour = new List<Vector2D>(OuterContour),
        InnerContours = InnerContours.Select(c => new List<Vector2D>(c)).ToList(),
        ExpansionPixels = ExpansionPixels,
        FeatherPixels = FeatherPixels,
        Enabled = Enabled,
        Visible = Visible,
        Locked = false
    };
}
