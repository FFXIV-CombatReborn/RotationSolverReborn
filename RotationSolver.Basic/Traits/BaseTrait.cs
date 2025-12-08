using Lumina.Excel.Sheets;

namespace RotationSolver.Basic.Traits;

/// <summary>
/// Represents a base trait with various properties and methods.
/// </summary>
public class BaseTrait : IBaseTrait
{
    private readonly Trait? _trait;

    /// <summary>
    /// Gets a value indicating whether the player has a sufficient level for this trait.
    /// </summary>
    public bool EnoughLevel => DataCenter.PlayerSyncedLevel() >= Level;

    /// <summary>
    /// Gets the level required for this trait.
    /// </summary>
    public byte Level => _trait?.Level ?? 1;

    /// <summary>
    /// Gets the icon ID associated with this trait.
    /// </summary>
    public uint IconID { get; }

    /// <summary>
    /// Gets the name of this trait.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description of this trait.
    /// </summary>
    public string Description => Name;
    
    /// <summary>
    /// Gets or sets a value indicating whether this trait is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool IsIntercepted { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool IsRestrictedDOT { get; set; }

    /// <summary>
    /// Gets the ID of this trait.
    /// </summary>
    public uint ID { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseTrait"/> class with the specified trait ID.
    /// </summary>
    /// <param name="traitId">The ID of the trait.</param>
    public BaseTrait(uint traitId)
    {
        ID = traitId;
        _trait = Service.GetSheet<Trait>().GetRow(traitId);
        Name = _trait?.Name.ExtractText() ?? string.Empty;
        IconID = (uint)(_trait?.Icon ?? 0);
    }
}