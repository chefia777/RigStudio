using System;
using System.Threading.Tasks;

namespace SpriteRigStudio.Desktop;

/// <summary>
/// Extension methods for fire-and-forget task execution.
/// </summary>
internal static class TaskExtensions
{
    /// <summary>
    /// Fires and forgets the task, suppressing all exceptions.
    /// </summary>
    public static async void FireAndForget(this Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch
        {
            // Suppress all exceptions for fire-and-forget tasks
        }
    }
}
