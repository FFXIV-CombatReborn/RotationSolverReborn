using Microsoft.CodeAnalysis;

namespace RotationSolver.SourceGenerators;

/// <summary>
/// A source generator that generates static code for various enums and classes.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class StaticCodeGenerator : IIncrementalGenerator
{
	/// <summary>
	/// Initializes the generator with the provided context.
	/// </summary>
	/// <param name="context">The initialization context.</param>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// The only input is the embedded resx data, so the code is generated once instead of after every edit.
		context.RegisterPostInitializationOutput(Execute);
	}

	/// <summary>
	/// Executes the source generation process.
	/// </summary>
	/// <param name="context">The post-initialization context.</param>
	private static void Execute(IncrementalGeneratorPostInitializationContext context)
	{
		try
		{
			GenerateStatus(context);
			GenerateActionID(context);
			GenerateContentType(context);
			GenerateActionCate(context);
			GenerateBNpcNameID(context);
			GenerateBaseRotation(context);
			GenerateRotations(context);
			GenerateOpCode(context);
		}
		catch (Exception ex)
		{
			// Post-initialization can't report diagnostics, so the failure surfaces as a compile error.
			context.AddSource("SourceGenerationError.g.cs", $"#error Source generation failed: {ex.Message.Replace('\r', ' ').Replace('\n', ' ')}");
		}
	}

	/// <summary>
	/// Generates the OpCode enum source code.
	/// </summary>
	/// <param name="context">The source production context.</param>
	private static void GenerateOpCode(IncrementalGeneratorPostInitializationContext context)
	{
		var code = $$"""
			namespace RotationSolver.Basic.Data;

			/// <summary>
			/// The opcode
			/// </summary>
			public enum OpCode : ushort
			{
				/// <summary>
				/// 
				/// </summary>
				None = 0,
			{{Properties.OpCode.Value.Table()}}
			}
			""";

		context.AddSource("OpCode.g.cs", code);
	}

	/// <summary>
	/// Generates the StatusID enum source code.
	/// </summary>
	/// <param name="context">The source production context.</param>
	private static void GenerateStatus(IncrementalGeneratorPostInitializationContext context)
	{
		var code = $$"""
			namespace RotationSolver.Basic.Data;
			
			/// <summary>
			/// The id of the status.
			/// </summary>
			public enum StatusID : ushort
			{
				/// <summary>
				/// 
				/// </summary>
				None = 0,
			{{Properties.Status.Value.Table()}}
			}
			""";

		context.AddSource("StatusID.g.cs", code);
	}

	/// <summary>
	/// Generates the TerritoryContentType enum source code.
	/// </summary>
	/// <param name="context">The source production context.</param>
	private static void GenerateContentType(IncrementalGeneratorPostInitializationContext context)
	{
		var code = $$"""
			namespace RotationSolver.Basic.Data;

			/// <summary>
			/// 
			/// </summary>
			public enum TerritoryContentType : byte
			{
				/// <summary>
				/// 
				/// </summary>
				None = 0,
			{{Properties.ContentType.Value.Table()}}
			}
			""";

		context.AddSource("TerritoryContentType.g.cs", code);
	}

	/// <summary>
	/// Generates the ActionCate enum source code.
	/// </summary>
	/// <param="context">The source production context.</param>
	private static void GenerateActionCate(IncrementalGeneratorPostInitializationContext context)
	{
		var code = $$"""
			namespace RotationSolver.Basic.Data;

			/// <summary>
			/// 
			/// </summary>
			public enum ActionCate : byte
			{
				/// <summary>
				/// 
				/// </summary>
				None = 0,
			{{Properties.ActionCategory.Value.Table()}}
			}
			""";

		context.AddSource("ActionCate.g.cs", code);
	}

	/// <summary>
	/// Generates the BNpcNameID enum source code.
	/// </summary>
	/// <param name="context">The source production context.</param>
	private static void GenerateBNpcNameID(IncrementalGeneratorPostInitializationContext context)
	{
		var code = $$"""
			namespace RotationSolver.Basic.Data;

			/// <summary>
			/// The id of the BNpc name.
			/// </summary>
			public enum NPCName : uint
			{
				/// <summary>
				/// 
				/// </summary>
				None = 0,
			{{Properties.BNpcName.Value.Table()}}
			}
			""";

		context.AddSource("BNpcNameID.g.cs", code);
	}

	/// <summary>
	/// Generates the ActionID enum source code.
	/// </summary>
	/// <param="context">The source production context.</param>
	private static void GenerateActionID(IncrementalGeneratorPostInitializationContext context)
	{
		var code = $$"""
			namespace RotationSolver.Basic.Data;

			/// <summary>
			/// The id of the status.
			/// </summary>
			public enum ActionID : uint
			{
				/// <summary>
				/// 
				/// </summary>
				None = 0,
			{{Properties.ActionId.Value.Table()}}
			}
			""";

		context.AddSource("ActionID.g.cs", code);
	}

	/// <summary>
	/// Generates the base rotation source code.
	/// </summary>
	/// <param="context">The source production context.</param>
	private static void GenerateBaseRotation(IncrementalGeneratorPostInitializationContext context)
	{
		context.AddSource("CustomRotation.g.cs", Properties.Action.Value);
		context.AddSource("DutyRotation.g.cs", Properties.DutyAction.Value);
	}

	/// <summary>
	/// Generates the rotations source code.
	/// </summary>
	/// <param="context">The source production context.</param>
	private static void GenerateRotations(IncrementalGeneratorPostInitializationContext context)
	{
		context.AddSource("BaseRotations.g.cs", Properties.Rotation.Value);
	}
}