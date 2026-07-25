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
		public static async Task Main()
		{
			try
			{
				var gameData = new Lumina.GameData(@"C:\FF14\game\sqpack", new LuminaOptions
				{
					LoadMultithreaded = true,
					CacheFileResources = true,
					PanicOnSheetChecksumMismatch = false,
					DefaultExcelLanguage = Language.English,
				});

				var assemblyLocation = typeof(Program).Assembly.Location;
				Console.WriteLine($"Assembly location: {assemblyLocation}");

				// Navigate up to the solution root (RotationSolverReborn directory)
				// Path is: .../RotationSolverReborn/RotationSolver.GameData/bin/Debug/net10.0-windows10.0.26100.0/RotationSolver.GameData.dll
				// We need to go up 5 levels from the assembly file
				var solutionRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(assemblyLocation)!, "..", "..", "..", "..", ".."));
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
                using RotationSolver.Basic.Actions;

                namespace RotationSolver.Basic.Rotations;
                
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
                using RotationSolver.Basic.Actions;

                namespace RotationSolver.Basic.Rotations.Duties;
                
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
            using RotationSolver.Basic.Actions;
            using RotationSolver.Basic.Traits;

            namespace RotationSolver.Basic.Rotations.Basic;

            """;

				var rotationsSheet = gameData.GetExcelSheet<ClassJob>()!;
				var rotationList = new List<string>();
				foreach (var job in rotationsSheet)
				{
					if (job.JobIndex > 0)
					{
						rotationList.Add(new RotationGetter(gameData, job).GetCode());
					}
				}
				WriteResource("Rotation", header + string.Join("\n\n", rotationList));

				using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
				var result = await client.GetAsync("https://raw.githubusercontent.com/karashiiro/FFXIVOpcodes/master/opcodes.json");

				if (result.StatusCode != HttpStatusCode.OK)
				{
					return;
				}

				var responseStream = await result.Content.ReadAsStringAsync();

				var root = JToken.Parse(responseStream)[0]!["lists"]!;
				var strs = new List<string>();
				foreach (var level1 in root.Children())
				{
					foreach (var level2 in level1.Children())
					{
						foreach (var tok in level2.Children())
						{
							if (tok is JObject i)
							{
								var name = ((JValue)i["name"]!).Value as string;
								var description = name!.Space();

								strs.Add($$"""
                    /// <summary>
                    ///{{description}}
                    /// </summary>
                    [Description("{{description}}")]
                    {{name}} = {{((JValue)i["opcode"]!).Value}},
                    """);
							}
						}
					}
				}
				WriteResource("OpCode", string.Join("\n", strs));

				Console.WriteLine("Finished!");
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"An error occurred: {ex.Message}");
			}
		}
	}
}