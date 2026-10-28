using Lumina.Excel.Sheets;

namespace RotationSolver.TextureItems;

internal class StatusTexture(Status status) : ITexture
{
	private readonly Status _status = status;

	public uint IconID => _status.Icon;

	public StatusID ID => (StatusID)_status.RowId;

	public string Name => $"{_status.Name} ({_status.RowId})";

	public string Description => _status.Description.ExtractText() ?? string.Empty;

	public bool IsEnabled { get; set; } = true;

	public bool IsIntercepted { get; set; } = true;

	public bool IsRestrictedDOT { get; set; } = false;

	public StatusTexture(StatusID id)
		: this((uint)id)
	{
	}

	public StatusTexture(uint id)
	: this(Service.GetSheet<Status>().GetRow(id))
	{
	}
}