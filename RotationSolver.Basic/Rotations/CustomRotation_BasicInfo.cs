using ECommons.DalamudServices;
using ECommons.ExcelServices;
using Lumina.Excel.Sheets;

namespace RotationSolver.Basic.Rotations;

/// <summary>
/// Represents a custom rotation for a specific job.
/// </summary>
public partial class CustomRotation : ICustomRotation
{
    private Job? _job = null;
    private JobRole? _role = null;
    private string? _name = null;
    private string? _description = null;
    private readonly IRotationConfigSet _configs;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomRotation"/> class.
    /// </summary>
    private protected CustomRotation()
    {
        IconID = IconSet.GetJobIcon(Job);
        _configs = new RotationConfigSet(this);
    }

    /// <inheritdoc/>
    public Job Job => _job ??= GetType().GetCustomAttribute<JobsAttribute>()?.Jobs[0] ?? Job.ADV;

    /// <inheritdoc/>
    public JobRole Role => _role ??= Svc.Data.GetExcelSheet<ClassJob>()!.GetRow((uint)Job)!.GetJobRole();

    /// <inheritdoc/>
    public string Name
    {
        get
        {
            if (_name != null)
            {
                return _name;
            }

            ClassJob classJob = Svc.Data.GetExcelSheet<ClassJob>().GetRow((uint)Job)!;
            return _name = $"{classJob.Abbreviation} - {classJob.Name}";
        }
    }

    /// <inheritdoc/>
    public bool IsEnabled
    {
        get
        {
            var disabled = Service.Config.DisabledJobs;
            foreach (var j in disabled)
            {
                if (j == Job)
                {
                    return false;
                }
            }
            return true;
        }
        set
        {
            var disabled = Service.Config.DisabledJobs;
            if (value)
            {
                // enable -> remove the job if present
                _ = disabled.Remove(Job);
            }
            else
            {
                // disable -> add only if not already present (avoid LINQ)
                bool exists = false;
                foreach (var j in disabled)
                {
                    if (j == Job)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    disabled.Add(Job);
                }
            }
        }
    }

    /// <inheritdoc/>
    public bool IsIntercepted { get; set; }

    /// <inheritdoc/>
    public bool IsRestrictedDOT { get; set; }

    /// <inheritdoc/>
    public uint IconID { get; }

    /// <inheritdoc/>
    IRotationConfigSet ICustomRotation.Configs => _configs;

    /// <inheritdoc/>
    bool ICustomRotation.IsInBurstWindow => IsBursting();

    /// <inheritdoc/>
    public string Description => _description ??= GetType().GetCustomAttribute<RotationAttribute>()?.Description ?? string.Empty;

    /// <inheritdoc/>
    public IAction? ActionHealAreaGCD { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionHealAreaAbility { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionHealSingleGCD { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionHealSingleAbility { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionDefenseAreaGCD { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionDefenseAreaAbility { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionDefenseSingleGCD { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionDefenseSingleAbility { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionMoveForwardGCD { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionMoveForwardAbility { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionMoveBackAbility { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionSpeedAbility { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionDispelStancePositionalGCD { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionDispelStancePositionalAbility { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionRaiseShirkGCD { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionRaiseShirkAbility { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionAntiKnockbackAbility { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this rotation is valid.
    /// </summary>
    [Description("Is this rotation valid")]
    public bool IsValid { get; private set; } = true;

    /// <summary>
    /// Gets the reason why this rotation is not valid.
    /// </summary>
    public string WhyNotValid { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether to show the status to the users.
    /// </summary>
    [Description("Show the status")]
    public virtual bool ShowStatus => false;

    /// <inheritdoc/>
    public override string ToString()
    {
        return GetType().GetCustomAttribute<RotationAttribute>()?.Name ?? GetType().Name;
    }

    /// <summary>
    /// Gets whether this rotation is in burst window
    /// </summary>
    public virtual bool IsBursting()
    {
        return true;
    }

    /// <summary>
    /// Updates the custom fields.
    /// </summary>
    protected virtual void UpdateInfo() { }

    /// <summary>
    ///
    /// </summary>
    public virtual void DisplayRotationStatus()
    {
        ImGui.TextWrapped($"If you want to display some extra information on this panel, please override the {nameof(DisplayRotationStatus)} method!");
    }

    /// <summary>
    /// 
    /// </summary>
    public virtual void DisplayBaseStatus()
    {
        ImGui.TextWrapped($"If you want to display some extra information on this panel, please override the {nameof(DisplayBaseStatus)} method!");
    }

    /// <summary>
    /// Handles actions when the territory changes.
    /// </summary>
    public virtual void OnTerritoryChanged() { }

    /// <summary>
    /// Creates a system warning to display to the end-user.
    /// </summary>
    /// <param name="warning">The warning message.</param>
    public void CreateSystemWarning(string warning)
    {
        _ = BasicWarningHelper.AddSystemWarning(warning);
    }
}
