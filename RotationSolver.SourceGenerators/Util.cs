using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace RotationSolver.SourceGenerators
{
	internal readonly struct DiagnosticSpec : IEquatable<DiagnosticSpec>
	{
		public string Id { get; }
		public string FilePath { get; }
		public TextSpan TextSpan { get; }
		public LinePositionSpan LineSpan { get; }
		public string? Arg { get; }

		public DiagnosticSpec(string id, Location location, string? arg = null)
		{
			Id = id;
			var lineSpan = location.GetLineSpan();
			FilePath = lineSpan.Path ?? string.Empty;
			TextSpan = location.SourceSpan;
			LineSpan = lineSpan.Span;
			Arg = arg;
		}

		public Location ToLocation() => string.IsNullOrEmpty(FilePath)
			? Location.None
			: Location.Create(FilePath, TextSpan, LineSpan);

		public bool Equals(DiagnosticSpec other)
			=> Id == other.Id
			&& FilePath == other.FilePath
			&& TextSpan.Equals(other.TextSpan)
			&& LineSpan.Equals(other.LineSpan)
			&& Arg == other.Arg;

		public override bool Equals(object? obj) => obj is DiagnosticSpec other && Equals(other);

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = Id.GetHashCode();
				hash = (hash * 397) ^ FilePath.GetHashCode();
				hash = (hash * 397) ^ TextSpan.GetHashCode();
				hash = (hash * 397) ^ LineSpan.GetHashCode();
				hash = (hash * 397) ^ (Arg?.GetHashCode() ?? 0);
				return hash;
			}
		}
	}

	internal sealed class TypeHierarchyPart : IEquatable<TypeHierarchyPart>
	{
		public string Keyword { get; }
		public string NameWithTypeParams { get; }

		public TypeHierarchyPart(string keyword, string nameWithTypeParams)
		{
			Keyword = keyword;
			NameWithTypeParams = nameWithTypeParams;
		}

		public bool Equals(TypeHierarchyPart? other)
			=> other is not null && Keyword == other.Keyword && NameWithTypeParams == other.NameWithTypeParams;

		public override bool Equals(object? obj) => Equals(obj as TypeHierarchyPart);

		public override int GetHashCode()
		{
			unchecked
			{
				return (Keyword.GetHashCode() * 397) ^ NameWithTypeParams.GetHashCode();
			}
		}
	}

	internal static class Util
	{
		private static readonly SymbolDisplayFormat FullyQualifiedWithNullabilityFormat =
			SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
				SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

		private static readonly Regex InvalidCharsRgx = new(@"[^_a-zA-Z0-9]", RegexOptions.Compiled);
		private static readonly Regex WhiteSpaceRgx = new(@"(?<=\s)", RegexOptions.Compiled);
		private static readonly Regex StartsWithLowerCaseCharRgx = new("^[a-z]", RegexOptions.Compiled);
		private static readonly Regex FirstCharFollowedByUpperCasesOnlyRgx = new("(?<=[A-Z])[A-Z0-9]+$", RegexOptions.Compiled);
		private static readonly Regex LowerCaseNextToNumberRgx = new("(?<=[0-9])[a-z]", RegexOptions.Compiled);
		private static readonly Regex UpperCaseInsideRgx = new("(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))", RegexOptions.Compiled);

		/// <summary>
		/// Gets the parent node of the specified type.
		/// </summary>
		/// <typeparam name="TS">The type of the parent node.</typeparam>
		/// <param name="node">The starting syntax node.</param>
		/// <returns>The parent node of the specified type, or null if not found.</returns>
		public static TS? GetParent<TS>(this SyntaxNode? node) where TS : SyntaxNode
		{
			if (node == null)
			{
				return null;
			}

			if (node is TS result)
			{
				return result;
			}

			return GetParent<TS>(node.Parent);
		}

		/// <summary>
		/// Gets the full metadata name of the specified symbol.
		/// </summary>
		/// <param name="s">The symbol.</param>
		/// <returns>The full metadata name of the symbol.</returns>
		public static string GetFullMetadataName(this ISymbol? s)
		{
			if (s == null || s is INamespaceSymbol)
			{
				return string.Empty;
			}

			while (s != null && s is not ITypeSymbol)
			{
				s = s.ContainingSymbol;
			}

			if (s is not ITypeSymbol typeSymbol)
			{
				return string.Empty;
			}

			var display = typeSymbol.ToDisplayString(FullyQualifiedWithNullabilityFormat);
			return display.StartsWith("global::", StringComparison.Ordinal)
				? display.Substring("global::".Length)
				: display;
		}

		public static string GetFullyQualifiedTypeName(this ITypeSymbol typeSymbol)
			=> typeSymbol.ToDisplayString(FullyQualifiedWithNullabilityFormat);

		public static bool CanInstantiateWithParameterlessCtor(this ITypeSymbol typeSymbol)
		{
			if (!typeSymbol.IsReferenceType || typeSymbol.SpecialType == SpecialType.System_String || typeSymbol.IsAbstract)
			{
				return false;
			}

			if (typeSymbol is not INamedTypeSymbol namedType)
			{
				return false;
			}

			foreach (var ctor in namedType.InstanceConstructors)
			{
				if (ctor.Parameters.Length == 0 && ctor.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal)
				{
					return true;
				}
			}

			return false;
		}

		public static string GetNamespaceDisplay(INamedTypeSymbol containingType)
			=> containingType.ContainingNamespace is { IsGlobalNamespace: false } ns
				? ns.ToDisplayString()
				: string.Empty;

		public static ImmutableArray<TypeHierarchyPart> GetTypeHierarchy(INamedTypeSymbol containingType)
		{
			var parts = new List<TypeHierarchyPart>();
			for (INamedTypeSymbol? current = containingType; current != null; current = current.ContainingType)
			{
				var keyword = current.TypeKind switch
				{
					TypeKind.Struct => current.IsRecord ? "record struct" : "struct",
					_ => current.IsRecord ? "record" : "class",
				};

				var name = current.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
				parts.Add(new TypeHierarchyPart(keyword, name));
			}

			parts.Reverse();
			return ImmutableArray.CreateRange(parts);
		}

		public static ImmutableArray<string> GetAllowedAttributes(
			FieldDeclarationSyntax field,
			SemanticModel model,
			params string[] allowedMetadataNames)
		{
			var result = ImmutableArray.CreateBuilder<string>();
			foreach (var attrSet in field.AttributeLists)
			{
				if (attrSet == null)
				{
					continue;
				}

				foreach (var attr in attrSet.Attributes)
				{
					var attrType = model.GetSymbolInfo(attr).Symbol?.ContainingType;
					if (attrType == null)
					{
						continue;
					}

					var metadataName = attrType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
					for (var i = 0; i < allowedMetadataNames.Length; i++)
					{
						if (metadataName == allowedMetadataNames[i])
						{
							result.Add(attr.ToString());
							break;
						}
					}
				}
			}

			return result.ToImmutable();
		}

		public static string GetSafeHintPrefix(string namespaceName, ImmutableArray<TypeHierarchyPart> hierarchy)
		{
			var sb = new StringBuilder();
			if (!string.IsNullOrEmpty(namespaceName))
			{
				sb.Append(namespaceName).Append('_');
			}

			for (var i = 0; i < hierarchy.Length; i++)
			{
				if (i > 0)
				{
					sb.Append('.');
				}
				sb.Append(InvalidCharsRgx.Replace(hierarchy[i].NameWithTypeParams, "_"));
			}

			return sb.ToString();
		}

		public static bool SequenceEquals<T>(ImmutableArray<T> left, ImmutableArray<T> right) where T : IEquatable<T>
		{
			if (left.Length != right.Length)
			{
				return false;
			}

			for (var i = 0; i < left.Length; i++)
			{
				if (!left[i].Equals(right[i]))
				{
					return false;
				}
			}

			return true;
		}

		public static bool StringSequenceEquals(ImmutableArray<string> left, ImmutableArray<string> right)
		{
			if (left.Length != right.Length)
			{
				return false;
			}

			for (var i = 0; i < left.Length; i++)
			{
				if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
				{
					return false;
				}
			}

			return true;
		}

		public static int GetSequenceHashCode<T>(ImmutableArray<T> items)
		{
			unchecked
			{
				var hash = 17;
				for (var i = 0; i < items.Length; i++)
				{
					hash = (hash * 31) + (items[i]?.GetHashCode() ?? 0);
				}
				return hash;
			}
		}

		/// <summary>
		/// Indents each line of the string with four spaces.
		/// </summary>
		/// <param name="str">The input string.</param>
		/// <returns>The indented string.</returns>
		public static string Table(this string str) => "    " + str.Replace("\n", "\n    ");

		/// <summary>
		/// Converts the input string to PascalCase.
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <returns>The PascalCase string.</returns>
		public static string ToPascalCase(this string input)
		{
			var parts = input.Split('.');
			for (var i = 0; i < parts.Length; i++)
			{
				parts[i] = ConvertToPascalCase(parts[i]);
			}
			return string.Join(".", parts);

			static string ConvertToPascalCase(string input)
			{
				var cleaned = InvalidCharsRgx.Replace(WhiteSpaceRgx.Replace(input, "_"), string.Empty);
				var parts = cleaned.Split(['_'], StringSplitOptions.RemoveEmptyEntries);
				var sb = new StringBuilder();
				foreach (var w0 in parts)
				{
					var w = StartsWithLowerCaseCharRgx.Replace(w0, m => m.Value.ToUpperInvariant());
					w = FirstCharFollowedByUpperCasesOnlyRgx.Replace(w, m => m.Value.ToLowerInvariant());
					w = LowerCaseNextToNumberRgx.Replace(w, m => m.Value.ToUpperInvariant());
					w = UpperCaseInsideRgx.Replace(w, m => m.Value.ToLowerInvariant());
					sb.Append(w);
				}

				return sb.ToString();
			}
		}
	}
}