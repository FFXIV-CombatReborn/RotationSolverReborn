namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Per-call detector for the enemy crystal carrier in Crystalline Conflict.
///
/// <para>
/// Detection strategy: status-based. The crystal in CC applies a known buff/status to its
/// current carrier. <see cref="GetCurrentCarrierId"/> scans <see cref="DataCenter.AllHostileTargets"/>
/// for the first target whose status list contains <see cref="CarrierStatusId"/>.
/// </para>
///
/// <para>
/// <b>Status ID is unverified at first ship.</b> The constant below is zero, which produces a
/// graceful null result (the scorer's <c>CrystalCarrierFactor</c> contributes 0). Task 7 of the
/// Phase 2 plan instructs the user to observe a real CC match, identify the carrier buff, and
/// replace this constant with the corresponding <see cref="StatusID"/> enum value.
/// </para>
///
/// <para>
/// Pure within the call: reads two already-resolved framework collections
/// (<see cref="DataCenter.IsInCrystallineConflict"/> and <see cref="DataCenter.AllHostileTargets"/>);
/// no Dalamud SDK calls or sheet lookups.
/// </para>
/// </summary>
public static class CrystalCarrierState
{
    /// <summary>
    /// Status ID applied to the player currently holding the crystal in Crystalline Conflict.
    /// Zero until verified in-game (see Phase 2 plan, Task 7). The factor degrades to a
    /// zero contribution when this value is zero.
    /// </summary>
    public const StatusID CarrierStatusId = (StatusID)0;

    /// <summary>
    /// Return the <see cref="Dalamud.Game.ClientState.Objects.Types.IGameObject.GameObjectId"/>
    /// of the enemy carrier, or <c>null</c> if not in CC, if the carrier status is unverified,
    /// or if no current hostile target holds the status.
    /// </summary>
    public static ulong? GetCurrentCarrierId()
    {
        if (!DataCenter.IsInCrystallineConflict) return null;
        if (CarrierStatusId == (StatusID)0) return null;

        foreach (var target in DataCenter.AllHostileTargets)
        {
            var statusList = target.StatusList;
            if (statusList == null) continue;
            foreach (var status in statusList)
            {
                if ((StatusID)status.StatusId == CarrierStatusId)
                {
                    return target.GameObjectId;
                }
            }
        }
        return null;
    }
}
