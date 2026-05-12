using System.Collections.Frozen;
using ECommons.GameHelpers;
using RotationSolver.Basic.Helpers;

namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Per-frame builder for the set of allied <see cref="Dalamud.Game.ClientState.Objects.Types.IGameObject.GameObjectId"/>s
/// that the PvP scorer should treat as "worth peeling for". Members are:
/// <list type="bullet">
///   <item>The local player (always — peeling self is always valuable).</item>
///   <item>Any allied healer in <see cref="DataCenter.PartyMembers"/>.</item>
///   <item>Any ally below <see cref="ThreatLowHpRatio"/> health.</item>
/// </list>
///
/// <para>
/// Reads boundary-resolved framework state (<see cref="ECommons.GameHelpers.Player.Object"/>,
/// <see cref="DataCenter.PartyMembers"/>); no Dalamud SDK calls beyond property
/// access on already-resolved <see cref="IBattleChara"/> instances. Output is a
/// <see cref="FrozenSet{T}"/>, immutable and cheap to enumerate per candidate.
/// </para>
/// </summary>
public static class ThreatenedAllyState
{
    /// <summary>
    /// Allies at or below this health ratio (0.0 to 1.0 scale) are flagged as
    /// threatened. 30% is the spec-pinned threshold from Phase 3.
    /// </summary>
    public const float ThreatLowHpRatio = 0.30f;

    /// <summary>
    /// Build the threatened-ally id set from the current frame's
    /// <see cref="DataCenter.PartyMembers"/> snapshot. Returns
    /// <see cref="FrozenSet{T}.Empty"/> if the local player is unavailable.
    /// </summary>
    public static IReadOnlySet<ulong> BuildThreatenedAllyIds()
    {
        var player = Player.Object;
        if (player == null) return FrozenSet<ulong>.Empty;

        var ids = new HashSet<ulong> { player.GameObjectId };

        foreach (var member in DataCenter.PartyMembers)
        {
            if (member == null) continue;

            var classJob = member.ClassJob;
            if (classJob.RowId != 0 && classJob.Value.GetJobRole() == JobRole.Healer)
            {
                ids.Add(member.GameObjectId);
                continue;
            }

            var ratio = member.GetHealthRatio();
            if (ratio > 0f && ratio < ThreatLowHpRatio)
            {
                ids.Add(member.GameObjectId);
            }
        }

        return ids.ToFrozenSet();
    }
}
