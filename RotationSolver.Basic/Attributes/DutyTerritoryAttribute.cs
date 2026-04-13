namespace RotationSolver.Basic.Attributes;

/// <summary>
/// The duty territory attribute. which contains the 
/// </summary>
/// <param name="territoryIds"></param>
[AttributeUsage(AttributeTargets.Class)]
public class DutyTerritoryAttribute(params uint[] territoryIds) : Attribute
{
	/// <summary>
	/// The terriotry ids.
	/// </summary>
	public uint[] TerritoryIds => territoryIds;
}

