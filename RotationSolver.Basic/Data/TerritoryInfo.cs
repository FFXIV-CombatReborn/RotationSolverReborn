using Lumina.Excel.Sheets;

namespace RotationSolver.Basic.Data;

/// <summary>
/// Represents detailed information about a specific territory, including its name, identifier,
/// and various attributes such as PvP status and content finder details.
/// </summary>
public class TerritoryInfo
{
	/// <summary>
	/// Gets the name of the territory.
	/// </summary>
	/// <remarks>
	/// The name represents the display name of the territory, extracted and processed
	/// to provide a user-friendly format. It may include localization depending on the underlying data.
	/// </remarks>
	public string Name { get; private set; }

	/// <summary>
	/// Gets the unique identifier of the territory.
	/// </summary>
	/// <remarks>
	/// The identifier is a numerical value that uniquely represents the territory within the data context.
	/// It is derived from the underlying sheet row identifier and is used for internal referencing and data mapping.
	/// </remarks>
	public uint Id { get; private set; }

	/// <summary>
	/// Indicates whether the territory is designated as a PvP (Player vs Player) zone.
	/// </summary>
	/// <remarks>
	/// This property reflects the PvP status of the territory as defined in the game data. A value of true signifies
	/// that the territory supports or is specifically intended for PvP activities, while a false value indicates
	/// that the territory does not include PvP content.
	/// </remarks>
	public bool IsPvP { get; private set; }

	/// <summary>
	/// Gets the content finder name associated with the territory.
	/// </summary>
	/// <remarks>
	/// The content finder name represents a unique identifier or label used in the game's content finder system
	/// to categorize or display the content linked to the specific territory.
	/// </remarks>
	public string ContentFinderName { get; private set; }

	/// <summary>
	/// Indicates whether the territory is classified as a high-end duty.
	/// </summary>
	/// <remarks>
	/// This property reflects the high-end difficulty status of the associated content in the game,
	/// typically referring to challenging endgame activities or encounters.
	/// </remarks>
	public bool IsHighEndDuty { get; private set; }

	/// <summary>
	/// Gets the content type of the territory.
	/// </summary>
	/// <remarks>
	/// The content type categorizes the type of activities or gameplay
	/// associated with a specific territory. This property is derived from
	/// the content finder condition and provides information about the
	/// territory's primary function, such as whether it is related to a
	/// dungeon, raid, or open-world content.
	/// </remarks>
	public TerritoryContentType ContentType { get; private set; }

	/// <summary>
	/// Gets the identifier of the content finder icon associated with the territory.
	/// </summary>
	/// <remarks>
	/// The icon identifier corresponds to the visual representation used in-game for the content finder,
	/// providing a graphical indication of the type or status of the content associated with the territory.
	/// A default icon may be used if no specific icon is designated.
	/// </remarks>
	public uint ContentFinderIcon { get; private set; }

	/// <summary>
	/// Represents detailed information about a specific game territory.
	/// Contains attributes such as the territory's name, unique identifier, PvP status,
	/// content finder details, and whether it is classified as a high-end duty.
	/// </summary>
	public TerritoryInfo(TerritoryType sheetType)
	{
		Id = sheetType.RowId;
		Name = sheetType.PlaceName.Value.Name.ExtractText();
		IsPvP = sheetType.IsPvpZone;

		ContentFinderCondition contentFinder = sheetType.ContentFinderCondition.Value;
		ContentFinderName = contentFinder.Name.ExtractText();
		IsHighEndDuty = contentFinder.HighEndDuty;
		ContentType = (TerritoryContentType)contentFinder.ContentType.Value.RowId;
		ContentFinderIcon = contentFinder.Icon;
	}
}