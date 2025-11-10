using Lumina.Excel.Sheets;

namespace RotationSolver.TextureItems;

internal class StatusTexture(Status status) : ITexture
{
    private readonly Status _status = status;

    /// <summary>
    /// Gets the icon ID associated with the texture.
    /// </summary>
    public uint IconID => _status.Icon;

    /// <summary>
    /// Gets the ID of the status.
    /// </summary>
    public StatusID ID => (StatusID)_status.RowId;

    /// <summary>
    /// Gets the name of the status.
    /// </summary>
    public string Name => $"{_status.Name} ({_status.RowId})";

    /// <summary>
    /// Gets the description of the status.
    /// </summary>
    public string Description => _status.Description.ExtractText() ?? string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the texture is enabled. IsIntercepted
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 
    /// </summary>
    public bool IsIntercepted { get; set; } = true;

    /// <summary>
    /// 
    /// </summary>
    public bool IsRestrictedDOT { get; set; } = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusTexture"/> class with the specified status ID.
    /// </summary>
    /// <param name="id">The ID of the status.</param>
    public StatusTexture(StatusID id)
        : this((uint)id)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusTexture"/> class with the specified status ID.
    /// </summary>
    /// <param name="id">The ID of the status.</param>
    public StatusTexture(uint id)
    : this(Service.GetSheet<Status>().GetRow(id))
    {
    }
}