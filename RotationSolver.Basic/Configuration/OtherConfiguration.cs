using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.Logging;
using Newtonsoft.Json.Converters;

namespace RotationSolver.Basic.Configuration;

internal class OtherConfiguration
{
	private static readonly HttpClient Http = CreateHttpClient();
	private static HttpClient CreateHttpClient()
	{
		var c = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
		try
		{
			c.DefaultRequestHeaders.UserAgent.ParseAdd("RotationSolver");
			c.DefaultRequestHeaders.Accept.ParseAdd("application/json");
		}
		catch { /* best-effort headers */ }
		return c;
	}
	/// <markdown file="List" name="AoE" section="Actions">
	/// **`It is recommended to click on the reset button after every patch.`**
	/// 
	/// RSR will use group mitigation if any enemy in the enmity list is casting
	/// one of the listed actions. Usually those actions are raid-wides.
	/// </markdown>
	public static HashSet<uint> HostileCastingArea = [];

	/// <markdown file="List" name="Tank Buster" section="Actions">
	/// **`It is recommended to click on the reset button after every patch.`**
	/// 
	/// RSR will use mitigation on target (heal) or self (tank) if the target is currently
	/// being targeted by one of the listed actions.
	/// </markdown>
	public static HashSet<uint> HostileCastingTank = [];

	/// <markdown file="List" name="Knockback" section="Actions">
	/// **`It is recommended to click on the reset button after every patch.`**
	///
	/// **Click on "Record knockback actions" at your own peril. Some duties expect you take the
	/// knockback in order to reach a proper safe-spot, like in Sil'dihn Subterrane (Savage).**
	/// 
	/// RSR will use anti-knockback actions when you would be hit by one of the listed actions.
	/// </markdown>
	public static HashSet<uint> HostileCastingKnockback = [];

	/// <markdown file="List" name="Gaze/Stop" section="Actions">
	/// **`It is recommended to click on the reset button after every patch.`**
	/// 
	/// If the target is casting one of the listed actions, RSR will stop casting
	/// in the seconds before the action is resolved
	/// <see cref="RotationSolver.Basic.Configuration.Configs._castingStop">here</see>.
	/// </markdown>
	public static HashSet<uint> HostileCastingStop = [];

	public static Dictionary<uint, string[]> NoHostileNames = [];
	public static Dictionary<uint, string[]> NoProvokeNames = [];

	/// <summary>
	/// Auto-recorded elemental weaknesses observed while in North Horn, keyed by NameId.
	/// </summary>
	public static Dictionary<uint, List<string>> NorthHornWeaknessRecords = [];

	/// <summary>
	/// Auto-recorded elemental weaknesses observed while in South Horn, keyed by NameId.
	/// </summary>
	public static Dictionary<uint, List<string>> SouthHornWeaknessRecords = [];

	/// <markdown file="List" name="Beneficial Positions" section="Map-Specific Settings">
	/// Adds a preferred location used for ground **healing** AoE abilities (example: Earthly Star).
	///
	/// You can add multiple locations, in case a boss fight moves you to another platform, like M4S - Wicked Thunder.
	/// </markdown>
	public static Dictionary<uint, Vector3[]> BeneficialPositions = [];

	/// <markdown file="List" name="Dispellable Debuffs" section="Statuses">
	/// **`It is recommended to click on the reset button after every patch.`**
	/// 
	/// Listed statuses will be dispelled (Esuna) first before any
	/// other dispellable statuses.
	/// </markdown>
	public static HashSet<uint> DangerousStatus = [];

	/// <markdown file="List" name="Priority" section="Statuses">
	/// **`It is recommended to click on the reset button after every patch.`**
	/// 
	/// If running in auto mode, if any enemy in your enmity list has this status,
	/// it will target them as priority.
	/// </markdown>
	public static HashSet<uint> PriorityStatus = [];

	/// <markdown file="List" name="Invulnerability" section="Statuses">
	/// **`It is recommended to click on the reset button after every patch.`**
	/// 
	/// Ignores target if they have one of the statuses listed.
	/// </markdown>
	public static HashSet<uint> InvincibleStatus = [];

	/// <markdown file="List" name="No-Casting Debuffs" section="Statuses">
	/// **`It is recommended to click on the reset button after every patch.`**
	/// 
	/// If you have any of the statuses listed, RSR will stop taking any actions.
	/// </markdown>
	public static HashSet<uint> NoCastingStatus = [];
	public static List<Job> DancePartnerPriority = [];
	public static List<Job> TheSpearPriority = [];
	public static List<Job> TheBalancePriority = [];
	public static List<Job> KardiaTankPriority = [];

	public static RotationSolverRecord RotationSolverRecord = new();

	private abstract class ListFile(string name, bool download)
	{
		public string Name { get; } = name;
		public bool Download { get; } = download;

		public abstract void Load(bool forceDownload);

		public abstract string Serialize();
	}

	private sealed class ListFile<T>(string name, Func<T> get, Action<T> set, bool download = true) : ListFile(name, download) where T : new()
	{
		public override void Load(bool forceDownload)
		{
			set(InitOne(get(), Name, Download, forceDownload));
		}

		public override string Serialize()
		{
			return JsonConvert.SerializeObject(get(), Formatting.Indented, SerializerSettings);
		}
	}

	private static readonly JsonSerializerSettings SerializerSettings = new()
	{
		TypeNameHandling = TypeNameHandling.None,
	};

	private static readonly JsonSerializerSettings DeserializerSettings = new()
	{
		TypeNameHandling = TypeNameHandling.None,
		Converters = [new StringEnumConverter()],
	};

	private static readonly ListFile[] Files =
	[
		new ListFile<HashSet<uint>>(nameof(DangerousStatus), () => DangerousStatus, v => DangerousStatus = v),
		new ListFile<HashSet<uint>>(nameof(PriorityStatus), () => PriorityStatus, v => PriorityStatus = v),
		new ListFile<HashSet<uint>>(nameof(InvincibleStatus), () => InvincibleStatus, v => InvincibleStatus = v),
		new ListFile<List<Job>>(nameof(DancePartnerPriority), () => DancePartnerPriority, v => DancePartnerPriority = v),
		new ListFile<List<Job>>(nameof(TheSpearPriority), () => TheSpearPriority, v => TheSpearPriority = v),
		new ListFile<List<Job>>(nameof(TheBalancePriority), () => TheBalancePriority, v => TheBalancePriority = v),
		new ListFile<List<Job>>(nameof(KardiaTankPriority), () => KardiaTankPriority, v => KardiaTankPriority = v),
		new ListFile<Dictionary<uint, string[]>>(nameof(NoHostileNames), () => NoHostileNames, v => NoHostileNames = v),
		new ListFile<Dictionary<uint, string[]>>(nameof(NoProvokeNames), () => NoProvokeNames, v => NoProvokeNames = v),
		new ListFile<HashSet<uint>>(nameof(HostileCastingArea), () => HostileCastingArea, v => HostileCastingArea = v),
		new ListFile<HashSet<uint>>(nameof(HostileCastingTank), () => HostileCastingTank, v => HostileCastingTank = v),
		new ListFile<Dictionary<uint, Vector3[]>>(nameof(BeneficialPositions), () => BeneficialPositions, v => BeneficialPositions = v),
		new ListFile<RotationSolverRecord>(nameof(RotationSolverRecord), () => RotationSolverRecord, v => RotationSolverRecord = v, false),
		new ListFile<HashSet<uint>>(nameof(NoCastingStatus), () => NoCastingStatus, v => NoCastingStatus = v),
		new ListFile<HashSet<uint>>(nameof(HostileCastingKnockback), () => HostileCastingKnockback, v => HostileCastingKnockback = v),
		new ListFile<HashSet<uint>>(nameof(HostileCastingStop), () => HostileCastingStop, v => HostileCastingStop = v),
		new ListFile<Dictionary<uint, List<string>>>(nameof(NorthHornWeaknessRecords), () => NorthHornWeaknessRecords, v => NorthHornWeaknessRecords = v, false),
		new ListFile<Dictionary<uint, List<string>>>(nameof(SouthHornWeaknessRecords), () => SouthHornWeaknessRecords, v => SouthHornWeaknessRecords = v, false),
	];

	private static ListFile GetFile(string name)
	{
		foreach (var file in Files)
		{
			if (file.Name == name)
			{
				return file;
			}
		}

		throw new ArgumentException($"Unknown list file {name}", nameof(name));
	}

	public static async Task InitAsync(CancellationToken cancellationToken = default)
	{
		if (!Directory.Exists(Svc.PluginInterface.ConfigDirectory.FullName))
		{
			_ = Directory.CreateDirectory(Svc.PluginInterface.ConfigDirectory.FullName);
		}

		var tasks = new Task[Files.Length];
		for (var i = 0; i < Files.Length; i++)
		{
			var file = Files[i];
			tasks[i] = Task.Run(() => file.Load(false), cancellationToken);
		}

		await Task.WhenAll(tasks);
	}

	public static Task Save()
	{
		// Serialized on the calling thread so the lists aren't read while the game thread changes them.
		var contents = new (string Path, string Json)[Files.Length];
		for (var i = 0; i < Files.Length; i++)
		{
			contents[i] = (GetFilePath(Files[i].Name), Files[i].Serialize());
		}

		return Task.Run(() =>
		{
			foreach (var (path, json) in contents)
			{
				WriteFile(path, json);
			}
		});
	}

	// Writes one list file, so editing a list doesn't rewrite all of them.
	internal static Task SaveList(string name)
	{
		var path = GetFilePath(name);
		var json = GetFile(name).Serialize();
		return Task.Run(() => WriteFile(path, json));
	}

	// Downloads in the background so the game thread isn't blocked by the request.
	private static void ResetFile(string name)
	{
		var file = GetFile(name);
		_ = Task.Run(() => file.Load(true));
	}

	#region Action Tab
	public static void ResetHostileCastingArea() => ResetFile(nameof(HostileCastingArea));

	public static void ResetHostileCastingTank() => ResetFile(nameof(HostileCastingTank));

	public static void ResetHostileCastingKnockback() => ResetFile(nameof(HostileCastingKnockback));

	public static void ResetHostileCastingStop() => ResetFile(nameof(HostileCastingStop));

	public static Task SaveHostileCastingArea() => SaveList(nameof(HostileCastingArea));

	public static Task SaveHostileCastingTank() => SaveList(nameof(HostileCastingTank));

	public static Task SaveHostileCastingKnockback() => SaveList(nameof(HostileCastingKnockback));
	#endregion

	#region Status Tab
	public static void ResetPriorityStatus() => ResetFile(nameof(PriorityStatus));

	public static void ResetInvincibleStatus() => ResetFile(nameof(InvincibleStatus));

	public static void ResetDangerousStatus() => ResetFile(nameof(DangerousStatus));

	public static void ResetNoCastingStatus() => ResetFile(nameof(NoCastingStatus));
	#endregion

	public static Task SaveRotationSolverRecord() => SaveList(nameof(RotationSolverRecord));

	public static Task SaveNoProvokeNames() => SaveList(nameof(NoProvokeNames));

	public static Task SaveBeneficialPositions() => SaveList(nameof(BeneficialPositions));

	public static void ResetDancePartnerPriority() => ResetFile(nameof(DancePartnerPriority));

	public static void ResetTheSpearPriority() => ResetFile(nameof(TheSpearPriority));

	public static void ResetTheBalancePriority() => ResetFile(nameof(TheBalancePriority));

	public static void ResetKardiaTankPriority() => ResetFile(nameof(KardiaTankPriority));

	public static Task SaveDancePartnerPriority() => SaveList(nameof(DancePartnerPriority));

	public static Task SaveTheSpearPriority() => SaveList(nameof(TheSpearPriority));

	public static Task SaveTheBalancePriority() => SaveList(nameof(TheBalancePriority));

	public static Task SaveKardiaTankPriority() => SaveList(nameof(KardiaTankPriority));

	public static Task SaveNoHostileNames() => SaveList(nameof(NoHostileNames));

	#region Occult Crescent Weakness Tracking

	public static Task SaveNorthHornWeaknessRecords() => SaveList(nameof(NorthHornWeaknessRecords));

	public static Task SaveSouthHornWeaknessRecords() => SaveList(nameof(SouthHornWeaknessRecords));

	public static void ResetOccultWeaknessRecords()
	{
		NorthHornWeaknessRecords = [];
		SouthHornWeaknessRecords = [];
		_ = SaveNorthHornWeaknessRecords();
		_ = SaveSouthHornWeaknessRecords();
	}

	public static bool RecordOccultWeakness(uint nameId, StatusID weakness)
	{
		if (nameId == 0)
		{
			return false;
		}

		// Skip checking/recording entirely if this NameId is already manually curated.
		if (StatusHelper.HasKnownOccultWeakness(nameId))
		{
			return false;
		}

		var records = DataCenter.IsInNorthHorn
			? NorthHornWeaknessRecords
			: DataCenter.IsInSouthHorn
				? SouthHornWeaknessRecords
				: null;

		if (records == null)
		{
			return false;
		}

		if (!records.TryGetValue(nameId, out var list))
		{
			list = [];
			records[nameId] = list;
		}

		var weaknessName = weakness.ToString();
		if (list.Contains(weaknessName))
		{
			return false;
		}

		list.Add(weaknessName);
		return true;
	}

	#endregion

	private static string GetFilePath(string name)
	{
		return Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, $"{name}.json");
	}

	// Written to a temp file first, so a crash mid-write can't leave a half-written list.
	private static void WriteFile(string path, string json)
	{
		const int retryCount = 3;
		for (var i = 0; i < retryCount; i++)
		{
			try
			{
				var tempPath = path + ".tmp";
				File.WriteAllText(tempPath, json);
				File.Move(tempPath, path, true);
				return;
			}
			catch (IOException ex) when (i < retryCount - 1)
			{
				PluginLog.Warning($"Failed to save the file to {path}. Retrying in 1s...: {ex.Message}");
				Thread.Sleep(1000);
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"Failed to save the file to {path}: {ex.Message}");
				return;
			}
		}
	}

	private static T InitOne<T>(T current, string name, bool download, bool forceDownload) where T : new()
	{
		var path = GetFilePath(name);
		PluginLog.Information($"Initializing {name} from {path}");

		var hasLocalFile = File.Exists(path);
		if (hasLocalFile && !forceDownload)
		{
			try
			{
				var value = JsonConvert.DeserializeObject<T>(File.ReadAllText(path), DeserializerSettings)
					?? throw new Exception("Deserialized value is null.");
				PluginLog.Information($"Loaded {name} from local file.");
				return value;
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"Failed to load {name} from local file. Reinitializing to default: {ex.Message}");
				return new T();
			}
		}

		if (!download && !forceDownload)
		{
			return new T();
		}

		try
		{
			var url = $"https://raw.githubusercontent.com/{Service.USERNAME}/{Service.REPO}/main/Resources/{name}.json";
			var str = Http.GetStringAsync(url).GetAwaiter().GetResult();
			var value = JsonConvert.DeserializeObject<T>(str, DeserializerSettings)
				?? throw new Exception("Deserialized value is null.");

			// Only a list that parsed is written, so a bad download never replaces the local copy.
			WriteFile(path, str);
			PluginLog.Information($"Downloaded and loaded {name} from GitHub.");
			return value;
		}
		catch (Exception ex)
		{
			// Nothing is written on failure, so the download is tried again next time.
			PluginLog.Warning($"Failed to download {name} from GitHub. Exception: {ex.Message}");
			_ = BasicWarningHelper.AddSystemWarning($"Github download failed.");
			return forceDownload ? current : new T();
		}
	}
}
