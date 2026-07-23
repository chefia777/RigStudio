using SpriteRigStudio.Domain.Geometry;

namespace SpriteRigStudio.Domain.Retargeting;

/// <summary>
/// Solves two-bone inverse kinematics for chains like upper arm → forearm → hand
/// or thigh → lower leg → foot.
/// Uses analytic (trigonometric) solution.
/// </summary>
public static class TwoBoneIKSolver
{
    /// <summary>
    /// Solves IK for a two-bone chain.
    /// </summary>
    /// <param name="upperLength">Length of the upper bone (e.g., upper arm).</param>
    /// <param name="lowerLength">Length of the lower bone (e.g., forearm).</param>
    /// <param name="target">Target position in local space of the chain root.</param>
    /// <param name="bendDirection">Preferred bend direction in degrees (positive = clockwise).</param>
    /// <returns>The IK result with angles relative to the parent bone direction.</returns>
    public static TwoBoneIKResult Solve(
        double upperLength,
        double lowerLength,
        Vector2D target,
        double bendDirection = -90)
    {
        var result = new TwoBoneIKResult();
        var dist = target.Length;

        // Check if target is reachable
        if (dist > upperLength + lowerLength)
        {
            // Target too far — stretch to max reach
            result.UpperAngleDegrees = target.Angle();
            result.LowerAngleDegrees = 0;
            result.Reached = false;
            return result;
        }

        if (dist < Math.Abs(upperLength - lowerLength))
        {
            // Target too close — fold completely
            result.UpperAngleDegrees = target.Angle();
            result.LowerAngleDegrees = 180;
            result.Reached = false;
            return result;
        }

        // Law of cosines
        var cosUpper = (upperLength * upperLength + dist * dist - lowerLength * lowerLength) / (2 * upperLength * dist);
        var upperAngle = Math.Acos(Math.Clamp(cosUpper, -1, 1));

        var cosLower = (upperLength * upperLength + lowerLength * lowerLength - dist * dist) / (2 * upperLength * lowerLength);
        var lowerAngle = Math.Acos(Math.Clamp(cosLower, -1, 1));

        var baseAngle = target.Angle();
        var bendRad = bendDirection * Math.PI / 180.0;

        result.UpperAngleDegrees = (baseAngle + bendRad * Math.Sign(upperAngle) * upperAngle) * 180.0 / Math.PI;
        result.LowerAngleDegrees = (lowerAngle - Math.PI) * 180.0 / Math.PI; // elbow bend
        result.Reached = true;
        return result;
    }
}
