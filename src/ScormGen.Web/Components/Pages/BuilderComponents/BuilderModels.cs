namespace ScormGen.Web.Components.Pages.BuilderComponents;

public static class BuilderStyles
{
    public const string Input    = "w-full border border-gray-300 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary/50";
    public const string Textarea = "w-full border border-gray-300 rounded-lg px-4 py-2 text-sm resize-y focus:outline-none focus:ring-2 focus:ring-primary/50";
    public const string AddBtn   = "text-xs px-3 py-1.5 text-primary border border-primary/30 rounded-lg hover:bg-primary/10 transition-colors";
}

public class BPackage
{
    public string FileName { get; set; } = "";
    public string Title { get; set; } = "";
    public string ContentType { get; set; } = "informational";
    public int PassingScorePercent { get; set; } = 80;
    public double PassingScore => PassingScorePercent / 100.0;
    public List<BContentItem> Content { get; } = new();
    public bool Expanded { get; set; } = true;
}

public abstract class BContentItem { public abstract string TypeLabel { get; } }

public sealed class BHeading : BContentItem
{
    public override string TypeLabel => "Heading";
    public string Level { get; set; } = "h2";
    public string Text { get; set; } = "";
}

public sealed class BParagraph : BContentItem
{
    public override string TypeLabel => "Paragraph";
    public string Text { get; set; } = "";
}

public sealed class BBulletedList : BContentItem
{
    public override string TypeLabel => "Bulleted List";
    public string RawText { get; set; } = "";
    public List<string> Items =>
        RawText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
               .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
}

public sealed class BMultipleChoice : BContentItem
{
    public override string TypeLabel => "Multiple Choice";
    public string Question { get; set; } = "";
    public List<BMcOption> Options { get; } = new() { new(), new(), new(), new() };
    public string CorrectAnswer { get; set; } = "";
    public string Explanation { get; set; } = "";
}

public sealed class BMcOption { public string Text { get; set; } = ""; }

public sealed class BScenario : BContentItem
{
    public override string TypeLabel => "Scenario";
    public string Name { get; set; } = "";
    public string Situation { get; set; } = "";
    public List<BScenarioOption> Options { get; } = new() { new(), new() };
    public string CorrectOption { get; set; } = "";
    public string KeyInsight { get; set; } = "";
}

public sealed class BScenarioOption
{
    public string Text { get; set; } = "";
    public string Analysis { get; set; } = "";
}
