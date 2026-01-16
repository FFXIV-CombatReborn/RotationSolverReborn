using ECommons.Logging;

namespace RotationSolver.Basic.Tweaks;

/// <summary>
/// Effective animation lock reduction tweak (a-la xivalex/noclippy).
/// The game handles instants and casted actions differently:
/// * instants: on action request (e.g. on the frame the action button is pressed), animation lock is set to 0.5 (or 0.35 for some specific actions); it then ticks down every frame
///   some time later (ping + server latency, typically 50-100ms if ping is good), we receive action effect packet - the packet contains action's animation lock (typically 0.6)
///   the game then updates animation lock (now equal to 0.5 minus time since request) to the packet data
///   so the 'effective' animation lock between action request and animation lock end is equal to action's animation lock + delay between request and response
///   this tweak reduces effective animation lock by either removing extra delay completely or clamping it to specified min/max values
/// * casts: on action request animation lock is not set (remains equal to 0), remaining cast time is set to action's cast time; remaining cast time then ticks down every frame
///   some time later (cast time minus approximately 0.5s, aka slidecast window), we receive action effect packet - the packet contains action's animation lock (typically 0.1)
///   the game then updates animation lock (still 0) to the packet data - however, since animation lock isn't ticking down while cast is in progress, there is no extra delay
///   this tweak does nothing for casts, since they already work correctly
/// The tweak also allows predicting the delay based on history (using exponential average).
/// </summary>
public sealed unsafe class AnimationLockTweak
{
    private float _lastReqInitialAnimLock;
    private uint _lastReqSequence = uint.MaxValue;

    private static float DelayMax => Math.Max(Service.Config.AnimationLockDelayMax2, 20) * 0.001f;
    private static float DelaySmoothing => Math.Clamp(Service.Config.AnimLockDelaySmoothing, 0.3f, 0.95f); // Exponential smoothing factor

    /// <summary>
    /// Gets the smoothed delay between client request and server response.
    /// </summary>
    public float DelayAverage { get; private set; } = 0.1f; // Smoothed delay between client request and server response

    /// <summary>
    /// Gets a conservative estimate of the delay to use for animation lock reduction.
    /// Returns <see cref="DelayMax"/> if animation lock delay removal is enabled; otherwise,
    /// returns 1.5 times the smoothed delay average, clamped to a maximum of 0.1 seconds.
    /// </summary>
    public float DelayEstimate => Service.Config.RemoveAnimationLockDelay ? DelayMax : Math.Min(DelayAverage * 1.5f, 0.1f); // Conservative estimate

    /// <summary>
    /// Record initial animation lock after action request
    /// </summary>
    /// <param name="expectedSequence">Expected sequence number for the action</param>
    /// <param name="initialAnimLock">Initial animation lock value</param>
    public void RecordRequest(uint expectedSequence, float initialAnimLock)
    {
        _lastReqInitialAnimLock = initialAnimLock;
        _lastReqSequence = expectedSequence;
    }
    
    /// <summary>
    /// Apply the tweak: calculate animation lock delay and determine how much animation lock should be reduced
    /// </summary>
    /// <param name="sequence">Action sequence number</param>
    /// <param name="gamePrevAnimLock">Previous animation lock from game</param>
    /// <param name="gameCurrAnimLock">Current animation lock from game</param>
    /// <param name="packetPrevAnimLock">Previous animation lock from packet</param>
    /// <param name="packetCurrAnimLock">Current animation lock from packet</param>
    /// <param name="delay">Output: calculated delay</param>
    /// <returns>Animation lock reduction amount</returns>
    public float Apply(uint sequence, float gamePrevAnimLock, float gameCurrAnimLock, float packetPrevAnimLock, float packetCurrAnimLock, out float delay)
    {
        delay = Math.Max(0, _lastReqInitialAnimLock - gamePrevAnimLock);
        
        if (_lastReqSequence != sequence && gameCurrAnimLock != gamePrevAnimLock)
        {
			if (Service.Config.InDebug)
			{
				PluginLog.Debug($"[AnimLockTweak] Animation lock updated by action with unexpected sequence ID #{sequence}: {gamePrevAnimLock:f3} -> {gameCurrAnimLock:f3}");
			}
        }
        
        float reduction = 0;
        
        if (_lastReqSequence == sequence && _lastReqInitialAnimLock > 0)
        {
            SanityCheck(packetPrevAnimLock, packetCurrAnimLock, gameCurrAnimLock);
            DelayAverage = Math.Clamp(delay * (1 - DelaySmoothing) + DelayAverage * DelaySmoothing, 0f, 0.5f); // Update the exponential average with bounds
            
            // The result will be subtracted from current animation lock (and thus from adjusted lock delay)
            reduction = Service.Config.RemoveAnimationLockDelay ? Math.Clamp(delay - DelayMax, 0, gameCurrAnimLock) : 0;
        }
        
        _lastReqInitialAnimLock = 0;
        _lastReqSequence = uint.MaxValue;
        return reduction;
    }

    /// <summary>
    /// Perform sanity check to detect conflicting plugins: disable the tweak if condition is false
    /// </summary>
    /// <param name="packetOriginalAnimLock">Original animation lock from packet</param>
    /// <param name="packetModifiedAnimLock">Modified animation lock from packet</param>
    /// <param name="gameCurrAnimLock">Current animation lock from game</param>
    private static void SanityCheck(float packetOriginalAnimLock, float packetModifiedAnimLock, float gameCurrAnimLock)
    {
        if (!Service.Config.RemoveAnimationLockDelay)
            return; // Tweak is disabled

        if (DataCenter.IsActivated())
            return;

        // If we don't have distinct packet data, skip this check
        if (packetOriginalAnimLock == packetModifiedAnimLock &&
            packetOriginalAnimLock == gameCurrAnimLock)
            return;

        // If the packet value appears to be aligned to 10ms increments, it's likely untouched and fine
        if ((packetOriginalAnimLock % 0.01f) is <= 0.0005f or >= 0.0095f)
            return;

        PluginLog.Warning($"[AnimLockTweak] Unexpected animation lock {packetOriginalAnimLock:f6} -> {packetModifiedAnimLock:f6} -> {gameCurrAnimLock:f6}, disabling animation lock tweak feature");

		// Log warning to chat
		if (Service.Config.InDebug)
		{
			PluginLog.Debug($"[RSR] Unexpected animation lock detected! Disabling animation lock reduction feature.");
		}

        // Temporarily disable the tweak (but don't save the config, in case this condition is temporary)
        Service.Config.RemoveAnimationLockDelay.Value = false;
    }
}
