using System.Text.RegularExpressions;
using ScormGen.Core.Loading;
using ScormGen.Core.Models;

namespace ScormGen.Core.Conversion;

// Parses the custom .scorm.md format into a Course object.
// This is a direct port of Python's md_converter.py — it does NOT use Markdig
// because .scorm.md is a custom format with its own syntax, not standard Markdown.
public class MarkdownConverter
{
    // ── Regex patterns (mirrors md_converter.py) ────────────────────────────

    private static readonly Regex FrontmatterFence   = new(@"^---\s*$", RegexOptions.Compiled);
    private static readonly Regex YamlKeyValue       = new(@"^([\w][\w_-]*):\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex PackageHeader      = new(
        @"^##\s+Package:\s*(.+?)\s*\|\s*(informational|ungraded|graded)\s*\|\s*(.+?)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PassingScoreRx     = new(@"^passing_score:\s*([0-9]*\.?[0-9]+)\s*$", RegexOptions.Compiled);
    private static readonly Regex H3                 = new(@"^###\s+h3:\s*(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex H4                 = new(@"^####\s+h4:\s*(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ScenarioStart      = new(@"^\*\*Scenario:\*\*\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex SituationRx        = new(@"^\*\*Situation:\*\*\s*(.*)$", RegexOptions.Compiled);
    private static readonly Regex QuestionRx         = new(@"^\*\*Question:\*\*\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex LetteredOption     = new(@"^-\s+([A-Z])\)\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex AnalysisRx         = new(@"^\s*\*\*Analysis:\*\*\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex CorrectRx          = new(@"^Correct:\s*([A-Z])\s*$", RegexOptions.Compiled);
    private static readonly Regex KeyInsightRx       = new(@"^Key Insight:\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex ExplanationRx      = new(@"^Explanation:\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex PlainBullet        = new(@"^-\s+(.+)$", RegexOptions.Compiled);

    private readonly CourseLoader _loader = new();

    public Course ConvertFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");
        return Convert(File.ReadAllText(path, System.Text.Encoding.UTF8));
    }

    public Course Convert(string markdownContent)
    {
        var lines = markdownContent.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        var dict = ParseScormMd(lines);

        // Validate by round-tripping through CourseLoader
        var json = System.Text.Json.JsonSerializer.Serialize(dict,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
        return _loader.Load(json);
    }

    private Dictionary<string, object?> ParseScormMd(string[] lines)
    {
        var (fields, i) = ParseFrontmatter(lines);

        foreach (var req in new[] { "course_id", "title" })
        {
            if (!fields.TryGetValue(req, out var v) || string.IsNullOrWhiteSpace(v))
                throw new FormatException($"Frontmatter missing required field: {req}");
        }

        var packages = new List<Dictionary<string, object?>>();
        int pkgNum = 1;

        while (i < lines.Length)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) { i++; continue; }

            var pm = PackageHeader.Match(line);
            if (!pm.Success) { i++; continue; }

            var fileName    = pm.Groups[1].Value.Trim();
            var contentType = pm.Groups[2].Value.Trim().ToLowerInvariant();
            var pkgTitle    = pm.Groups[3].Value.Trim();
            i++;

            // Skip blanks, check for optional passing_score
            while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i])) i++;
            double passingScore = 0.8;
            if (i < lines.Length)
            {
                var psm = PassingScoreRx.Match(lines[i]);
                if (psm.Success) { passingScore = double.Parse(psm.Groups[1].Value); i++; }
            }

            var (content, nextI) = ParsePackageContent(lines, i);
            i = nextI;

            var pkg = new Dictionary<string, object?>
            {
                ["file_number"]  = pkgNum,
                ["file_name"]    = fileName,
                ["content_type"] = contentType,
                ["title"]        = pkgTitle,
                ["content"]      = content,
            };
            if (contentType == "graded")
                pkg["passing_score"] = passingScore;

            packages.Add(pkg);
            pkgNum++;
        }

        return new Dictionary<string, object?>
        {
            ["course_id"] = fields["course_id"],
            ["title"]     = fields["title"],
            ["version"]   = fields.TryGetValue("version", out var ver) ? ver : "1.0",
            ["packages"]  = packages,
        };
    }

    // ── Frontmatter ──────────────────────────────────────────────────────────

    private (Dictionary<string, string> fields, int nextIndex) ParseFrontmatter(string[] lines)
    {
        if (lines.Length == 0 || !FrontmatterFence.IsMatch(lines[0]))
            throw new FormatException(
                "File must start with a --- frontmatter block containing course_id, title, and version.");

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int idx = 1; idx < lines.Length; idx++)
        {
            if (FrontmatterFence.IsMatch(lines[idx]))
                return (fields, idx + 1);
            var m = YamlKeyValue.Match(lines[idx]);
            if (m.Success)
                fields[m.Groups[1].Value] = StripYamlQuotes(m.Groups[2].Value);
        }
        throw new FormatException("Frontmatter block was never closed with ---");
    }

    private static string StripYamlQuotes(string value)
    {
        var v = value.Trim();
        if (v.Length >= 2 && ((v[0] == '"' && v[^1] == '"') || (v[0] == '\'' && v[^1] == '\'')))
            return v[1..^1];
        return v;
    }

    // ── Package content ───────────────────────────────────────────────────────

    private (List<Dictionary<string, object?>> content, int nextIndex) ParsePackageContent(
        string[] lines, int start)
    {
        var content = new List<Dictionary<string, object?>>();
        var pendingBullets = new List<string>();
        var pendingPara = new List<string>();
        int i = start;

        void EmitParagraph()
        {
            if (pendingPara.Count == 0) return;
            var text = string.Join(" ", pendingPara).Trim();
            if (text.Length > 0)
                content.Add(new() { ["type"] = "paragraph", ["text"] = text });
            pendingPara.Clear();
        }

        void EmitBullets()
        {
            if (pendingBullets.Count == 0) return;
            content.Add(new() { ["type"] = "bulleted_list", ["items"] = new List<string>(pendingBullets) });
            pendingBullets.Clear();
        }

        while (i < lines.Length)
        {
            var line = lines[i];

            if (PackageHeader.IsMatch(line)) break;

            if (string.IsNullOrWhiteSpace(line))
            {
                EmitParagraph();
                EmitBullets();
                i++;
                continue;
            }

            var h3m = H3.Match(line);
            if (h3m.Success)
            {
                EmitParagraph(); EmitBullets();
                content.Add(new() { ["type"] = "heading", ["level"] = "h3", ["text"] = h3m.Groups[1].Value.Trim() });
                i++; continue;
            }

            var h4m = H4.Match(line);
            if (h4m.Success)
            {
                EmitParagraph(); EmitBullets();
                content.Add(new() { ["type"] = "heading", ["level"] = "h4", ["text"] = h4m.Groups[1].Value.Trim() });
                i++; continue;
            }

            if (ScenarioStart.IsMatch(line))
            {
                EmitParagraph(); EmitBullets();
                var (item, nextI) = ParseScenarioBlock(lines, i);
                content.Add(item);
                i = nextI;
                continue;
            }

            if (QuestionRx.IsMatch(line))
            {
                EmitParagraph(); EmitBullets();
                var (item, nextI) = ParseQuestionBlock(lines, i);
                content.Add(item);
                i = nextI;
                continue;
            }

            var bullet = PlainBullet.Match(line);
            if (bullet.Success && !LetteredOption.IsMatch(line))
            {
                EmitParagraph();
                pendingBullets.Add(bullet.Groups[1].Value.Trim());
                i++; continue;
            }

            EmitBullets();
            pendingPara.Add(line.Trim());
            i++;
        }

        EmitParagraph();
        EmitBullets();
        return (content, i);
    }

    // ── Scenario block ────────────────────────────────────────────────────────

    private (Dictionary<string, object?> item, int nextIndex) ParseScenarioBlock(string[] lines, int start)
    {
        var scenarioName = ScenarioStart.Match(lines[start]).Groups[1].Value.Trim();
        int i = start + 1;

        while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i])) i++;

        if (i >= lines.Length || !SituationRx.IsMatch(lines[i]))
            throw new FormatException(
                $"Scenario '{scenarioName}' (line {start + 1}): expected **Situation:** line.");

        var sitm = SituationRx.Match(lines[i]);
        i++;

        var situationParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(sitm.Groups[1].Value))
            situationParts.Add(sitm.Groups[1].Value.Trim());

        var currentPara = new List<string>();
        while (i < lines.Length)
        {
            var line = lines[i];
            if (LetteredOption.IsMatch(line) || IsBlockBoundary(line)) break;
            if (string.IsNullOrWhiteSpace(line))
            {
                if (currentPara.Count > 0) { situationParts.Add(string.Join(" ", currentPara)); currentPara.Clear(); }
            }
            else currentPara.Add(line.Trim());
            i++;
        }
        if (currentPara.Count > 0) situationParts.Add(string.Join(" ", currentPara));
        var situation = string.Join("\n\n", situationParts.Where(p => p.Length > 0));

        var options = new List<Dictionary<string, string>>();
        Dictionary<string, string>? currentOpt = null;
        var correctOption = string.Empty;
        var keyInsight = string.Empty;

        while (i < lines.Length)
        {
            var line = lines[i];
            var optm  = LetteredOption.Match(line);
            var anlm  = AnalysisRx.Match(line);
            var corrm = CorrectRx.Match(line);
            var keym  = KeyInsightRx.Match(line);

            if (optm.Success)
            {
                if (currentOpt != null) options.Add(currentOpt);
                currentOpt = new() { ["letter"] = optm.Groups[1].Value, ["text"] = optm.Groups[2].Value.Trim(), ["analysis"] = "" };
            }
            else if (anlm.Success && currentOpt != null)
                currentOpt["analysis"] = anlm.Groups[1].Value.Trim();
            else if (corrm.Success)
            {
                if (currentOpt != null) { options.Add(currentOpt); currentOpt = null; }
                correctOption = corrm.Groups[1].Value;
            }
            else if (keym.Success)
            {
                keyInsight = keym.Groups[1].Value.Trim();
                i++; break;
            }
            else if (IsBlockBoundary(line)) break;

            i++;
        }

        if (string.IsNullOrEmpty(correctOption))
            throw new FormatException($"Scenario '{scenarioName}' (line {start + 1}): missing 'Correct: X' line.");

        return (new Dictionary<string, object?>
        {
            ["type"]           = "scenario",
            ["name"]           = scenarioName,
            ["situation"]      = situation,
            ["options"]        = options,
            ["correct_option"] = correctOption,
            ["key_insight"]    = keyInsight,
        }, i);
    }

    // ── Multiple-choice block ─────────────────────────────────────────────────

    private (Dictionary<string, object?> item, int nextIndex) ParseQuestionBlock(string[] lines, int start)
    {
        var qm = QuestionRx.Match(lines[start]);
        var questionParts = new List<string> { qm.Groups[1].Value.Trim() };
        int i = start + 1;

        while (i < lines.Length)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line) || LetteredOption.IsMatch(line) || IsBlockBoundary(line)) break;
            questionParts.Add(line.Trim());
            i++;
        }
        var question = string.Join(" ", questionParts).Trim();

        var options = new List<Dictionary<string, string>>();
        var correctAnswer = string.Empty;
        var explanation = string.Empty;

        while (i < lines.Length)
        {
            var line = lines[i];
            var optm  = LetteredOption.Match(line);
            var corrm = CorrectRx.Match(line);
            var explm = ExplanationRx.Match(line);

            if (optm.Success)
                options.Add(new() { ["letter"] = optm.Groups[1].Value, ["text"] = optm.Groups[2].Value.Trim() });
            else if (corrm.Success)
                correctAnswer = corrm.Groups[1].Value;
            else if (explm.Success)
            {
                explanation = explm.Groups[1].Value.Trim();
                i++;
                while (i < lines.Length)
                {
                    var next = lines[i];
                    if (string.IsNullOrWhiteSpace(next) || IsBlockBoundary(next) || CorrectRx.IsMatch(next)) break;
                    explanation += " " + next.Trim();
                    i++;
                }
                break;
            }
            else if (string.IsNullOrWhiteSpace(line) && correctAnswer.Length > 0)
            {
                i++; break;
            }
            else if (IsBlockBoundary(line)) break;

            i++;
        }

        if (string.IsNullOrEmpty(correctAnswer))
            throw new FormatException($"Question '{question[..Math.Min(60, question.Length)]}' (line {start + 1}): missing 'Correct: X' line.");

        return (new Dictionary<string, object?>
        {
            ["type"]           = "multiple_choice",
            ["question"]       = question,
            ["options"]        = options,
            ["correct_answer"] = correctAnswer,
            ["explanation"]    = explanation,
        }, i);
    }

    private bool IsBlockBoundary(string line) =>
        PackageHeader.IsMatch(line) ||
        H3.IsMatch(line) ||
        H4.IsMatch(line) ||
        ScenarioStart.IsMatch(line) ||
        QuestionRx.IsMatch(line);
}
