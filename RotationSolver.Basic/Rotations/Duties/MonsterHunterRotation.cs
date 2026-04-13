namespace RotationSolver.Basic.Rotations.Duties;

/// <summary>
/// Represents a rotation for variant duties in the game.
/// </summary>
[DutyTerritory(761, 762, 1300, 1306)]
public abstract class MonsterHunterRotation : DutyRotation
{
}

public partial class DutyRotation
{
	/// <summary>
	/// Displays the rotation status on the window.
	/// </summary>
	public virtual void PhantomDisplayDutyStatus()
	{
		if (!DataCenter.IsInMonsterHunterDuty)
			return;

		ImGui.Spacing();
		ImGui.Text($"MegaPotionPvE Slotted: {MegaPotionPvE.Info.IsOnSlot}");
		ImGui.Text($"MegaPotionPvE Charges: {MegaPotionPvE.Cooldown.CurrentCharges}");
		ImGui.Spacing();
		ImGui.Text($"Rathalos Normal: {RathalosNormal}");
		ImGui.Text($"Rathalos EX: {RathalosEX}");
		ImGui.Spacing();
		ImGui.Spacing();
		ImGui.Text($"MegaPotionPvE_44247 Slotted: {MegaPotionPvE_44247.Info.IsOnSlot}");
		ImGui.Text($"MegaPotionPvE_44247 Charges: {MegaPotionPvE_44247.Cooldown.CurrentCharges}");
		ImGui.Spacing();
		ImGui.Text($"Arkveld Normal: {ArkveldNormal}");
		ImGui.Text($"Arkveld EX: {ArkveldEX}");
		ImGui.Spacing();
	}

	/// <summary>
	/// Modifies the settings for MegaPotionPvE.
	/// </summary>
	/// <param name="setting">The action setting to modify.</param>
	static partial void ModifyMegaPotionPvE(ref ActionSetting setting)
	{
		setting.TargetType = TargetType.Self;
		setting.IsFriendly = true;
		setting.StatusNeed = [StatusID.Scalebound];
	}

	/// <summary>
	/// Modifies the settings for MegaPotionPvE.
	/// </summary>
	/// <param name="setting">The action setting to modify.</param>
	static partial void ModifyMegaPotionPvE_44247(ref ActionSetting setting)
	{
		setting.TargetType = TargetType.Self;
		setting.IsFriendly = true;
	}
}