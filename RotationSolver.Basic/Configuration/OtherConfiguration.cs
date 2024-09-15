using ECommons.DalamudServices;

namespace RotationSolver.Basic.Configuration;

internal class OtherConfiguration
{
    public static HashSet<uint> HostileCastingArea = [];
    public static HashSet<uint> HostileCastingTank = [];
    public static HashSet<uint> HostileCastingKnockback = [];

    public static SortedList<uint, float> AnimationLockTime = [];

    public static Dictionary<uint, string[]> NoHostileNames = [];
    public static Dictionary<uint, string[]> NoProvokeNames = [];
    public static Dictionary<uint, Vector3[]> BeneficialPositions = [];

    public static HashSet<uint> DangerousStatus = [];
    public static HashSet<uint> PriorityStatus = [];
    public static HashSet<uint> InvincibleStatus = [];
    public static HashSet<uint> NoCastingStatus = [];

    public static RotationSolverRecord RotationSolverRecord = new();

    public static Dictionary<uint, StatusID[]> StatusProvide = [];
    public static Dictionary<uint, StatusID[]> TargetStatusProvide = [];
    
    public static void Init()
    {
        if (!Directory.Exists(Svc.PluginInterface.ConfigDirectory.FullName))
        {
            Directory.CreateDirectory(Svc.PluginInterface.ConfigDirectory.FullName);
        }

        Task.Run(() => InitOne(ref StatusProvide, nameof(StatusProvide)));
        Task.Run(() => InitOne(ref TargetStatusProvide, nameof(TargetStatusProvide)));
        Task.Run(() => InitOne(ref DangerousStatus, nameof(DangerousStatus)));
        Task.Run(() => InitOne(ref PriorityStatus, nameof(PriorityStatus)));
        Task.Run(() => InitOne(ref InvincibleStatus, nameof(InvincibleStatus)));
        Task.Run(() => InitOne(ref NoHostileNames, nameof(NoHostileNames)));
        Task.Run(() => InitOne(ref NoProvokeNames, nameof(NoProvokeNames)));
        Task.Run(() => InitOne(ref AnimationLockTime, nameof(AnimationLockTime)));
        Task.Run(() => InitOne(ref HostileCastingArea, nameof(HostileCastingArea)));
        Task.Run(() => InitOne(ref HostileCastingTank, nameof(HostileCastingTank)));
        Task.Run(() => InitOne(ref BeneficialPositions, nameof(BeneficialPositions)));
        Task.Run(() => InitOne(ref RotationSolverRecord, nameof(RotationSolverRecord), false));
        Task.Run(() => InitOne(ref NoCastingStatus, nameof(NoCastingStatus)));
        Task.Run(() => InitOne(ref HostileCastingKnockback, nameof(HostileCastingKnockback)));
    }

    public static Task Save()
    {
        return Task.Run(async () =>
        {
            await SavePriorityStatus();
            await SaveDangerousStatus();
            await SaveInvincibleStatus();
            await SaveNoHostileNames();
            await SaveAnimationLockTime();
            await SaveHostileCastingArea();
            await SaveHostileCastingTank();
            await SaveBeneficialPositions();
            await SaveRotationSolverRecord();
            await SaveNoProvokeNames();
            await SaveNoCastingStatus();
            await SaveHostileCastingKnockback();
            await SaveStatusProvide();
            await SaveTargetStatusProvide();
        });
    }

    private static Task SaveStatusProvide()
    {
        return Task.Run(() => Save(StatusProvide, nameof(StatusProvide)));
    }

    private static Task SaveTargetStatusProvide()
    {
        return Task.Run(() => Save(TargetStatusProvide, nameof(TargetStatusProvide)));
    }
    private static Task SaveHostileCastingKnockback()
    {
        return Task.Run(() => Save(HostileCastingKnockback, nameof(HostileCastingKnockback)));
    }

    public static Task SaveNoCastingStatus()
    {
        return Task.Run(() => Save(NoCastingStatus, nameof(NoCastingStatus)));
    }

    public static Task SavePriorityStatus()
    {
        return Task.Run(() => Save(PriorityStatus, nameof(PriorityStatus)));
    }

    public static Task SaveRotationSolverRecord()
    {
        return Task.Run(() => Save(RotationSolverRecord, nameof(RotationSolverRecord)));
    }
    public static Task SaveNoProvokeNames()
    {
        return Task.Run(() => Save(NoProvokeNames, nameof(NoProvokeNames)));
    }

    public static Task SaveBeneficialPositions()
    {
        return Task.Run(() => Save(BeneficialPositions, nameof(BeneficialPositions)));
    }

    public static Task SaveHostileCastingArea()
    {
        return Task.Run(() => Save(HostileCastingArea, nameof(HostileCastingArea)));
    }

    public static Task SaveHostileCastingTank()
    {
        return Task.Run(() => Save(HostileCastingTank, nameof(HostileCastingTank)));
    }

    public static Task SaveDangerousStatus()
    {
        return Task.Run(() => Save(DangerousStatus, nameof(DangerousStatus)));
    }

    public static Task SaveInvincibleStatus()
    {
        return Task.Run(() => Save(InvincibleStatus, nameof(InvincibleStatus)));
    }

    public static Task SaveNoHostileNames()
    {
        return Task.Run(() => Save(NoHostileNames, nameof(NoHostileNames)));
    }

    public static Task SaveAnimationLockTime()
    {
        return Task.Run(() => Save(AnimationLockTime, nameof(AnimationLockTime)));
    }

    private static string GetFilePath(string name)
    {
        var directory = Svc.PluginInterface.ConfigDirectory.FullName;

        return directory + $"\\{name}.json";
    }

    private static void Save<T>(T value, string name)
        => SavePath(value, GetFilePath(name));

    private static void SavePath<T>(T value, string path)
    {
        try
        {
            File.WriteAllText(path,
            JsonConvert.SerializeObject(value, Formatting.Indented, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.None,
            }));
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, $"Failed to save the file to {path}");
        }
    }

    private static void InitOne<T>(ref T value, string name, bool download = true)
    {
        var path = GetFilePath(name);
        if (File.Exists(path))
        {
            try
            {
                value = JsonConvert.DeserializeObject<T>(File.ReadAllText(path))!;
            }
            catch (Exception ex)
            {
                Svc.Log.Warning(ex, $"Failed to load {name}.");
            }
        }
        else if (download)
        {
            try
            {
                using var client = new HttpClient();
                var str = client.GetStringAsync($"https://raw.githubusercontent.com/{Service.USERNAME}/{Service.REPO}/main/Resources/{name}.json").Result;

                File.WriteAllText(path, str);
                value = JsonConvert.DeserializeObject<T>(str, new JsonSerializerSettings()
                {
                    MissingMemberHandling = MissingMemberHandling.Error,
                    Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                    {
                        args.ErrorContext.Handled = true;
                    }
                    !
                })!;
            }
            catch
            {
                SavePath(value, path);
            }
        }
        else
        {
            SavePath(value, path);
        }
    }
}
#pragma warning restore CA2211
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
