using Dalamud.Plugin.Services;

namespace RotationSolver.Basic.Data
{
	/// <summary>
	/// Manages job gauges and provides thread-safe access to them.
	/// </summary>
	/// <remarks>
	/// Initializes a new instance of the <see cref="JobGaugeManager"/> class.
	/// </remarks>
	/// <param name="jobGauges">The job gauges service.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="jobGauges"/> is null.</exception>
	public class JobGaugeManager(IJobGauges jobGauges)
	{
		private readonly IJobGauges jobGauges = jobGauges ?? throw new ArgumentNullException(nameof(jobGauges));
		private readonly Lock lockObject = new();

		/// <summary>
		/// Gets the job gauge of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of the job gauge.</typeparam>
		/// <returns>The job gauge of the specified type.</returns>
		public T GetJobGauge<T>() where T : JobGaugeBase
		{
			lock (lockObject)
			{
				return jobGauges.Get<T>();
			}
		}
	}
}