using ECommons.ExcelServices;
using RotationSolver.Basic.Traits;
namespace RotationSolver.Basic.Rotations;

/// <summary>
/// Represents a custom rotation with various properties and methods.
/// </summary>
public interface ICustomRotation : ITexture
{
	/// <summary>
	/// Gets the average count of non-recommended members using this rotation.
	/// </summary>
	double AverageCountOfLastUsing { get; }

	/// <summary>
	/// Gets the maximum count of non-recommended members using this rotation.
	/// </summary>
	int MaxCountOfLastUsing { get; }

	/// <summary>
	/// Gets the average count of non-recommended members using this rotation during combat time.
	/// </summary>
	double AverageCountOfCombatTimeUsing { get; }

	/// <summary>
	/// Gets the maximum count of non-recommended members using this rotation during combat time.
	/// </summary>
	int MaxCountOfCombatTimeUsing { get; }

	/// <summary>
	/// Gets a value indicating whether to show the status on the formal page.
	/// </summary>
	bool ShowStatus { get; }

	/// <summary>
	/// Gets a value indicating whether this rotation has burst damage skills up.
	/// </summary>
	bool IsInBurstWindow { get; }

	/// <summary>
	/// Gets a value indicating whether this rotation is valid.
	/// </summary>
	bool IsValid { get; }

	/// <summary>
	/// Gets the reason why this rotation is not valid.
	/// </summary>
	string WhyNotValid { get; }

	/// <summary>
	/// Gets the job associated with this rotation.
	/// </summary>
	Job Job { get; }

	/// <summary>
	/// Gets the role associated with this rotation.
	/// </summary>
	JobRole Role { get; }

	internal IRotationConfigSet Configs { get; }

	/// <summary>
	/// Gets the type of medicine associated with this rotation.
	/// </summary>
	MedicineType MedicineType { get; }

	/// <summary>
	/// Gets all base actions associated with this rotation.
	/// </summary>
	IBaseAction[] AllBaseActions { get; }

	/// <summary>
	/// Gets all actions, including base and item actions, associated with this rotation.
	/// </summary>
	IAction[] AllActions { get; }

	/// <summary>
	/// Gets all traits associated with this rotation.
	/// </summary>
	IBaseTrait[] AllTraits { get; }

	/// <summary>
	/// Gets all boolean properties associated with this rotation.
	/// </summary>
	PropertyInfo[] AllBools { get; }

	/// <summary>
	/// Gets all byte or integer properties associated with this rotation.
	/// </summary>
	PropertyInfo[] AllBytesOrInt { get; }

	/// <summary>
	/// Gets all float properties associated with this rotation.
	/// </summary>
	PropertyInfo[] AllFloats { get; }


	internal IAction? ActionHealAreaGCD { get; }
	internal IAction? ActionHealAreaAbility { get; }
	internal IAction? ActionHealSingleGCD { get; }
	internal IAction? ActionHealSingleAbility { get; }
	internal IAction? ActionDefenseAreaGCD { get; }
	internal IAction? ActionDefenseAreaAbility { get; }
	internal IAction? ActionDefenseSingleGCD { get; }
	internal IAction? ActionDefenseSingleAbility { get; }
	internal IAction? ActionMoveForwardGCD { get; }
	internal IAction? ActionMoveForwardAbility { get; }
	internal IAction? ActionMoveBackAbility { get; }
	internal IAction? ActionSpeedAbility { get; }
	internal IAction? ActionDispelStancePositionalGCD { get; }
	internal IAction? ActionDispelStancePositionalAbility { get; }
	internal IAction? ActionRaiseShirkGCD { get; }
	internal IAction? ActionRaiseShirkAbility { get; }
	internal IAction? ActionAntiKnockbackAbility { get; }

	/// <summary>
	/// Tries to use this rotation.
	/// </summary>
	/// <param name="newAction">The next action.</param>
	/// <param name="gcdAction">The next GCD action.</param>
	/// <returns>True if the rotation was successfully invoked; otherwise, false.</returns>
	bool TryInvoke(out IAction? newAction, out IAction? gcdAction);

	/// <summary>
	/// Displays the rotation status on the window.
	/// </summary>
	void DisplayRotationStatus();

	/// <summary>
	/// Displays the rotation status on the window.
	/// </summary>
	void DisplayBaseStatus();

	/// <summary>
	/// Occurs when the territory changes or the rotation changes.
	/// </summary>
	void OnTerritoryChanged();

	/// <summary>
	/// Creates a system warning to display to the end-user.
	/// </summary>
	/// <param name="warning">The warning to display.</param>
	void CreateSystemWarning(string warning);
}
