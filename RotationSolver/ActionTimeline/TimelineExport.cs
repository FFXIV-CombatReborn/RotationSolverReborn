namespace RotationSolver.ActionTimeline;

/// <summary>
/// Represents an exported timeline session for JSON serialization
/// </summary>
public class TimelineExportSession
{
	[JsonProperty("sessionInfo")]
	public SessionInfo SessionInfo { get; set; } = new();

	[JsonProperty("actions")]
	public List<ExportedAction> Actions { get; set; } = new();
}

/// <summary>
/// Information about the recording session
/// </summary>
public class SessionInfo
{
	[JsonProperty("startTime")]
	public DateTime StartTime { get; set; }

	[JsonProperty("endTime")]
	public DateTime EndTime { get; set; }

	[JsonProperty("duration")]
	public double DurationSeconds { get; set; }

	[JsonProperty("playerName")]
	public string PlayerName { get; set; } = string.Empty;

	[JsonProperty("playerJob")]
	public string PlayerJob { get; set; } = string.Empty;

	[JsonProperty("territory")]
	public string Territory { get; set; } = string.Empty;

	[JsonProperty("duty")]
	public string Duty { get; set; } = string.Empty;

	[JsonProperty("exportedAt")]
	public DateTime ExportedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Represents an action in the exported timeline
/// </summary>
public class ExportedAction
{
	[JsonProperty("name")]
	public string Name { get; set; } = string.Empty;

	[JsonProperty("id")]
	public uint Id { get; set; }

	[JsonProperty("icon")]
	public uint Icon { get; set; }

	[JsonProperty("type")]
	public string Type { get; set; } = string.Empty;

	[JsonProperty("startTime")]
	public DateTime StartTime { get; set; }

	[JsonProperty("endTime")]
	public DateTime EndTime { get; set; }

	[JsonProperty("combatTime")]
	public double CombatTimeSeconds { get; set; }

	[JsonProperty("castTime")]
	public double CastTimeSeconds { get; set; }

	[JsonProperty("state")]
	public string State { get; set; } = string.Empty;

	[JsonProperty("target")]
	public string Target { get; set; } = string.Empty;
}
