using RotationSolver.Basic.Rotations.Duties;

namespace RotationSolver.RebornRotations.Duty;

[Rotation("Hardboiled Reborn", CombatType.PvE)]

internal class HardboiledDefault : HardboiledRotation
{
	public override void DisplayDutyStatus()
	{
		ImGui.Spacing();
		ImGui.Text($"ShootHardPvE Slotted: {ShootHardPvE.Info.IsOnSlot}");
		ImGui.Text($"ShootHardPvE Charges: {ShootHardPvE.Cooldown.CurrentCharges}");
		ImGui.Spacing();
		ImGui.Text($"ShootHarderPvE Slotted: {ShootHarderPvE.Info.IsOnSlot}");
		ImGui.Text($"ShootHarderPvE Charges: {ShootHarderPvE.Cooldown.CurrentCharges}");
		ImGui.Spacing();
		ImGui.Text($"SmokingGunPvE Slotted: {SmokingGunPvE.Info.IsOnSlot}");
		ImGui.Text($"SmokingGunPvE Charges: {SmokingGunPvE.Cooldown.CurrentCharges}");
		ImGui.Spacing();
		ImGui.Text($"PulpFissionPvE Slotted: {PulpFissionPvE.Info.IsOnSlot}");
		ImGui.Text($"PulpFissionPvE Charges: {PulpFissionPvE.Cooldown.CurrentCharges}");
		ImGui.Spacing();
		ImGui.Text($"MoltingPythonPvE Slotted: {MoltingPythonPvE.Info.IsOnSlot}");
		ImGui.Text($"MoltingPythonPvE Charges: {MoltingPythonPvE.Cooldown.CurrentCharges}");
		ImGui.Spacing();
		ImGui.Text($"NightStallionPvE Slotted: {NightStallionPvE.Info.IsOnSlot}");
		ImGui.Text($"NightStallionPvE Charges: {NightStallionPvE.Cooldown.CurrentCharges}");
		ImGui.Spacing();
		ImGui.Text($"ThickSkinPvE Slotted: {ThickSkinPvE.Info.IsOnSlot}");
		ImGui.Text($"ThickSkinPvE Charges: {ThickSkinPvE.Cooldown.CurrentCharges}");
		ImGui.Spacing();
		ImGui.Text($"TheShortGoodbyePvE Slotted: {TheShortGoodbyePvE.Info.IsOnSlot}");
		ImGui.Text($"TheShortGoodbyePvE Charges: {TheShortGoodbyePvE.Cooldown.CurrentCharges}");
		ImGui.Spacing();
	}

	public override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		if (DataCenter.IsLichCastingSpecialIndicator() || StatusHelper.PlayerHasStatus(false, StatusID.Bind_3776))
		{
			if (MoltingPythonPvE.CanUse(out act, usedUp: true, skipComboCheck: true))
			{
				return true;
			}
		}

		return base.EmergencyAbility(nextGCD, out act);
	}

	public override bool GeneralAbility(IAction nextGCD, out IAction? act)
	{
		if (NightStallionPvE.CanUse(out act))
		{
			return true;
		}

		return base.GeneralAbility(nextGCD, out act);
	}

	public override bool DefenseAreaGCD(out IAction? act)
	{
		if (ThickSkinPvE.CanUse(out act))
		{
			return true;
		}

		return base.HealSingleGCD(out act);
	}

	public override bool HealSingleGCD(out IAction? act)
	{
		if (ThickSkinPvE.CanUse(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		return base.HealSingleGCD(out act);
	}

	public override bool GeneralGCD(out IAction? act)
	{
		if (TheShortGoodbyePvE.CanUse(out act))
		{
			return true;
		}

		if (PulpFissionPvE.CanUse(out act, skipComboCheck: true))
		{
			return true;
		}

		if (SmokingGunPvE.Info.IsOnSlot)
		{
			if (SmokingGunPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (ShootHarderPvE.Info.IsOnSlot)
		{
			if (ShootHarderPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (ShootHardPvE.Info.IsOnSlot)
		{
			if (ShootHardPvE.CanUse(out act))
			{
				return true;
			}
		}

		return base.GeneralGCD(out act);
	}
}