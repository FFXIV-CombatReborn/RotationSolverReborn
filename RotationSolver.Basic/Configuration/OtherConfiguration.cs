using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.Logging;
using Newtonsoft.Json.Converters;
using RotationSolver.Basic.Resources;

namespace RotationSolver.Basic.Configuration;

internal class OtherConfiguration
{
	/// <markdown file="List" name="AoE" section="Actions">
	///
	/// RSR will use group mitigation if any enemy in the enmity list is casting
	/// one of the listed actions. Usually those actions are raid-wides.
	/// </markdown>
	public static HashSet<uint> HostileCastingArea = [];

	/// <markdown file="List" name="Tank Buster" section="Actions">
	///
	/// RSR will use mitigation on target (heal) or self (tank) if the target is currently
	/// being targeted by one of the listed actions.
	/// </markdown>
	public static HashSet<uint> HostileCastingTank = [];

	/// <markdown file="List" name="Knockback" section="Actions">
	///
	/// **Click on "Record knockback actions" at your own peril. Some duties expect you take the
	/// knockback in order to reach a proper safe-spot, like in Sil'dihn Subterrane (Savage).**
	///
	/// RSR will use anti-knockback actions when you would be hit by one of the listed actions.
	/// </markdown>
	public static HashSet<uint> HostileCastingKnockback = [];

	/// <markdown file="List" name="Gaze/Stop" section="Actions">
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
	///
	/// Listed statuses will be dispelled (Esuna) first before any
	/// other dispellable statuses.
	/// </markdown>
	public static HashSet<uint> DangerousStatus = [];

	/// <markdown file="List" name="Priority" section="Statuses">
	///
	/// If running in auto mode, if any enemy in your enmity list has this status,
	/// it will target them as priority.
	/// </markdown>
	public static HashSet<uint> PriorityStatus = [];

	/// <markdown file="List" name="Invulnerability" section="Statuses">
	///
	/// Ignores target if they have one of the statuses listed.
	/// </markdown>
	public static HashSet<uint> InvincibleStatus = [];

	/// <markdown file="List" name="No-Casting Debuffs" section="Statuses">
	///
	/// If you have any of the statuses listed, RSR will stop taking any actions.
	/// </markdown>
	public static HashSet<uint> NoCastingStatus = [];
	public static List<Job> DancePartnerPriority = [];
	public static List<Job> TheSpearPriority = [];
	public static List<Job> TheBalancePriority = [];
	public static List<Job> KardiaTankPriority = [];

	public static RotationSolverRecord RotationSolverRecord = new();

	private abstract class ListFile(string name)
	{
		public string Name { get; } = name;

		public abstract string FilePath { get; }

		public abstract void Load();

		public abstract void Reset();

		public abstract string Serialize();
	}

	// A list that only exists in the user's config folder.
	private sealed class LocalFile<T>(string name, Func<T> get, Action<T> set) : ListFile(name) where T : class, new()
	{
		public override string FilePath => GetFilePath(Name);

		public override void Load() => set(ReadFile<T>(FilePath) ?? new T());

		public override void Reset() => set(new T());

		public override string Serialize() => JsonConvert.SerializeObject(get(), Formatting.Indented, SerializerSettings);
	}

	private abstract class BuiltInList<T, TChanges>(string name, T builtInList, Func<T> get, Action<T> set) : ListFile(name)
		where T : class, new()
		where TChanges : class
	{
		private T BuiltIn { get; } = builtInList;

		public override string FilePath => GetChangesPath(Name);

		public override void Load()
		{
			var changes = ReadFile<TChanges>(FilePath) ?? MigrateLegacyFile();
			set(changes == null ? Copy(BuiltIn) : Apply(BuiltIn, changes));
		}

		public override void Reset() => set(Copy(BuiltIn));

		public override string Serialize() => JsonConvert.SerializeObject(Diff(BuiltIn, get()), Formatting.Indented, SerializerSettings);

		protected abstract T Copy(T value);

		protected abstract T Apply(T builtIn, TChanges changes);

		protected abstract TChanges Diff(T builtIn, T current);

		protected virtual TChanges? Migrate(T builtIn, T legacy) => null;

		private TChanges? MigrateLegacyFile()
		{
			var legacyPath = GetFilePath(Name);
			if (!File.Exists(legacyPath))
			{
				return null;
			}

			var legacy = ReadFile<T>(legacyPath);
			var changes = legacy == null ? null : Migrate(BuiltIn, legacy);
			if (changes != null)
			{
				WriteFile(FilePath, JsonConvert.SerializeObject(changes, Formatting.Indented, SerializerSettings));
				if (!File.Exists(FilePath))
				{
					return changes;
				}
			}

			try
			{
				File.Delete(legacyPath);
				PluginLog.Information($"Moved {Name} to the built-in list{(changes == null ? string.Empty : " and kept your additions")}.");
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"Failed to delete the old {legacyPath}: {ex.Message}");
			}

			return changes;
		}
	}

	private sealed class SetChanges
	{
		public HashSet<uint> Added { get; set; } = [];
		public HashSet<uint> Removed { get; set; } = [];
	}

	private sealed class SetList(string name, uint[] builtInIds, Func<HashSet<uint>> get, Action<HashSet<uint>> set, params uint[] retired)
		: BuiltInList<HashSet<uint>, SetChanges>(name, [.. builtInIds], get, set)
	{
		protected override HashSet<uint> Copy(HashSet<uint> value) => [.. value];

		protected override HashSet<uint> Apply(HashSet<uint> builtIn, SetChanges changes)
		{
			var result = new HashSet<uint>(builtIn);
			result.ExceptWith(changes.Removed);
			result.UnionWith(changes.Added);
			return result;
		}

		protected override SetChanges Diff(HashSet<uint> builtIn, HashSet<uint> current)
		{
			var changes = new SetChanges();
			foreach (var id in current)
			{
				if (!builtIn.Contains(id))
				{
					_ = changes.Added.Add(id);
				}
			}

			foreach (var id in builtIn)
			{
				if (!current.Contains(id))
				{
					_ = changes.Removed.Add(id);
				}
			}

			return changes;
		}

		// A missing id may have joined the built-in list after the old file was downloaded, so only additions carry over.
		// Retired ids were once built in and later dropped, so an old file holding them got them from a download.
		protected override SetChanges Migrate(HashSet<uint> builtIn, HashSet<uint> legacy)
		{
			var changes = Diff(builtIn, legacy);
			changes.Removed.Clear();
			changes.Added.ExceptWith(retired);
			return changes;
		}
	}

	private sealed class MapChanges<TItem>
	{
		public Dictionary<uint, TItem[]> Added { get; set; } = [];
		public Dictionary<uint, TItem[]> Removed { get; set; } = [];
	}

	// Per-territory lists.
	private sealed class MapList<TItem>(string name, Dictionary<uint, TItem[]> builtInMap, Func<Dictionary<uint, TItem[]>> get, Action<Dictionary<uint, TItem[]>> set)
		: BuiltInList<Dictionary<uint, TItem[]>, MapChanges<TItem>>(name, builtInMap, get, set)
	{
		protected override Dictionary<uint, TItem[]> Copy(Dictionary<uint, TItem[]> value)
		{
			var result = new Dictionary<uint, TItem[]>(value.Count);
			foreach (var (key, items) in value)
			{
				result[key] = [.. items];
			}

			return result;
		}

		protected override Dictionary<uint, TItem[]> Apply(Dictionary<uint, TItem[]> builtIn, MapChanges<TItem> changes)
		{
			var result = Copy(builtIn);
			foreach (var (key, removed) in changes.Removed)
			{
				if (result.TryGetValue(key, out var items))
				{
					result[key] = Except(items, removed, false);
				}
			}

			foreach (var (key, added) in changes.Added)
			{
				result[key] = result.TryGetValue(key, out var items) ? [.. items, .. Except(added, items, true)] : [.. added];
			}

			return result;
		}

		protected override MapChanges<TItem> Diff(Dictionary<uint, TItem[]> builtIn, Dictionary<uint, TItem[]> current)
		{
			var changes = new MapChanges<TItem>();
			foreach (var (key, items) in current)
			{
				var added = Except(items, builtIn.GetValueOrDefault(key), true);
				if (added.Length > 0)
				{
					changes.Added[key] = added;
				}
			}

			foreach (var (key, items) in builtIn)
			{
				var removed = Except(items, current.GetValueOrDefault(key), true);
				if (removed.Length > 0)
				{
					changes.Removed[key] = removed;
				}
			}

			return changes;
		}

		protected override MapChanges<TItem> Migrate(Dictionary<uint, TItem[]> builtIn, Dictionary<uint, TItem[]> legacy)
		{
			var changes = Diff(builtIn, legacy);
			changes.Removed.Clear();
			return changes;
		}

		private static TItem[] Except(TItem[] items, TItem[]? other, bool skipBlank)
		{
			var result = new List<TItem>(items.Length);
			foreach (var item in items)
			{
				if ((skipBlank && item is null or string { Length: 0 })
					|| (other != null && Array.IndexOf(other, item) >= 0)
					|| result.Contains(item))
				{
					continue;
				}

				result.Add(item);
			}

			return [.. result];
		}
	}

	private sealed class OrderList(string name, Job[] builtInOrder, Func<List<Job>> get, Action<List<Job>> set)
		: BuiltInList<List<Job>, List<Job>>(name, [.. builtInOrder], get, set)
	{
		protected override List<Job> Copy(List<Job> value) => [.. value];

		protected override List<Job> Apply(List<Job> builtIn, List<Job> changes)
		{
			var result = new List<Job>(builtIn.Count);
			foreach (var job in changes)
			{
				if (builtIn.Contains(job) && !result.Contains(job))
				{
					result.Add(job);
				}
			}

			foreach (var job in builtIn)
			{
				if (!result.Contains(job))
				{
					result.Add(job);
				}
			}

			return result;
		}

		protected override List<Job> Diff(List<Job> builtIn, List<Job> current)
		{
			if (current.Count == builtIn.Count)
			{
				var same = true;
				for (var i = 0; i < current.Count && same; i++)
				{
					same = current[i] == builtIn[i];
				}

				if (same)
				{
					return [];
				}
			}

			return [.. current];
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
		new SetList(nameof(DangerousStatus), StatusLists.DangerousStatus, () => DangerousStatus, v => DangerousStatus = v),
		new SetList(nameof(PriorityStatus), StatusLists.PriorityStatus, () => PriorityStatus, v => PriorityStatus = v, 861, 1323, 2654, 3202),
		new SetList(nameof(InvincibleStatus), StatusLists.InvincibleStatus, () => InvincibleStatus, v => InvincibleStatus = v, 151, 198, 350, 385, 945, 1303, 3054),
		new OrderList(nameof(DancePartnerPriority), JobPriorityLists.DancePartnerPriority, () => DancePartnerPriority, v => DancePartnerPriority = v),
		new OrderList(nameof(TheSpearPriority), JobPriorityLists.TheSpearPriority, () => TheSpearPriority, v => TheSpearPriority = v),
		new OrderList(nameof(TheBalancePriority), JobPriorityLists.TheBalancePriority, () => TheBalancePriority, v => TheBalancePriority = v),
		new OrderList(nameof(KardiaTankPriority), JobPriorityLists.KardiaTankPriority, () => KardiaTankPriority, v => KardiaTankPriority = v),
		new MapList<string>(nameof(NoHostileNames), TerritoryLists.NoHostileNames, () => NoHostileNames, v => NoHostileNames = v),
		new MapList<string>(nameof(NoProvokeNames), TerritoryLists.NoProvokeNames, () => NoProvokeNames, v => NoProvokeNames = v),
		new SetList(nameof(HostileCastingArea), ActionLists.HostileCastingArea, () => HostileCastingArea, v => HostileCastingArea = v, 41147, 41763),
		new SetList(nameof(HostileCastingTank), ActionLists.HostileCastingTank, () => HostileCastingTank, v => HostileCastingTank = v,
			13364, 13365, 13367, 13371, 13375, 13385, 13388, 13438, 13444, 13452, 13454, 13462, 13475),
		new MapList<Vector3>(nameof(BeneficialPositions), TerritoryLists.BeneficialPositions, () => BeneficialPositions, v => BeneficialPositions = v),
		new LocalFile<RotationSolverRecord>(nameof(RotationSolverRecord), () => RotationSolverRecord, v => RotationSolverRecord = v),
		new SetList(nameof(NoCastingStatus), StatusLists.NoCastingStatus, () => NoCastingStatus, v => NoCastingStatus = v),
		new SetList(nameof(HostileCastingKnockback), ActionLists.HostileCastingKnockback, () => HostileCastingKnockback, v => HostileCastingKnockback = v),
		new SetList(nameof(HostileCastingStop), ActionLists.HostileCastingStop, () => HostileCastingStop, v => HostileCastingStop = v, 3715, 3719, 3720, 3721, 3722),
		new LocalFile<Dictionary<uint, List<string>>>(nameof(NorthHornWeaknessRecords), () => NorthHornWeaknessRecords, v => NorthHornWeaknessRecords = v),
		new LocalFile<Dictionary<uint, List<string>>>(nameof(SouthHornWeaknessRecords), () => SouthHornWeaknessRecords, v => SouthHornWeaknessRecords = v),
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
		_ = Directory.CreateDirectory(GetChangesPath(string.Empty));

		var tasks = new Task[Files.Length];
		for (var i = 0; i < Files.Length; i++)
		{
			var file = Files[i];
			tasks[i] = Task.Run(file.Load, cancellationToken);
		}

		await Task.WhenAll(tasks);
	}

	public static Task Save()
	{
		// Serialized on the calling thread so the lists aren't read while the game thread changes them.
		var contents = new (string Path, string Json)[Files.Length];
		for (var i = 0; i < Files.Length; i++)
		{
			contents[i] = (Files[i].FilePath, Files[i].Serialize());
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
		var file = GetFile(name);
		var path = file.FilePath;
		var json = file.Serialize();
		return Task.Run(() => WriteFile(path, json));
	}

	private static void ResetFile(string name)
	{
		GetFile(name).Reset();
		_ = SaveList(name);
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

	private static string GetChangesPath(string name)
	{
		var directory = Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "ListChanges");
		return name.Length == 0 ? directory : Path.Combine(directory, $"{name}.json");
	}

	private static T? ReadFile<T>(string path) where T : class
	{
		if (!File.Exists(path))
		{
			return null;
		}

		try
		{
			return JsonConvert.DeserializeObject<T>(File.ReadAllText(path), DeserializerSettings);
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"Failed to load {path}: {ex.Message}");
			return null;
		}
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
}
