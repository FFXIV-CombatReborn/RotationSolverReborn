using Lumina;
using Lumina.Data;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json.Linq;
using RotationSolver.GameData;
using RotationSolver.GameData.Getters;
using RotationSolver.GameData.Getters.Actions;
using System.Net;
using System.Resources.NetStandard;

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
            var gameData = new GameData(@"C:\FF14\game\sqpack", new LuminaOptions
            {
                LoadMultithreaded = true,
                CacheFileResources = true,
                PanicOnSheetChecksumMismatch = false,
                DefaultExcelLanguage = Language.English,
            });

            var dirInfo = new DirectoryInfo(typeof(Program).Assembly.Location);
            dirInfo = dirInfo.Parent!.Parent!.Parent!.Parent!.Parent!.Parent!;

            using var res = new ResXResourceWriter(Path.Combine(dirInfo.FullName, "RotationSolver.SourceGenerators\\Properties\\Resources.resx"));

            res.AddResource("StatusId", new StatusGetter(gameData).GetCode());
            res.AddResource("ContentType", new ContentTypeGetter(gameData).GetCode());
            res.AddResource("ActionId", new ActionIdGetter(gameData).GetCode());
            res.AddResource("ActionCategory", new ActionCategoryGetter(gameData).GetCode());

            var rotationBase = new ActionRoleRotationGetter(gameData);
            var rotationCodes = rotationBase.GetCode();
            var rotationItems = new ItemGetter(gameData);
            var rotationItemCodes = rotationItems.GetCode();

            res.AddResource("Action", $$"""
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

            res.AddResource("DutyAction", $$"""
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

            var rotations = gameData.GetExcelSheet<ClassJob>()!
                .Where(job => job.JobIndex > 0)
                .Select(job => new RotationGetter(gameData, job).GetCode());
            res.AddResource("Rotation", header + string.Join("\n\n", rotations));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var result = await client.GetAsync("https://raw.githubusercontent.com/karashiiro/FFXIVOpcodes/master/opcodes.json");

            if (result.StatusCode != HttpStatusCode.OK) return;
            var responseStream = await result.Content.ReadAsStringAsync();

            var strs = JToken.Parse(responseStream)[0]!["lists"]!.Children()
                .SelectMany(i => i.Children()).SelectMany(i => i.Children()).Cast<JObject>()
                .Select(i =>
                {
                    var name = ((JValue)i["name"]!).Value as string;
                    var description = name!.Space();

                    return $$"""
                    /// <summary>
                    ///{{description}}
                    /// </summary>
                    [Description("{{description}}")]
                    {{name}} = {{((JValue)i["opcode"]!).Value}},
                    """;
                });
            res.AddResource("OpCode", string.Join("\n", strs));

            res.Generate();

            Console.WriteLine("Finished!");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}