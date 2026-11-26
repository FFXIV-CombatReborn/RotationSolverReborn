using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;
using Newtonsoft.Json.Linq;
using RotationSolver.GameData.Getters;
using RotationSolver.GameData.Getters.Actions;
using System.Net;
using System.Resources.NetStandard;

namespace RotationSolver.GameData
{

	/// <summary>
	/// Entry point for the RotationSolver GameData program.
	/// </summary>
	public class Program
	{
		/// <summary>
		/// Main method to execute the program.
		/// </summary>
		public static async Task<int> Main(string[] args)
		{
			try
			{
				var sqpackPath = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
					? args[0]
					: Environment.GetEnvironmentVariable("FFXIV_SQPACK_PATH");
				if (string.IsNullOrWhiteSpace(sqpackPath))
				{
					sqpackPath = @"C:\FF14\game\sqpack";
				}

				var gameData = new Lumina.GameData(sqpackPath, new LuminaOptions
				{
					LoadMultithreaded = true,
					CacheFileResources = true,
					PanicOnSheetChecksumMismatch = false,
					DefaultExcelLanguage = Language.English,
				});

				var solutionRoot = FindSolutionRoot(AppContext.BaseDirectory)
					?? FindSolutionRoot(Environment.CurrentDirectory)
					?? throw new DirectoryNotFoundException("Could not locate RotationSolver.sln in parent directories.");
				Console.WriteLine($"Solution root: {solutionRoot}");

				var propertiesDir = Path.Combine(solutionRoot, "RotationSolver.SourceGenerators", "Properties");
				Console.WriteLine($"Properties directory: {propertiesDir}");
				Console.WriteLine($"Properties directory exists: {Directory.Exists(propertiesDir)}");

				void WriteResource(string name, string content)
				{
					var path = Path.Combine(propertiesDir, $"{name}.resx");
					Console.WriteLine($"Writing resource: {path}");
					using var writer = new ResXResourceWriter(path);
					writer.AddResource("Value", content);
					writer.Generate();
				}

				WriteResource("Status", new StatusGetter(gameData).GetCode());
				WriteResource("ContentType", new ContentTypeGetter(gameData).GetCode());
				WriteResource("ActionId", new ActionIdGetter(gameData).GetCode());
				WriteResource("ActionCategory", new ActionCategoryGetter(gameData).GetCode());
				WriteResource("BNpcName", new BNpcNameGetter(gameData).GetCode());

				var rotationBase = new ActionRoleRotationGetter(gameData);
				var rotationCodes = rotationBase.GetCode();
				var rotationItems = new ItemGetter(gameData);
				var rotationItemCodes = rotationItems.GetCode();

				WriteResource("Action", $$"""
                using RotationSolver.Engine.Actions;

                namespace RotationSolver.Rotations;
                
                /// <summary>
                /// The Custom Rotation.
                /// <br>Number of Actions: {{rotationBase.Count}}</br>
                /// </summary>
                public abstract partial class CustomRotation
                {
                #region Actions
                {{rotationCodes.Table()}}

                {{Util.ArrayNames("AllBaseActions", "IBaseAction",
					"public virtual", [.. rotationBase.AddedNames]).Table()}}
                #endregion

                #region Items
                {{rotationItemCodes.Table()}}

                {{Util.ArrayNames("AllItems", "IBaseItem",
					"public ", [.. rotationItems.AddedNames]).Table()}}
                #endregion
                }
                """);

				var dutyRotationBase = new ActionDutyRotationGetter(gameData);
				rotationCodes = dutyRotationBase.GetCode();

				WriteResource("DutyAction", $$"""
                using RotationSolver.Engine.Actions;

                namespace RotationSolver.Rotations.Duties;
                
                /// <summary>
                /// The Custom Rotation.
                /// <br>Number of Actions: {{dutyRotationBase.Count}}</br>
                /// </summary>
                public abstract partial class DutyRotation
                {
                {{rotationCodes.Table()}}
                }
                """);

				var header = """
            using ECommons.DalamudServices;
            using ECommons.ExcelServices;
            using RotationSolver.Engine.Actions;
            using RotationSolver.Engine.Traits;

            """;

				var rotationsSheet = gameData.GetExcelSheet<ClassJob>()!;
				var pvpLimitBreaks = RotationGetter.CollectPvPLimitBreaks(gameData);
				var rotationList = new List<string>();
				foreach (var job in rotationsSheet)
				{
					if (job.JobIndex > 0)
					{
						rotationList.Add(new RotationGetter(gameData, job, pvpLimitBreaks).GetCode());
					}
				}
				WriteResource("Rotation", header + string.Join("\n\n", rotationList));

				using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
				var result = await client.GetAsync("https://raw.githubusercontent.com/karashiiro/FFXIVOpcodes/master/opcodes.json");

				if (result.StatusCode != HttpStatusCode.OK)
				{
					Console.Error.WriteLine($"Failed to fetch opcodes.json: HTTP {(int)result.StatusCode} ({result.StatusCode})");
					return 1;
				}

				var responseStream = await result.Content.ReadAsStringAsync();

				var regions = JArray.Parse(responseStream);
				JToken? globalRegion = null;
				foreach (var regionToken in regions)
				{
					if (string.Equals(regionToken["region"]?.Value<string>(), "Global", StringComparison.OrdinalIgnoreCase))
					{
						globalRegion = regionToken;
						break;
					}
				}
				globalRegion ??= regions.First;

				var root = globalRegion?["lists"] ?? throw new InvalidDataException("Missing opcode lists in opcodes.json.");
				var strs = new List<string>();
				var seenOpCodes = new HashSet<string>(StringComparer.Ordinal);
				foreach (var level1 in root.Children())
				{
					foreach (var level2 in level1.Children())
					{
						foreach (var tok in level2.Children())
						{
							if (tok is JObject i)
							{
								var name = ((JValue?)i["name"])?.Value as string;
								if (string.IsNullOrWhiteSpace(name) || !seenOpCodes.Add(name))
								{
									continue;
								}

								var description = name.Space();

								strs.Add($$"""
                    /// <summary>
                    ///{{description}}
                    /// </summary>
                    [Description("{{description}}")]
                    {{name}} = {{((JValue?)i["opcode"])?.Value}},
                    """);
							}
						}
					}
				}
				WriteResource("OpCode", string.Join("\n", strs));

				Console.WriteLine("Finished!");
				return 0;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"An error occurred: {ex}");
				return 1;
			}
		}

		private static string? FindSolutionRoot(string startDirectory)
		{
			var current = new DirectoryInfo(Path.GetFullPath(startDirectory));
			while (current != null)
			{
				if (File.Exists(Path.Combine(current.FullName, "RotationSolver.sln")))
				{
					return current.FullName;
				}

				current = current.Parent;
			}

			return null;
		}
	}
}