namespace SpriteRigStudio.Domain.Animations;

/// <summary>
/// Determines how an animation behaves after its duration.
/// </summary>
public enum LoopMode
{
    /// <summary>Plays once and stops.</summary>
    Once = 0,

    /// <summary>Repeats from the beginning.</summary>
    Loop,

    /// <summary>Plays forward then backward repeatedly.</summary>
    PingPong,

    /// <summary>Plays once and holds the last frame.</summary>
    Clamp
}
