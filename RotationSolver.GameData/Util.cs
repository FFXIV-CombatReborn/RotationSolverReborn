using Lumina.Excel.Sheets;
using System.Text;
using System.Text.RegularExpressions;

namespace RotationSolver.GameData;
/// <summary>
/// Utility class for various helper methods.
/// </summary>
internal static partial class Util
{
    /// <summary>
    /// Determines if the job category represents a single job for combat.
    /// </summary>
    /// <param name="jobCategory">The job category to check.</param>
    /// <returns>True if the job category represents a single job for combat; otherwise, false.</returns>
    public static bool IsSingleJobForCombat(this ClassJobCategory jobCategory)
    {
        if (jobCategory.RowId == 68) return true; // ACN SMN SCH 
        var str = jobCategory.Name.ToString().Replace(" ", "");
        bool allUpper = true;
        foreach (char c in str)
        {
            if (!char.IsUpper(c)) { allUpper = false; break; }
        }
        if (!allUpper) return false;
        if (str.Length is not 3 and not 6) return false;
        return true;
    }

    /// <summary>
    /// Indents each line of the string with four spaces.
    /// </summary>
    /// <param name="str">The string to indent.</param>
    /// <returns>The indented string.</returns>
    public static string Table(this string str) => "    " + str.Replace("\n", "\n    ");

    /// <summary>
    /// Inserts spaces before each uppercase letter in the string.
    /// </summary>
    /// <param name="str">The string to modify.</param>
    /// <returns>The modified string with spaces.</returns>
    public static string Space(this string str)
    {
        var result = new StringBuilder();
        bool lower = false;

        foreach (var c in str)
        {
            var isLower = char.IsLower(c);
            if (lower && !isLower)
            {
                result.Append(' ');
            }
            lower = isLower;
            result.Append(c);
        }

        return result.ToString();
    }

    /// <summary>
    /// Removes non-ASCII characters from the string.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The string containing only ASCII characters.</returns>
    public static string OnlyAscii(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var sb = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            if (char.IsAscii(c)) sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Converts the string to PascalCase.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The PascalCase string.</returns>
    public static string ToPascalCase(this string input)
    {
        string cleaned = InvalidCharsRgx().Replace(WhiteSpace().Replace(input, "_"), string.Empty);
        string[] parts = cleaned.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (string w0 in parts)
        {
            string w = StartWithLowerCaseChar().Replace(w0, m => m.Value.ToUpper());
            w = FirstCharFollowedByUpperCasesOnly().Replace(w, m => m.Value.ToLower());
            w = LowerCaseNextToNumber().Replace(w, m => m.Value.ToUpper());
            w = UpperCaseInside().Replace(w, m => m.Value.ToLower());
            sb.Append(w);
        }
        var result = sb.ToString();

        if (result.Length > 0 && char.IsNumber(result[0]))
        {
            result = "_" + result;
        }
        return result;
    }

    [GeneratedRegex("[^_a-zA-Z0-9]")]
    private static partial Regex InvalidCharsRgx();
    [GeneratedRegex("(?<=\\s)")]
    private static partial Regex WhiteSpace();
    [GeneratedRegex("^[a-z]")]
    private static partial Regex StartWithLowerCaseChar();
    [GeneratedRegex("(?<=[A-Z])[A-Z0-9]+$")]
    private static partial Regex FirstCharFollowedByUpperCasesOnly();
    [GeneratedRegex("(?<=[0-9])[a-z]")]
    private static partial Regex LowerCaseNextToNumber();
    [GeneratedRegex("(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))")]
    private static partial Regex UpperCaseInside();

    /// <summary>
    /// Generates a property with an array of names.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="propertyType">The type of the property.</param>
    /// <param name="modifier">The modifier for the property.</param>
    /// <param name="items">The items to include in the array.</param>
    /// <returns>The generated property code.</returns>
    public static string ArrayNames(string propertyName, string propertyType, string modifier, params string[] items)
    {
        var thisItems = $"""
            [
                {string.Join(", ", items)},
                {(modifier.Contains("override") ? $"..base.{propertyName}," : string.Empty)}
            ]
            """;
        return $$"""
        private {{propertyType}}[] _{{propertyName}} = null;

        /// <inheritdoc/>
        {{modifier}} {{propertyType}}[] {{propertyName}} => _{{propertyName}} ??= {{thisItems}};
        """;
    }

    /// <summary>
    /// Generates code for an action.
    /// </summary>
    /// <param name="item">The action item.</param>
    /// <param name="actionName">The name of the action.</param>
    /// <param name="actionDescName">The description name of the action.</param>
    /// <param name="desc">The description of the action.</param>
    /// <param name="isDuty">Indicates if the action is a duty action.</param>
    /// <returns>The generated action code.</returns>
    public static string ToCode(this Lumina.Excel.Sheets.Action item,
        string actionName, string actionDescName, string desc, bool isDuty)
    {
        if (isDuty)
        {
            actionDescName += " Duty Action";
        }

        return $$"""
        private readonly Lazy<IBaseAction> _{{actionName}}Creator = new(() => 
        {
            IBaseAction action = new BaseAction((ActionID){{item.RowId}}, {{isDuty.ToString().ToLower()}});
            CustomRotation.LoadActionSetting(ref action);

            var setting = action.Setting;
            Modify{{actionName}}(ref setting);
            action.Setting = setting;

            return action;
        });

        /// <summary>
        /// Modify {{actionDescName}}
        /// </summary>
        static partial void Modify{{actionName}}(ref ActionSetting setting);

        /// <summary>
        /// {{actionDescName}}
        /// {{desc}}
        /// </summary>
        {{(isDuty ? $"[ID({item.RowId})]" : string.Empty)}}
        {{(item.ActionCategory.RowId is 15 ? "private" : "public")}} IBaseAction {{actionName}} => _{{actionName}}Creator.Value;
        """;
    }

    /// <summary>
    /// Sanitizes a description string to be safe for XML documentation comments.
    /// </summary>
    /// <param name="description">The description to sanitize.</param>
    /// <returns>The sanitized description safe for XML comments.</returns>
    public static string SanitizeXmlDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
            return description;

        // Remove or replace problematic HTML-like tags that are not valid XML
        // Handle colortype tags with parentheses and their remnants
        description = System.Text.RegularExpressions.Regex.Replace(description, @"<colortype\([^)]*\)>", "", RegexOptions.IgnoreCase);
        description = System.Text.RegularExpressions.Regex.Replace(description, @"</colortype>", "", RegexOptions.IgnoreCase);
        
        // Handle edgecolortype tags with parentheses and their remnants
        description = System.Text.RegularExpressions.Regex.Replace(description, @"<edgecolortype\([^)]*\)>", "", RegexOptions.IgnoreCase);
        description = System.Text.RegularExpressions.Regex.Replace(description, @"</edgecolortype>", "", RegexOptions.IgnoreCase);
        
        // Handle if tags with parentheses and their remnants
        description = System.Text.RegularExpressions.Regex.Replace(description, @"<if\([^)]*\)>", "", RegexOptions.IgnoreCase);
        description = System.Text.RegularExpressions.Regex.Replace(description, @"</if>", "", RegexOptions.IgnoreCase);
        
        // Handle other problematic tags
        description = System.Text.RegularExpressions.Regex.Replace(description, @"</?br/?>", " ", RegexOptions.IgnoreCase);
        description = System.Text.RegularExpressions.Regex.Replace(description, @"</?indent/?>", "", RegexOptions.IgnoreCase);
        description = System.Text.RegularExpressions.Regex.Replace(description, @"</?sheet\([^)]*\)/?>", "", RegexOptions.IgnoreCase);
        description = System.Text.RegularExpressions.Regex.Replace(description, @"</?value\([^)]*\)/?>", "", RegexOptions.IgnoreCase);
        
        // Remove any remaining tags with parentheses in them
        description = System.Text.RegularExpressions.Regex.Replace(description, @"<[^>]*\([^>]*\)[^>]*>", "", RegexOptions.IgnoreCase);
        
        // Clean up tag remnants like ,)> or similar patterns that might be left behind
        description = System.Text.RegularExpressions.Regex.Replace(description, @"[,)]\s*>", "", RegexOptions.IgnoreCase);
        description = System.Text.RegularExpressions.Regex.Replace(description, @"<\s*[,)]", "", RegexOptions.IgnoreCase);
        
        // Remove standalone parentheses and commas that might be remnants
        description = System.Text.RegularExpressions.Regex.Replace(description, @"\s*[,)]\s*(?=\s|$)", "", RegexOptions.IgnoreCase);
        
        // Clean up multiple spaces
        description = System.Text.RegularExpressions.Regex.Replace(description, @"\s+", " ");
        
        // Clean up any remaining invalid XML characters
        description = System.Text.RegularExpressions.Regex.Replace(description, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", "");
        
        // Escape XML special characters, but preserve the structure for later XML processing
        // Only escape & and quotes, let the XML processing handle < and > for valid XML tags
        description = description.Replace("&", "&amp;");
        description = description.Replace("\"", "&quot;");
        
        return description.Trim();
    }

    /// <summary>
    /// Gets the description name for an action.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <returns>The description name of the action.</returns>
    public static string GetDescName(this Lumina.Excel.Sheets.Action action)
    {
        var jobs = action.ClassJobCategory.IsValid ? action.ClassJobCategory.Value.Name.ToString() : string.Empty;
        jobs = string.IsNullOrEmpty(jobs) ? string.Empty : $" ({jobs})";

        var cate = (action.IsPvP || action.Unknown16) ? " <i>PvP</i>" : " <i>PvE</i>";

        return $"<see href=\"https://garlandtools.org/db/#action/{action.RowId}\"><strong>{action.Name}</strong></see>{cate}{jobs} [{action.RowId}] [{(action.ActionCategory.IsValid ? action.ActionCategory.Value.Name.ToString() : string.Empty)}]";
    }
}
