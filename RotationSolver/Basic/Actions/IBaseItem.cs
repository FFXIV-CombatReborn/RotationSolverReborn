namespace RotationSolver.Basic.Actions;

/// <summary>
/// The base item.
/// </summary>
public interface IBaseItem : IAction
{
	/// <summary>
	/// 
	/// </summary>
	uint A4 { get; set; }
	/// <summary>
	/// Has this item.
	/// </summary>
	bool HasIt { get; }
	/// <summary>
	/// The item can be used.
	/// </summary>
	/// <param name="item"></param>
	/// <param name="clippingCheck"></param>
	/// <returns></returns>
	bool CanUse(out IAction item, bool clippingCheck = false);
}
