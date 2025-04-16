using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        var provider = context.SyntaxProvider.CreateSyntaxProvider(
            (s, _) => s is ClassDeclarationSyntax,
            (c, _) => (ClassDeclarationSyntax)c.Node).Where(i => i is not null);

        var compilation = context.CompilationProvider.Combine(provider.Collect());

        context.RegisterSourceOutput(compilation, (spc, source) => Execute(spc));
    }

    /// <summary>
    /// Executes the source generation process.
    /// </summary>
    /// <param name="context">The source production context.</param>
    private static void Execute(SourceProductionContext context)
    {
        try
        {
            GenerateStatus(context);
            GenerateActionID(context);
            GenerateContentType(context);
            GenerateActionCate(context);
            GenerateBaseRotation(context);
            GenerateRotations(context);
            GenerateOpCode(context);
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                "SG0001",
                "Source Generation Error",
                $"An error occurred during source generation: {ex.Message}",
                "SourceGenerator",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true), Location.None));
        }
    }

    /// <summary>
    /// Generates the OpCode enum source code.
    /// </summary>
    /// <param name="context">The source production context.</param>
    private static void GenerateOpCode(SourceProductionContext context)
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
            {{Properties.Resources.OpCode.Table()}}
            }
            """;

        context.AddSource("OpCode.g.cs", code);
    }

    /// <summary>
    /// Generates the StatusID enum source code.
    /// </summary>
    /// <param name="context">The source production context.</param>
    private static void GenerateStatus(SourceProductionContext context)
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
            {{Properties.Resources.StatusId.Table()}}
            }
            """;

        context.AddSource("StatusID.g.cs", code);
    }

    /// <summary>
    /// Generates the TerritoryContentType enum source code.
    /// </summary>
    /// <param name="context">The source production context.</param>
    private static void GenerateContentType(SourceProductionContext context)
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
            {{Properties.Resources.ContentType.Table()}}
            }
            """;

        context.AddSource("TerritoryContentType.g.cs", code);
    }

    /// <summary>
    /// Generates the ActionCate enum source code.
    /// </summary>
    /// <param="context">The source production context.</param>
    private static void GenerateActionCate(SourceProductionContext context)
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
            {{Properties.Resources.ActionCategory.Table()}}
            }
            """;

        context.AddSource("ActionCate.g.cs", code);
    }

    /// <summary>
    /// Generates the ActionID enum source code.
    /// </summary>
    /// <param="context">The source production context.</param>
    private static void GenerateActionID(SourceProductionContext context)
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
            {{Properties.Resources.ActionId.Table()}}
            }
            """;

        context.AddSource("ActionID.g.cs", code);
    }

    /// <summary>
    /// Generates the base rotation source code.
    /// </summary>
    /// <param="context">The source production context.</param>
    private static void GenerateBaseRotation(SourceProductionContext context)
    {
        context.AddSource("CustomRotation.g.cs", Properties.Resources.Action);
        context.AddSource("DutyRotation.g.cs", Properties.Resources.DutyAction);
    }

    /// <summary>
    /// Generates the rotations source code.
    /// </summary>
    /// <param="context">The source production context.</param>
    private static void GenerateRotations(SourceProductionContext context)
    {
        context.AddSource("BaseRotations.g.cs", Properties.Resources.Rotation);
    }
}