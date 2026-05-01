namespace RotationSolver.Basic.Rotations.Basic;

public partial class BeastmasterRotation
{
	/// <summary>
	/// 
	/// </summary>
	public override MedicineType MedicineType => MedicineType.Strength;

	//private protected sealed override IBaseAction Raise => ;
	//private protected sealed override IBaseAction TankStance => ;

	#region Job Gauge

	#endregion

	#region Status Tracking

	#endregion

	#region Draw Debug

	/// <inheritdoc/>
	public override void DisplayBaseStatus()
	{
		ImGui.TextWrapped($"Beastmaster Data");
	}
	#endregion

	#region Actions

	#endregion
}
