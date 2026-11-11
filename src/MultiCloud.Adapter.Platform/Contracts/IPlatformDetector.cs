namespace MultiCloud.Adapter.Platform.Contracts;

/// <summary>
/// Works out which platform the application is running on, from the environment variables each platform sets.
/// </summary>
/// <remarks>
/// This is the one place in the solution that knows how each platform announces itself. Nothing else in the code
/// decides anything from the answer; it is only reported, on the home page, in a response header and on every log
/// line.
/// </remarks>
public interface IPlatformDetector
{
    /// <summary>
    /// Reads the environment and describes the platform. Each call reads the environment again.
    /// </summary>
    /// <returns>The description; a value the platform does not supply is absent.</returns>
    PlatformDescription Detect();
}
