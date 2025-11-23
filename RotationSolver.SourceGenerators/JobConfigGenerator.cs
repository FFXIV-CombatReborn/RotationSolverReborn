using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace RotationSolver.SourceGenerators;

/// <summary>
/// Source generator for creating job configuration properties.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class JobConfigGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the generator.
    /// </summary>
    /// <param name="context">The initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "RotationSolver.Basic.Attributes.JobConfigAttribute",
                static (node, _) => node is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax { Parent: ClassDeclarationSyntax or StructDeclarationSyntax } } },
                static (n, ct) => ((VariableDeclaratorSyntax)n.TargetNode, n.SemanticModel))
            .Where(m => m.Item1 != null);

        context.RegisterSourceOutput(provider.Collect(), Execute);
    }

    /// <summary>
    /// Executes the source generation.
    /// </summary>
    /// <param name="context">The source production context.</param>
    /// <param name="array">The collected syntax nodes and semantic models.</param>
    private void Execute(SourceProductionContext context, ImmutableArray<(VariableDeclaratorSyntax, SemanticModel SemanticModel)> array)
    {
        var dict = new Dictionary<SyntaxNode, List<(VariableDeclaratorSyntax, SemanticModel)>>();
        foreach (var entry in array)
        {
            var key = entry.Item1.Parent!.Parent!.Parent!;
            if (!dict.TryGetValue(key, out var list))
            {
                list = [];
                dict[key] = list;
            }
            list.Add((entry.Item1, entry.SemanticModel));
        }

        foreach (var kv in dict)
        {
            var type = (TypeDeclarationSyntax)kv.Key;
            var namespaceName = type.GetParent<BaseNamespaceDeclarationSyntax>()?.Name.ToString() ?? "Null";
            var classType = type is ClassDeclarationSyntax ? "class" : "struct";
            var className = type.Identifier.Text;

            var propertyCodes = new List<string>();
            foreach (var (variableInfo, model) in kv.Value)
            {
                var typeSymbol = model.GetDeclaredSymbol(type) as ITypeSymbol;
                var field = (FieldDeclarationSyntax)variableInfo.Parent!.Parent!;
                var variableName = variableInfo.Identifier.ToString();
                var propertyName = variableName.ToPascalCase();

                if (variableName == propertyName)
                {
                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                        "RS001",
                        "Field name should not be in Pascal Case",
                        "Please don't use Pascal Case to name your field '{0}'",
                        "Naming",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true), variableInfo.Identifier.GetLocation(), variableName));
                    continue;
                }

                var key = string.Join(".", namespaceName, className, propertyName);
                var fieldTypeStr = field.Declaration.Type;
                var fieldType = model.GetTypeInfo(fieldTypeStr).Type!;
                var fieldStr = fieldType.GetFullMetadataName();

                var attributeNames = new List<string>();
                foreach (var attrSet in field.AttributeLists)
                {
                    if (attrSet == null) continue;
                    foreach (var attr in attrSet.Attributes)
                    {
                        var attrSymbol = model.GetSymbolInfo(attr).Symbol?.GetFullMetadataName();
                        if (attrSymbol is "RotationSolver.Basic.Attributes.UIAttribute"
                            or "RotationSolver.Basic.Attributes.UnitAttribute"
                            or "RotationSolver.Basic.Attributes.RangeAttribute"
                            or "RotationSolver.Basic.Attributes.JobConfigAttribute"
                            or "RotationSolver.Basic.Attributes.LinkDescriptionAttribute")
                        {
                            attributeNames.Add(attr.ToString());
                        }
                    }
                }

                var attributeStr = attributeNames.Count == 0 ? "" : $"[{string.Join(", ", attributeNames)}]";
                var propertyCode = $$"""
                    [JsonProperty]
                    private Dictionary<Job, {{fieldStr}}> {{variableName}}Dict = new();

                    [JsonIgnore]
                    {{attributeStr}}
                    public {{fieldStr}} {{propertyName}}
                    {
                        get
                        {
                            if ({{variableName}}Dict.TryGetValue(DataCenter.Job, out var value)) return value;
                            return {{variableName}}Dict[DataCenter.Job] = {{variableName}};
                        }
                        set
                        {
                            {{variableName}}Dict[DataCenter.Job] = value;
                        }
                    }
                """;

                propertyCodes.Add(propertyCode);
            }

            if (propertyCodes.Count == 0) continue;

            var code = $$"""
                using ECommons.ExcelServices;

                namespace {{namespaceName}}
                {
                    partial {{classType}} {{className}}
                    {
                        {{string.Join("\n\n", propertyCodes)}}
                    }
                }
            """;

            context.AddSource($"{namespaceName}_{className}.g.cs", code);
        }
    }
}