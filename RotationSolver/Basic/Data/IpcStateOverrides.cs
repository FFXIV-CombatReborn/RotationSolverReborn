namespace RotationSolver.Basic.Data;

/// <summary>
/// Settings an IPC caller overrides for as long as the operating mode it requested stays active.
/// A null value keeps the user's own setting. None of these are ever written to the user's config.
/// </summary>
/// <param name="HostileType">Overrides the engage setting (HostileType).</param>
/// <param name="TargetFreely">Overrides TargetFreely.</param>
/// <param name="AutoOffAfterCombat">Overrides AutoOffAfterCombat.</param>
/// <param name="FriendlyPartyNpcHealRaise">Overrides FriendlyPartyNpcHealRaise3.</param>
public sealed record IpcStateOverrides(TargetHostileType? HostileType, bool? TargetFreely, bool? AutoOffAfterCombat, bool? FriendlyPartyNpcHealRaise);
