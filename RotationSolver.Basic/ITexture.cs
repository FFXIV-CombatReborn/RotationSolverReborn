namespace RotationSolver.Basic;

/// <summary>
/// Represents an entity that has a texture.
/// </summary>
public interface ITexture
{
    /// <summary>
    /// Gets the icon ID associated with the texture.
    /// </summary>
    uint IconID { get; }

    /// <summary>
    /// Gets the name of the texture.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the texture.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the texture is enabled.
    /// </summary>
    /// <markdown file="Actions" name="Enabled (Action Name)">
    /// Enable or disable the usage of a skill. Turning this option off prevents RSR from using it completely.
    /// </markdown>
    bool IsEnabled { get; set; }

    /// <summary>
    /// 
    /// </summary>
    bool IsIntercepted { get; set; }

    /// <summary>
    /// 
    /// </summary>
    bool IsRestrictedDOT { get; set; }
}