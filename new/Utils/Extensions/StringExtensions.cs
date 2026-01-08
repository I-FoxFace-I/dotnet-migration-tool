using System.Globalization;
using System.Text;
using System.Globalization;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System;

namespace IvoEngine.Extensions;

public static class StringExtensions
{
    private static Random _random = new Random();
    private static char[] _replacements = new char[] { 'ψ', 'ω', 'ε' };
    private static char[] _removeFromUrlAddress = { '-', '{', '}', '(', ')', '[', ']', ':', '.', ';',
                                                        '|', '*', ' ', '_', '!', '?', '=', '\\' };
    public static bool EqualsIgnoreCase(this string self, string other)
    {
        return string.Equals(self, other, StringComparison.OrdinalIgnoreCase);
    }
    public static IEnumerable<string> GetLines(this string str, bool removeEmptyLines = false)
    {
        if (removeEmptyLines)
        {
            return str.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            return str.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        }
    }
    public static List<string> GetLinesAsList(this string self, bool removeEmptyLines = false)
    {
        if (removeEmptyLines)
        {
            return self.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        else
        {
            return self.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
        }
    }
    public static string AddPrefix(this string self, string prefix)
    {
        return string.Concat(prefix, self);
    }
    public static string AddSuffix(this string self, string suffix)
    {
        return string.Concat(self, suffix);
    }
    public static bool IsSnakeCase(this string self)
    {
        if (string.IsNullOrEmpty(self))
        {
            return false;
        }

        if (self.StartsWith("_") || self.EndsWith("_") || self.Contains("__"))
        {
            return false;
        }

        foreach (char c in self)
        {
            if (!char.IsLower(c) && !char.IsNumber(c) && c != '_')
            {
                return false;
            }
        }

        return true;
    }
    public static bool IsTitleCase(this string self)
    {
        if (string.IsNullOrEmpty(self))
        {
            return false;
        }

        if (!char.IsUpper(self[0]))
        {
            return false;
        }

        for (int i = 1; i < self.Length; i++)
        {
            if (!char.IsLetter(self[i]) && !char.IsDigit(self[i]))
            {
                return false;
            }
        }

        return true;
    }
    public static bool IsCamelCase(this string self)
    {
        if (string.IsNullOrEmpty(self))
        {
            return false;
        }

        if (!char.IsLower(self[0]))
        {
            return false;
        }

        for (int i = 1; i < self.Length; i++)
        {
            if (!char.IsLetterOrDigit(self[i]))
            {
                return false;
            }
        }

        return true;
    }
    private static string SnakeCaseToCamelCase(string self)
    {
        if (string.IsNullOrEmpty(self))
        {
            return self;
        }
        var titleCaseName = SnakeCaseToTitleCase(self);
        return char.ToLower(titleCaseName[0]) + titleCaseName.Substring(1);
    }
    private static string SnakeCaseToTitleCase(string self)
    {
        if (string.IsNullOrEmpty(self))
        {
            return self;
        }

        return self
            .Split('_')
            .Where(segment => !string.IsNullOrEmpty(segment))
            .Select(segment => char.ToUpper(segment[0]) + segment.Substring(1))
            .Aggregate(string.Empty, (current, segment) => current + segment);
    }
    private static string CamelCaseToSnakeCase(string self)
    {
        if (string.IsNullOrEmpty(self))
        {
            return self;
        }

        var builder = new StringBuilder();
        for (int i = 0; i < self.Length; i++)
        {
            char c = self[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    builder.Append('_');
                }
                builder.Append(char.ToLower(c));
            }
            else
            {
                builder.Append(c);
            }
        }
        return builder.ToString();
    }
    private static string CamelCaseToTitleCase(string self)
    {
        if (string.IsNullOrEmpty(self))
        {
            return self;
        }

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(self);
    }
    private static string TitleCaseToSnakeCase(string self)
    {
        if (string.IsNullOrEmpty(self))
        {
            return self;
        }

        var builder = new StringBuilder();
        for (int i = 0; i < self.Length; i++)
        {
            char c = self[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    builder.Append('_');
                }
                builder.Append(char.ToLower(c));
            }
            else
            {
                builder.Append(c);
            }
        }
        return builder.ToString();
    }
    private static string TitleCaseToCamelCase(string self)
    {
        if (string.IsNullOrEmpty(self))
        {
            return self;
        }

        return string.Concat(self[0].ToString().ToLower(), self.Substring(1));
    }
    public static string ToSnakeCase(this string self)
    {
        if (string.IsNullOrEmpty(self))
        {
            return self;
        }
        self = self.Replace('-', '_');
        self = self.Replace('/', '_');
        self = self.ReplaceSpecialCharacters();
        if (self.IsSnakeCase())
        {
            return self;
        }
        else if (self.IsCamelCase())
        {
            return CamelCaseToSnakeCase(self);
        }
        else if (self.IsTitleCase())
        {
            return TitleCaseToSnakeCase(self);
        }
        else
        {
            return TitleCaseToSnakeCase(self);
            //throw new Exception($"Text self {self} must be snake_case, camelCase or TitleCase");
        }
    }
    public static string ToCamelCase(this string self)
    {
        if (string.IsNullOrEmpty(self))
        {
            return self;
        }
        self = self.Replace('-', '_');
        self = self.ReplaceSpecialCharacters();
        if (self.IsSnakeCase())
        {
            return SnakeCaseToCamelCase(self);
        }
        else if (self.IsCamelCase())
        {
            return self;
        }
        else if (self.IsTitleCase())
        {
            return TitleCaseToCamelCase(self);
        }
        else
        {
            return SnakeCaseToCamelCase(self);
            //throw new Exception($"Text self {self} must be snake_case, camelCase or TitleCase");
        }
    }
    public static string ToTileCase(this string self)
    {
        if (string.IsNullOrEmpty(self))
        {
            return self;
        }
        self = self.Replace('-', '_');
        self = self.ReplaceSpecialCharacters();
        if (self.IsSnakeCase())
        {
            return SnakeCaseToTitleCase(self);
        }
        else if (self.IsCamelCase())
        {
            return CamelCaseToTitleCase(self);
        }
        else if (self.IsTitleCase())
        {
            return self;
        }
        else
        {
            return SnakeCaseToTitleCase(self);
        }
    }
    public static string FirstSnakeCaseItem(this string self)
    {
        var retValue = string.Empty;
        if (self != null)
        {
            if (self.Contains("_"))
            {
                retValue = self.Split('_').First();
            }
            else
            {
                retValue = self;
            }
        }
        return retValue;
    }
    public static string ReplaceSpecialCharacters(this string self)
    {
        // Odstranění diakritiky
        //Console.WriteLine(self);
        string normalizedString = self.Normalize(NormalizationForm.FormD);
        //Console.WriteLine(normalizedString);
        StringBuilder stringBuilder = new StringBuilder();

        foreach (char c in normalizedString)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }
        //Console.WriteLine(stringBuilder.ToString());
        string stringWithoutDiacritics = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        //Console.WriteLine(stringWithoutDiacritics);
        // Nahrazení nežádoucích znaků
        string pattern = @"[^a-zA-Z0-9\s_]";
        //Console.WriteLine(Regex.Replace(stringWithoutDiacritics, pattern, new MatchEvaluator(ReplaceMatch)));
        return Regex.Replace(stringWithoutDiacritics, pattern, new MatchEvaluator(ReplaceMatch));
    }
    private static string ReplaceMatch(Match m)
    {
        string replacement = "ψ"; // Můžete změnit na jiný zástupný znak, například "α", "β", atd.
        return replacement;
        //return _replacements[_random.Next(_replacements.Length)].ToString();
    }
    public static string LabelFromUrl(this string self)
    {
        var modifiedText = Regex.Replace(self, "[^0-9A-Za-z /_]", "");
        if (modifiedText.StartsWith('/'))
            modifiedText = modifiedText.Substring(1);
        var splitTextItems = modifiedText.Split("/");
        var label = string.Join('-', splitTextItems.Select(x => x.ToTileCase()));
        return label;
    }
}


