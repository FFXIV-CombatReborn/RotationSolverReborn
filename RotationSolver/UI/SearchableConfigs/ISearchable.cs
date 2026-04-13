namespace RotationSolver.UI.SearchableConfigs;

internal interface ISearchable
{
	JobFilter PvPFilter { get; set; }
	JobFilter PvEFilter { get; set; }
	CheckBoxSearch? Parent { get; set; }

	string SearchingKeys { get; }
	bool ShowInChild { get; }

	void Draw();
}