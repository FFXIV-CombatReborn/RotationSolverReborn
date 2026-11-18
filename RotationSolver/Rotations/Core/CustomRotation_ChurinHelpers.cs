using Lumina.Excel.Sheets;

namespace RotationSolver.Rotations;

public partial class CustomRotation
{
	#region ChurinHelpers
	/// <summary>
	/// Simplified potion strategy for timing-based potion usage
	/// </summary>
	public enum PotionStrategy
	{
		/// <summary>
		/// 
		/// </summary>
		[Description("Use potions in the opener and at 6 minutes")] ZeroSix,

		/// <summary>
		/// 
		/// </summary>
		[Description("Use potions at 2 and 8 minutes")] TwoEight,

		/// <summary>
		/// 
		/// </summary>
		[Description("Use potions in the opener, at 5 minutes and at 10 minutes")] ZeroFiveTen,

		/// <summary>
		/// 
		/// </summary>
		[Description("Use custom potion timings")] Custom
	}

	/// <summary>
	/// Lightweight potion manager focused purely on condition tracking and timing presets.
	/// </summary>
	public class Potions
	{
		#region Fields and Properties

		/// <summary>
		/// Gets or sets the current potion strategy
		/// </summary>
		public PotionStrategy Strategy { get; set; } = PotionStrategy.ZeroSix;

		/// <summary>
		/// Gets or sets whether the potion system is enabled
		/// </summary>
		public bool Enabled { get; set; }

		/// <summary>
		/// Gets or sets the custom timing struct
		/// </summary>
		public CustomTimingsData CustomTimings { get; set; } = new CustomTimingsData();

		/// <summary>
		/// 
		/// </summary>
		public float OpenerPotionTime { get; set; }

		#endregion
		/// <summary>
		/// Struct to hold custom potion timings
		/// </summary>
		public struct CustomTimingsData
		{
			/// <summary>
			/// Gets or sets the array of timing values in seconds
			/// </summary>
			public float[] Timings { get; set; }

			/// <summary>
			/// Initializes a new instance of the CustomTimings struct
			/// </summary>
			public CustomTimingsData()
			{
				Timings = [];
			}
		}

		#region Preset Timing Arrays

		/// <summary>
		/// Gets the 0-6 minute rotation timing pattern
		/// </summary>
		protected static readonly float[] ZeroSixTimings =
		[
			0.0f,    // Pull
            360.0f,  // 6 minutes
        ];

		/// <summary>
		/// Gets the 0-5-10 minute rotation timing pattern
		/// </summary>
		protected static readonly float[] ZeroFiveTenTimings =
		[
			0.0f,    // Pull
            300.0f,  // 5 minutes
            600.0f,  // 10 minutes
        ];

		/// <summary>
		/// Gets the 2-8 minute rotation timing pattern
		/// </summary>
		protected static readonly float[] TwoEightTimings =
		[
			120.0f,  // 2 minutes
            480.0f,  // 8 minutes
        ];

		#endregion

		/// <summary>
		/// The timing window in seconds for potion usage alignment.
		/// Set to 59 seconds to provide a generous buffer for GCD timing variations,
		/// ensuring potions are used within the intended combat phase while accounting for slight delays.
		/// </summary>
		protected const float TimingWindowSeconds = 59.0f;

		#region Core Methods

		/// <summary>
		/// Main method to check if potion should be used based on conditions and timing
		/// </summary>
		/// <param name="rotation">The custom rotation instance</param>
		/// <param name="act">The action to use if potion usage is recommended</param>
		/// <param name="clippingCheck">Whether to perform a clipping check when using the potion</param>
		/// <returns>True if both conditions and timing are met for potion usage</returns>
		public bool ShouldUsePotion(CustomRotation rotation, out IAction? act, bool clippingCheck = true)
		{
			act = null;

			if (!Enabled || StatusHelper.PlayerHasStatus(true, StatusID.Medicated))
			{
				return false;
			}

			// Check if conditions are met for potion usage
			if (!IsConditionMet())
			{
				return false;
			}

			// Check if current time aligns with strategy timing
			if (!CanUseAtTime())
			{
				return false;
			}

			return rotation.UseBurstMedicine(out act, clippingCheck);
		}

		/// <summary>
		/// Checks if potion can be used at the current combat time.
		/// Validates timing against the configured strategy and windows.
		/// </summary>
		/// <returns>True if potion can be used at this time</returns>
		public virtual bool CanUseAtTime()
		{
			if (!Enabled)
			{
				return false;
			}

			if (Strategy == PotionStrategy.Custom)
			{
				var timingsArr = CustomTimings.Timings;
				if (timingsArr == null || timingsArr.Length == 0)
				{
					return false;
				}

				var allZero = true;
				for (var i = 0; i < timingsArr.Length; i++)
				{
					if (timingsArr[i] != 0f)
					{
						allZero = false;
						break;
					}
				}
				if (allZero)
				{
					return false;
				}
			}

			// Get timing array based on strategy using extracted method
			var timings = GetTimingsArray();

			// Check if current time aligns with any timing window
			foreach (var timing in timings)
			{
				if (IsTimingValid(timing))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Validates current game state conditions for potion usage
		/// </summary>
		/// <returns>True if conditions are met for potion usage</returns>
		public virtual bool IsConditionMet()
		{
			if (!Enabled)
			{
				return false;
			}

			// Basic condition checks - override in derived classes for job-specific logic
			return true;
		}

		/// <summary>
		/// Checks if the specified timing represents an opener potion (timing == 0)
		/// </summary>
		/// <param name="timing">The potion timing to check</param>
		/// <returns>True if the timing is 0 (opener), false otherwise</returns>
		public static bool IsOpenerPotion(float timing) => timing == 0.0f;

		/// <summary>
		/// Gets the timing array based on the current strategy.
		/// This method encapsulates the strategy-to-timings mapping to eliminate duplication.
		/// </summary>
		/// <returns>Array of timing values in seconds for the current strategy.</returns>
		protected float[] GetTimingsArray()
		{
			return Strategy switch
			{
				PotionStrategy.ZeroSix => ZeroSixTimings,
				PotionStrategy.TwoEight => TwoEightTimings,
				PotionStrategy.ZeroFiveTen => ZeroFiveTenTimings,
				PotionStrategy.Custom => CustomTimings.Timings ?? [],
				_ => ZeroSixTimings
			};
		}

		/// <summary>
		/// Checks if the given timing is valid for potion usage at the current combat time.
		/// Validates both combat timing windows and opener countdown conditions.
		/// Virtual to allow job-specific timing logic overrides (e.g., DNC's more lenient >= timing check).
		/// </summary>
		/// <param name="timing">The timing value to check in seconds.</param>
		/// <returns>True if the timing allows potion usage.</returns>
		protected virtual bool IsTimingValid(float timing)
		{
			// Check combat timing window: timing must be positive, current time must be past the timing,
			// and within the allowed window to account for GCD variations
			if (timing > 0f
			&& DataCenter.CombatTimeRaw >= timing
			&& (DataCenter.CombatTimeRaw - timing) <= TimingWindowSeconds)
			{
				return true;
			}

			// Check opener timing: if it's an opener potion and countdown is within configured time
			var countDown = Service.CountDownTime;
			if (IsOpenerPotion(timing) && countDown > 0 && countDown <= OpenerPotionTime && !InCombat)
			{
				return true;
			}

			return false;
		}

		#endregion
	}

	#endregion
}
