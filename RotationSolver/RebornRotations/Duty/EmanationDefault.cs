using RotationSolver.Basic.Rotations.Duties;

namespace RotationSolver.RebornRotations.Duty;

[Rotation("Beauty's Wicked Wiles", CombatType.PvE)]

internal class EmanationDefault : EmanationRotation
{
    public override void DisplayDutyStatus()
    {
        ImGui.Spacing();
        ImGui.Text($"VrilPvE Slotted: {VrilPvE.Info.IsOnSlot}");
        ImGui.Text($"VrilPvE Charges: {VrilPvE.Cooldown.CurrentCharges}");
        ImGui.Spacing();
        ImGui.Text($"VrilPvE_9345 Slotted: {VrilPvE_9345.Info.IsOnSlot}");
        ImGui.Text($"VrilPvE_9345 Charges: {VrilPvE_9345.Cooldown.CurrentCharges}");
        ImGui.Spacing();
    }

    #region Configs
    [RotationConfig(CombatType.PvE, Name = "Auto Use Vril")]
    public bool AllowVril2 { get; set; } = true;
    #endregion

    public override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (AllowVril2)
        {
            if (VrilPvE.Cooldown.CurrentCharges > 0)
            {
                if (VrilPvE.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }

            if (VrilPvE_9345.Cooldown.CurrentCharges > 0)
            {
                if (VrilPvE_9345.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
        }

        return base.EmergencyAbility(nextGCD, out act);
    }
}