using System.IO.Compression;
using System.Net;
using System.Text.Json;
using ScormGen.Core.Models;
using ScormGen.Core.Templates;

namespace ScormGen.Core.Packaging;

public abstract class ScormPackageGeneratorBase : IPackageGenerator
{
    protected abstract string ApiJs { get; }

    protected abstract string BuildManifest(string identifier, string title, string contentType, double passingScore);

    public byte[] PackageCourse(Course course)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var packagePaths = new List<string>();
            foreach (var pkg in course.Packages)
            {
                var zipPath = Path.Combine(tempDir, $"{pkg.FileName}.zip");
                var zipBytes = BuildPackageZip(pkg);
                File.WriteAllBytes(zipPath, zipBytes);
                packagePaths.Add(zipPath);
            }

            using var ms = new MemoryStream();
            using (var outer = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var path in packagePaths)
                    outer.CreateEntryFromFile(path, Path.GetFileName(path));
            }
            return ms.ToArray();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private byte[] BuildPackageZip(ScormPackage pkg)
    {
        var html = pkg.ContentType.ToLowerInvariant() switch
        {
            "informational" => GenerateInformational(pkg),
            "ungraded" => pkg.Content.Any(c => c is Scenario)
                ? GenerateScenario(pkg)
                : GenerateUngradedQuiz(pkg),
            "graded" => GenerateGradedQuiz(pkg),
            _ => throw new ArgumentException($"Unknown content_type '{pkg.ContentType}'")
        };

        var manifest = BuildManifest(pkg.FileName, pkg.Title, pkg.ContentType, pkg.PassingScore);

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(zip, "index.html", html);
            AddEntry(zip, "scorm_api.js", ApiJs);
            AddEntry(zip, "imsmanifest.xml", manifest);
        }
        return ms.ToArray();
    }

    private static void AddEntry(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), System.Text.Encoding.UTF8);
        writer.Write(content);
    }

    // -------------------------------------------------------------------------
    // HTML generation — shared across all SCORM format versions
    // -------------------------------------------------------------------------

    private string GenerateInformational(ScormPackage pkg)
    {
        var parts = new List<string>();
        foreach (var item in pkg.Content)
        {
            switch (item)
            {
                case Heading h:
                    parts.Add($"<{h.Level}>{Escape(h.Text)}</{h.Level}>");
                    break;
                case Paragraph p:
                    parts.Add($"<p>{Escape(p.Text)}</p>");
                    break;
                case BulletedList bl:
                    var lis = string.Concat(bl.Items.Select(i => $"<li>{Escape(i)}</li>"));
                    parts.Add($"<ul>{lis}</ul>");
                    break;
            }
        }
        return HtmlTemplates.GetInformationalHtml(pkg.Title, string.Join("\n", parts), ApiJs);
    }

    private string GenerateScenario(ScormPackage pkg)
    {
        var scenario = pkg.Content.OfType<Scenario>().First();

        var optionsHtml = string.Concat(scenario.Options.Select(opt => $"""

            <button type="button" class="scenario-option" data-option="{opt.Letter}" onclick="selectOption('{opt.Letter}')">
                <span class="scenario-option-heading">Option {Escape(opt.Letter)}</span>
                <span class="scenario-option-body">{Escape(opt.Text)}</span>
            </button>
"""));

        var analysisHtml = string.Concat(scenario.Options.Select(opt =>
            $"<p><strong>Option {Escape(opt.Letter)}:</strong> {Escape(opt.Analysis)}</p>"));

        var additionalHtml = !string.IsNullOrEmpty(scenario.KeyInsight)
            ? $"""

            <div class="additional-info">
                <h4>Key Insight</h4>
                <p>{Escape(scenario.KeyInsight)}</p>
            </div>
"""
            : string.Empty;

        return HtmlTemplates.GetScenarioHtml(
            title: $"Scenario: {scenario.Name}",
            situation: ParagraphsToHtml(scenario.Situation),
            optionsHtml: optionsHtml,
            analysisHtml: analysisHtml,
            additionalHtml: additionalHtml,
            correctOption: scenario.CorrectOption,
            apiJs: ApiJs);
    }

    private string GenerateUngradedQuiz(ScormPackage pkg)
    {
        var questions = pkg.Content.OfType<MultipleChoice>().ToList();
        var questionsHtml = BuildQuizQuestionsHtml(questions, graded: false);
        return HtmlTemplates.GetUngradedQuizHtml(pkg.Title, questionsHtml, questions.Count, ApiJs);
    }

    private string GenerateGradedQuiz(ScormPackage pkg)
    {
        var questions = pkg.Content.OfType<MultipleChoice>().ToList();
        var questionsHtml = BuildQuizQuestionsHtml(questions, graded: true);

        int questionCount = questions.Count;
        int minCorrect = (int)(questionCount * pkg.PassingScore);
        int passingPercent = (int)(pkg.PassingScore * 100);

        var correctAnswers = new Dictionary<int, string>();
        var explanations = new Dictionary<int, string>();
        for (int i = 0; i < questions.Count; i++)
        {
            correctAnswers[i + 1] = questions[i].CorrectAnswer;
            explanations[i + 1] = questions[i].Explanation;
        }

        var jsonOpts = new JsonSerializerOptions();
        return HtmlTemplates.GetGradedQuizHtml(
            title: pkg.Title,
            questionsHtml: questionsHtml,
            questionCount: questionCount,
            passingScore: pkg.PassingScore,
            passingPercent: passingPercent,
            minCorrect: minCorrect,
            correctAnswersJson: JsonSerializer.Serialize(correctAnswers, jsonOpts),
            explanationsJson: JsonSerializer.Serialize(explanations, jsonOpts),
            apiJs: ApiJs);
    }

    private static string BuildQuizQuestionsHtml(List<MultipleChoice> questions, bool graded)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            int num = i + 1;

            var optionsHtml = string.Concat(q.Options.Select(opt =>
            {
                var onclick = graded
                    ? string.Empty
                    : $" onclick=\"checkAnswer({num}, '{opt.Letter}', '{q.CorrectAnswer}')\"";
                return $"""

                <li class="option-item">
                    <input type="radio" name="q{num}" id="q{num}{opt.Letter}"
                           value="{opt.Letter}"{onclick}>
                    <label for="q{num}{opt.Letter}" class="option-label">
                        <span class="option-letter">{opt.Letter})</span>
                        <span>{Escape(opt.Text)}</span>
                    </label>
                </li>
""";
            }));

            sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"""

            <div class="question-container" id="question-{num}">
                <span class="question-number">{num}</span>
                <span class="question-text" id="question-label-{num}">{Escape(q.Question)}</span>
                <ul class="options-list" role="radiogroup" aria-labelledby="question-label-{num}">
                    {optionsHtml}
                </ul>
""");

            if (!graded)
            {
                var feedbackCorrect = q.Explanation.Length > 0
                    ? $"Correct! {Escape(q.Explanation)}"
                    : "Correct!";
                var feedbackIncorrect =
                    $"Not quite. The correct answer is {q.CorrectAnswer}. {Escape(q.Explanation)}";

                sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"""

                <div class="feedback" id="feedback-{num}" aria-live="polite">
                    <p id="feedback-text-{num}"></p>
                    <p class="explanation">{Escape(q.Explanation)}</p>
                </div>
""");
            }

            sb.Append("\n            </div>");
        }
        return sb.ToString();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string Escape(string text) => WebUtility.HtmlEncode(text);

    private static string ParagraphsToHtml(string text)
    {
        var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0);
        return string.Join("\n", paragraphs.Select(p => $"<p>{Escape(p)}</p>"));
    }
}
