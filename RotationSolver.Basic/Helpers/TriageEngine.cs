using Dalamud.Game.ClientState.Objects.Types;
using RotationSolver.Basic.Configuration;

namespace RotationSolver.Basic.Helpers;

public static class TriageEngine
{
    /// <summary>
    /// Calculates an absolute risk score for a party member based on their HP, incoming mechanics, and status debuffs.
    /// Higher score = Higher priority for emergency healing or specific triage abilities.
    /// </summary>
    public static float CalculateRiskScore(IBattleChara member)
    {
        if (member == null || member.IsDead) return 0f;

        float score = 0f;

        // Base risk is inversely proportional to HP
        float hpRatio = member.GetHealthRatio();
        score += (1f - hpRatio) * 100f; // Max 100 points for being at low HP

        // Massive penalty if they have Doom
        if (member.HasStatus(false, StatusID.Doom, StatusID.Doom_1769) || member.DoomNeedHealing())
        {
            score += 200f;
        }

        // Heavy penalty for Weakness/Brink of Death (makes them highly susceptible to raid-wides)
        if (member.HasStatus(false, StatusID.Weakness, StatusID.BrinkOfDeath))
        {
            score += 50f;
        }

        // Extremely high priority if Walking Dead triggered
        if (member.HasStatus(false, StatusID.WalkingDead))
        {
            score += 300f;
        }

        // Contextual risk: If a Tankbuster is targeting them and they are low, critical risk
        if (DamagePredictor.IsTankbusterPredictable(member))
        {
            if (hpRatio < 0.6f)
            {
                score += 150f;
            }
            else
            {
                score += 50f;
            }
        }

        return score;
    }

    /// <summary>
    /// Evaluates if the target is in a critical triage state where they absolutely must be prioritized over all standard rotation logic.
    /// </summary>
    public static bool IsCriticalTriage(IBattleChara member)
    {
        return CalculateRiskScore(member) >= 150f;
    }
}
