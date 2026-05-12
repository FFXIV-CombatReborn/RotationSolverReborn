using ECommons.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;

namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Dictionary-backed PvP LB database. Construct via <see cref="WithEmbeddedJson"/>
/// at startup. Parse failures log a warning and return an empty DB (consumers fall
/// through to a zero-contribution scoring term).
/// </summary>
public sealed class LBDatabase : ILBDatabase
{
    private readonly Dictionary<uint, LBEntry> _entries;

    private LBDatabase(Dictionary<uint, LBEntry> entries)
    {
        _entries = entries;
    }

    /// <inheritdoc/>
    public bool TryGet(uint actionId, out LBEntry entry) => _entries.TryGetValue(actionId, out entry);

    /// <summary>
    /// An empty DB. Used as the parse-failure fallback and the early-init default.
    /// </summary>
    public static LBDatabase Empty { get; } = new(new Dictionary<uint, LBEntry>());

    /// <summary>
    /// Read the shipped <c>Data/PvPLBs.json</c> embedded resource and parse it.
    /// Missing resource or parse failure returns <see cref="Empty"/> with a warning logged.
    /// </summary>
    public static LBDatabase WithEmbeddedJson()
    {
        try
        {
            var assembly = typeof(LBDatabase).Assembly;
            using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName);
            if (stream == null)
            {
                var available = string.Join(", ", assembly.GetManifestResourceNames());
                TryWarn($"Embedded resource '{EmbeddedResourceName}' not found; LB DB will be empty. Available resources: [{available}]");
                return Empty;
            }
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            return LoadFromJson(json);
        }
        catch (Exception ex)
        {
            TryWarn($"Failed to read embedded LB JSON ({ex.Message}); LB DB will be empty.");
            return Empty;
        }
    }

    /// <summary>
    /// Parse a JSON array of <see cref="LBEntry"/>-shaped objects. Returns an empty DB
    /// on parse failure with a warning logged.
    /// </summary>
    public static LBDatabase LoadFromJson(string json)
    {
        try
        {
            var settings = new JsonSerializerSettings
            {
                Converters = { new StringEnumConverter() },
                MissingMemberHandling = MissingMemberHandling.Error,
            };
            var entries = JsonConvert.DeserializeObject<List<LBEntry>>(json, settings);
            if (entries == null)
            {
                TryWarn("PvP LB JSON parsed to null; LB DB will be empty.");
                return Empty;
            }
            return new LBDatabase(BuildDictionary(entries));
        }
        catch (JsonException ex)
        {
            TryWarn($"PvP LB JSON parse failed ({ex.Message}); LB DB will be empty.");
            return Empty;
        }
    }

    private const string EmbeddedResourceName = "RotationSolver.Basic.Data.PvPLBs.json";

    private static void TryWarn(string message)
    {
        try { PluginLog.Warning(message); }
        catch { /* no plugin context — swallow */ }
    }

    private static Dictionary<uint, LBEntry> BuildDictionary(IEnumerable<LBEntry> entries)
    {
        var result = new Dictionary<uint, LBEntry>();
        foreach (var entry in entries)
        {
            // Last entry wins on duplicate ActionId.
            result[entry.ActionId] = entry;
        }
        return result;
    }
}
