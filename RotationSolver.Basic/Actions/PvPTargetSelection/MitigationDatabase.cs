using ECommons.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;

namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Dictionary-backed mitigation database. Construct via <see cref="WithEmbeddedDefaults"/>
/// for the shipped seed list, or via <see cref="LoadFromJson"/> to override from a user file.
/// JSON parse failures log a warning and fall back to the embedded defaults.
/// </summary>
public sealed class MitigationDatabase : IMitigationDatabase
{
    private readonly Dictionary<StatusID, MitigationEntry> _entries;

    private MitigationDatabase(Dictionary<StatusID, MitigationEntry> entries)
    {
        _entries = entries;
    }

    /// <inheritdoc/>
    public bool TryGet(StatusID id, out MitigationEntry entry) => _entries.TryGetValue(id, out entry);

    /// <summary>
    /// The seed list of major PvP defensives. Serves as the parse-failure fallback when
    /// <see cref="WithEmbeddedJson"/> cannot read or parse the shipped JSON resource.
    /// </summary>
    public static IReadOnlyList<MitigationEntry> EmbeddedDefaults { get; } = new[]
    {
        new MitigationEntry(StatusID.Guard,           MitigationKind.Invuln,  0.00, "PvP Guard: universal heavy DR, treated as effective invuln."),
        new MitigationEntry(StatusID.HallowedGround,  MitigationKind.Invuln,  0.00, "PLD Hallowed Ground: true invulnerability."),
        new MitigationEntry(StatusID.LivingDead,      MitigationKind.Invuln,  0.00, "DRK Living Dead: invulnerability state until expiration."),
        new MitigationEntry(StatusID.Holmgang_409,    MitigationKind.Invuln,  0.00, "WAR Holmgang: true invulnerability."),
        new MitigationEntry(StatusID.Superbolide,     MitigationKind.Invuln,  0.00, "GNB Superbolide: drops to 1 HP with self-recovery; conservative skip."),
        new MitigationEntry(StatusID.Bloodwhetting,   MitigationKind.HeavyDR, 0.50, "WAR Bloodwhetting: heavy DR plus self-heal."),
        new MitigationEntry(StatusID.SacredSoil,      MitigationKind.Shield,  0.20, "SCH Sacred Soil: damage shield, modeled as DR equivalent."),
        new MitigationEntry(StatusID.Macrocosmos,     MitigationKind.Shield,  0.20, "AST Macrocosmos: damage shield plus delayed heal."),
    };

    /// <summary>
    /// Build a database from the embedded seed list.
    /// </summary>
    public static MitigationDatabase WithEmbeddedDefaults() => new(BuildDictionary(EmbeddedDefaults));

    /// <summary>
    /// Read the shipped <c>Data/PvPMitigations.json</c> embedded resource and parse it as the
    /// authoritative seed list. Returns the embedded defaults if the resource is missing or
    /// fails to parse, with a warning logged via <see cref="PluginLog"/>.
    /// </summary>
    public static MitigationDatabase WithEmbeddedJson()
    {
        try
        {
            var assembly = typeof(MitigationDatabase).Assembly;
            using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName);
            if (stream == null)
            {
                var available = string.Join(", ", assembly.GetManifestResourceNames());
                TryWarn($"Embedded resource '{EmbeddedResourceName}' not found; falling back to in-code defaults. Available resources: [{available}]");
                return WithEmbeddedDefaults();
            }
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            return LoadFromJson(json);
        }
        catch (Exception ex)
        {
            TryWarn($"Failed to read embedded mitigation JSON ({ex.Message}); falling back to in-code defaults.");
            return WithEmbeddedDefaults();
        }
    }

    private const string EmbeddedResourceName = "RotationSolver.Basic.Data.PvPMitigations.json";

    /// <summary>
    /// Parse a JSON array of <see cref="MitigationEntry"/>-shaped objects. Returns a DB containing
    /// only the entries from the JSON. On parse failure, logs a warning and returns the embedded defaults.
    /// </summary>
    public static MitigationDatabase LoadFromJson(string json)
    {
        try
        {
            // Strict schema (MissingMemberHandling.Error) is a deliberate divergence from the
            // rest of the codebase: this is a small, stable schema and a user-editable file,
            // so unrecognized fields should fail loudly rather than be silently ignored.
            var settings = new JsonSerializerSettings
            {
                Converters = { new StringEnumConverter() },
                MissingMemberHandling = MissingMemberHandling.Error,
            };
            var entries = JsonConvert.DeserializeObject<List<MitigationEntry>>(json, settings);
            if (entries == null)
            {
                TryWarn("PvP mitigation JSON parsed to null; falling back to embedded defaults.");
                return WithEmbeddedDefaults();
            }
            return new MitigationDatabase(BuildDictionary(entries));
        }
        catch (JsonException ex)
        {
            TryWarn($"PvP mitigation JSON parse failed ({ex.Message}); falling back to embedded defaults.");
            return WithEmbeddedDefaults();
        }
    }

    // PluginLog requires Dalamud plugin initialization. Guard the log call so callers
    // running before plugin init (or under unusual contexts) still observe the fallback
    // behavior rather than seeing a NullReferenceException.
    private static void TryWarn(string message)
    {
        try { PluginLog.Warning(message); }
        catch { /* no plugin context — swallow */ }
    }

    private static Dictionary<StatusID, MitigationEntry> BuildDictionary(IEnumerable<MitigationEntry> entries)
    {
        var result = new Dictionary<StatusID, MitigationEntry>();
        foreach (var entry in entries)
        {
            // Last entry wins on duplicate Id; explicit user JSON overrides shipped defaults if both present.
            result[entry.Id] = entry;
        }
        return result;
    }
}
