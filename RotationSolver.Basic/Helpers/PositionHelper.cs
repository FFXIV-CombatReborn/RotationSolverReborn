using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using System.Numerics;

namespace RotationSolver.Basic.Helpers;

public static class PositionHelper
{
    /// <summary>
    /// Calculates the geometric centroid (center of mass) of the active party.
    /// Excludes dead members and members who are exceptionally far away (out of standard AoE range).
    /// </summary>
    /// <param name="maxDistanceYalms">The maximum distance a member can be from the player to be included in the calculation.</param>
    /// <returns>A Vector3 coordinate representing the optimal placement for an area-of-effect field.</returns>
    public static Vector3 GetPartyCentroid(float maxDistanceYalms = 30f)
    {
        if (Player.Object == null) return Vector3.Zero;

        var party = Svc.Party;
        if (party.Length == 0) return Player.Object.Position;

        Vector3 sum = Vector3.Zero;
        int count = 0;

        foreach (var member in party)
        {
            var chara = member.GameObject as IBattleChara;
            if (chara != null && !chara.IsDead)
            {
                if (Vector3.Distance(Player.Object.Position, chara.Position) <= maxDistanceYalms)
                {
                    sum += chara.Position;
                    count++;
                }
            }
        }

        if (count == 0) return Player.Object.Position;

        return sum / count;
    }
}
