using ECommons.DalamudServices;

namespace RotationSolver.Basic.Data;

/// <summary>
/// Represents new VFX data.
/// </summary>
public readonly record struct VfxNewData
{
    /// <summary>
    /// Gets the object ID.
    /// </summary>
    public readonly ulong ObjectId;

    /// <summary>
    /// Gets the path.
    /// </summary>
    public readonly string Path;

    /// <summary>
    /// Gets the time when the VFX data was created.
    /// </summary>
    public readonly DateTime Time;

    /// <summary>
    /// Gets the estimated duration for the VFX (seconds).
    /// Stored as an estimation because the engine does not expose VFX lifetime directly.
    /// </summary>
    public readonly float Duration;

    /// <summary>
    /// Initializes a new instance of the <see cref="VfxNewData"/> struct.
    /// </summary>
    /// <param name="objectId">The object ID.</param>
    /// <param name="path">The path.</param>
    /// <param name="duration">Estimated duration for the VFX in seconds.</param>
    public VfxNewData(ulong objectId, string path, float duration)
    {
        ObjectId = objectId;
        Path = path;
        Time = DateTime.Now;
        Duration = duration;
	}

    /// <summary>
    /// Gets the duration since the VFX data was created.
    /// </summary>
    public TimeSpan TimeDuration => DateTime.Now - Time;

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return $"Object Effect: {Svc.Objects.SearchById(ObjectId)?.Name ?? "Object"}: {Path}, {TimeDuration.TotalSeconds}";
    }
}