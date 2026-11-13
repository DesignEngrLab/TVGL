using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PolygonImportExport;

/// <summary>Resolves static SVG visibility for geometry import without fetching stylesheets or executing scripts.</summary>
internal static class SvgVisibility
{
    private sealed record Declaration(string Name, string Value, bool Important);
    private sealed record Rule(string Selector, (int Id, int Class, int Type) Specificity, Declaration[] Declarations);
    private sealed record State(bool Excluded, string Display, string Visibility, double Opacity);
    private static readonly HashSet<string> Properties = new(StringComparer.OrdinalIgnoreCase) { "display", "visibility", "opacity" };
    private static readonly Regex Token = new(@"^(?<type>[a-zA-Z_][\w-]*|\*)?(?<suffix>(?:[.#][\w-]+|\[[\w-]+(?:\s*=\s*(?:'[^']*'|""[^""]*""|[\w-]+))?\])*)$", RegexOptions.CultureInvariant);
    private static readonly Regex Suffix = new(@"([.#])([\w-]+)|\[([\w-]+)(?:\s*=\s*('[^']*'|""[^""]*""|[\w-]+))?\]", RegexOptions.CultureInvariant);

    /// <summary>Returns visible graphics, honoring presentation attributes, CSS cascade and ancestor suppression.</summary>
    internal static XElement[] VisibleElements(XDocument document)
    {
        if (document.Nodes().OfType<XProcessingInstruction>().Any(p => p.Target == "xml-stylesheet")
            || document.Descendants().Any(e => e.Name.LocalName == "link" && ((string?)e.Attribute("rel"))?.Contains("stylesheet", StringComparison.OrdinalIgnoreCase) == true))
            throw new NotSupportedException("SVG external stylesheets are not supported; inline visibility styles before import.");
        var rules = new List<Rule>();
        foreach (var style in document.Descendants().Where(e => e.Name.LocalName == "style"))
        {
            if (style.Attribute("type") is { } type && type.Value != "text/css") continue;
            if (style.Attribute("media") is { } media && media.Value.Trim() is not ("" or "all"))
                throw new NotSupportedException("SVG conditional stylesheet media is not supported; resolve styles before import.");
            var css = RemoveComments(style.Value);
            if (css.Contains('@') || css.Contains('{') && Regex.IsMatch(css, @"\{[^}]*\{"))
                throw new NotSupportedException("SVG CSS at-rules and nested rules are not supported; flatten the stylesheet before import.");
            foreach (Match block in Regex.Matches(css, @"([^{}]+)\{([^{}]*)\}"))
            {
                var declarations = ParseDeclarations(block.Groups[2].Value);
                if (declarations.Length == 0) continue;
                foreach (var selector in block.Groups[1].Value.Split(','))
                {
                    var normalized = NormalizeSelector(selector);
                    var pieces = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (pieces.Length == 0 || pieces[0] is ">" or "+" or "~" || pieces[^1] is ">" or "+" or "~"
                        || pieces.Zip(pieces.Skip(1)).Any(pair => pair.First is ">" or "+" or "~" && pair.Second is ">" or "+" or "~")
                        || pieces.Where(p => p is not (">" or "+" or "~")).Any(p => !Token.IsMatch(p)))
                        throw new NotSupportedException($"Unsupported SVG visibility selector: {selector.Trim()}. Flatten CSS before import.");
                    var suffixes = pieces.SelectMany(p => Suffix.Matches(Token.Match(p).Groups["suffix"].Value).Cast<Match>()).ToArray();
                    var specificity = (suffixes.Count(m => m.Groups[1].Value == "#"),
                        suffixes.Count(m => m.Groups[1].Value == "." || m.Groups[3].Success),
                        pieces.Count(p => Token.Match(p).Groups["type"].Value is not ("" or "*")));
                    rules.Add(new Rule(normalized, specificity, declarations));
                }
            }
        }
        var states = new Dictionary<XElement, State>();
        var visible = new List<XElement>();
        foreach (var element in document.Descendants())
        {
            var parent = element.Parent != null ? states.GetValueOrDefault(element.Parent) : null;
            var values = new Dictionary<string, (string Value, (int Important, int Inline, int Id, int Class, int Type, int Order) Rank)>();
            void Apply(Declaration declaration, int inline, (int Id, int Class, int Type) specificity, int order)
            {
                var rank = (declaration.Important ? 1 : 0, inline, specificity.Id, specificity.Class, specificity.Type, order);
                if (!values.TryGetValue(declaration.Name, out var prior) || rank.CompareTo(prior.Rank) >= 0)
                    values[declaration.Name] = (declaration.Value, rank);
            }
            foreach (var name in Properties)
                if (element.Attribute(name) is { } attribute) Apply(new(name, attribute.Value.Trim().ToLowerInvariant(), false), 0, (0, 0, 0), -1);
            int order = 0;
            foreach (var rule in rules)
                if (Matches(element, rule.Selector))
                    foreach (var declaration in rule.Declarations) Apply(declaration, 0, rule.Specificity, order++);
            foreach (var declaration in ParseDeclarations((string?)element.Attribute("style") ?? "")) Apply(declaration, 1, (0, 0, 0), order++);
            string Resolve(string name, string initial, string inherited, bool inherits)
            {
                if (!values.TryGetValue(name, out var entry)) return inherits ? inherited : initial;
                return entry.Value switch { "inherit" => inherited, "initial" => initial, "unset" => inherits ? inherited : initial, _ => entry.Value };
            }
            var display = Resolve("display", "inline", parent?.Display ?? "inline", false);
            var visibility = Resolve("visibility", "visible", parent?.Visibility ?? "visible", true);
            var opacityText = Resolve("opacity", "1", (parent?.Opacity ?? 1).ToString(CultureInfo.InvariantCulture), false);
            if (display is not ("none" or "inline" or "block" or "inline-block" or "contents" or "list-item" or "table" or "inline-table" or "table-row" or "table-cell" or "table-row-group" or "table-header-group" or "table-footer-group" or "table-column" or "table-column-group" or "table-caption" or "flex" or "inline-flex" or "grid" or "inline-grid" or "flow-root") || visibility is not ("visible" or "hidden" or "collapse")
                || !double.TryParse(opacityText.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out var opacity) || !double.IsFinite(opacity))
                throw new NotSupportedException("Unsupported SVG visibility value; resolve dynamic CSS before import.");
            if (opacityText.EndsWith('%')) opacity /= 100;
            opacity = Math.Clamp(opacity, 0, 1);
            var excluded = parent?.Excluded == true || display == "none" || opacity == 0
                || element.Name.LocalName is "defs" or "symbol" or "clipPath" or "mask" or "pattern" or "marker" or "metadata" or "style" or "script";
            states[element] = new State(excluded, display, visibility, opacity);
            if (!excluded && visibility == "visible" && (element.Name.NamespaceName is "" or "http://www.w3.org/2000/svg")) visible.Add(element);
        }
        return visible.ToArray();
    }

    /// <summary>Reads visibility-related declarations, preserving order and important priority.</summary>
    private static Declaration[] ParseDeclarations(string style)
    {
        var result = new List<Declaration>();
        foreach (var text in SplitDeclarations(style))
        {
            int colon = text.IndexOf(':');
            if (colon < 0) continue;
            var name = text[..colon].Trim().ToLowerInvariant();
            if (!Properties.Contains(name)) continue;
            var value = text[(colon + 1)..].Trim().ToLowerInvariant();
            bool important = Regex.IsMatch(value, @"!\s*important\s*$");
            value = Regex.Replace(value, @"!\s*important\s*$", "").Trim();
            if (value.Length > 0) result.Add(new(name, value, important));
        }
        return result.ToArray();
    }

    /// <summary>Splits declarations only outside quoted strings, escapes, comments and nested CSS component values.</summary>
    private static IEnumerable<string> SplitDeclarations(string style)
    {
        var text = new System.Text.StringBuilder();
        var closers = new Stack<char>();
        char quote = '\0';
        for (int i = 0; i < style.Length; i++)
        {
            char c = style[i];
            if (c == '\\')
            {
                text.Append(c);
                if (++i >= style.Length) throw new NotSupportedException("Incomplete SVG CSS escape.");
                text.Append(style[i]);
                continue;
            }
            if (quote != '\0')
            {
                text.Append(c);
                if (c == quote) quote = '\0';
                continue;
            }
            if (c == '/' && i + 1 < style.Length && style[i + 1] == '*')
            {
                int end = style.IndexOf("*/", i + 2, StringComparison.Ordinal);
                if (end < 0) throw new NotSupportedException("Unterminated SVG CSS comment.");
                i = end + 1;
                continue;
            }
            if (c is '\'' or '"') quote = c;
            else if (c is '(' or '[' or '{') closers.Push(c == '(' ? ')' : c == '[' ? ']' : '}');
            else if (c is ')' or ']' or '}')
            {
                if (closers.Count == 0 || closers.Pop() != c) throw new NotSupportedException("Unbalanced SVG CSS value.");
            }
            if (c == ';' && quote == '\0' && closers.Count == 0)
            {
                yield return text.ToString(); text.Clear();
            }
            else text.Append(c);
        }
        if (quote != '\0' || closers.Count != 0) throw new NotSupportedException("Unterminated SVG CSS value.");
        yield return text.ToString();
    }

    /// <summary>Removes stylesheet comments without treating comment markers inside strings as comments.</summary>
    private static string RemoveComments(string css)
    {
        var text = new System.Text.StringBuilder();
        char quote = '\0';
        for (int i = 0; i < css.Length; i++)
        {
            char c = css[i];
            if (c == '\\')
            {
                text.Append(c);
                if (++i >= css.Length) throw new NotSupportedException("Incomplete SVG CSS escape.");
                text.Append(css[i]);
                continue;
            }
            if (quote != '\0')
            {
                if (c == quote) quote = '\0';
            }
            else if (c is '\'' or '"') quote = c;
            else if (c == '/' && i + 1 < css.Length && css[i + 1] == '*')
            {
                int end = css.IndexOf("*/", i + 2, StringComparison.Ordinal);
                if (end < 0) throw new NotSupportedException("Unterminated SVG CSS comment.");
                i = end + 1;
                continue;
            }
            text.Append(c);
        }
        if (quote != '\0') throw new NotSupportedException("Unterminated SVG CSS string.");
        return text.ToString();
    }

    /// <summary>Normalizes selector combinators while rejecting whitespace-containing attribute values conservatively.</summary>
    private static string NormalizeSelector(string selector) => Regex.Replace(Regex.Replace(selector.Trim(), @"\s*([>+~])\s*", " $1 "), @"\s+", " ");

    /// <summary>Matches type/class/ID/attribute selectors with descendant, child and sibling combinators.</summary>
    private static bool Matches(XElement element, string selector)
    {
        var tokens = selector.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        bool At(XElement current, int index)
        {
            if (!MatchesCompound(current, tokens[index])) return false;
            if (index == 0) return true;
            var relation = tokens[index - 1];
            if (relation == ">") return current.Parent != null && At(current.Parent, index - 2);
            if (relation == "+") return current.ElementsBeforeSelf().LastOrDefault() is { } previous && At(previous, index - 2);
            if (relation == "~") return current.ElementsBeforeSelf().Any(previous => At(previous, index - 2));
            return current.Ancestors().Any(ancestor => At(ancestor, index - 1));
        }
        return At(element, tokens.Length - 1);
    }

    /// <summary>Matches one simple compound selector using case-sensitive SVG names, IDs, classes and attribute values.</summary>
    private static bool MatchesCompound(XElement element, string selector)
    {
        var type = Token.Match(selector).Groups["type"].Value;
        if (type is not ("" or "*") && type != element.Name.LocalName) return false;
        foreach (Match suffix in Suffix.Matches(selector[type.Length..]))
        {
            if (suffix.Groups[1].Value == "#" && (string?)element.Attribute("id") != suffix.Groups[2].Value) return false;
            if (suffix.Groups[1].Value == "." && !Regex.Split((string?)element.Attribute("class") ?? "", @"\s+").Contains(suffix.Groups[2].Value)) return false;
            if (suffix.Groups[3].Success)
            {
                var attribute = element.Attribute(suffix.Groups[3].Value);
                if (attribute == null || suffix.Groups[4].Success && attribute.Value != suffix.Groups[4].Value.Trim('\'', '"')) return false;
            }
        }
        return true;
    }
}
