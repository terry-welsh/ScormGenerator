using ScormGen.Core.Conversion;
using ScormGen.Core.Models;

namespace ScormGen.Tests.Unit;

public class MarkdownConverterTests
{

    private const string MinimalCourse = """
        ---
        course_id: TEST001
        title: Test Course
        version: 1.0
        ---

        ## Package: PKG1_INFO | informational | Introduction

        ### h3: Welcome

        This is an introductory paragraph.

        - First bullet
        - Second bullet
        """;

    [Fact]
    public void Convert_MinimalCourse_ReturnsCourse()
    {
        var course = MarkdownConverter.Convert(MinimalCourse);

        Assert.Equal("TEST001", course.CourseId);
        Assert.Equal("Test Course", course.Title);
        Assert.Equal("1.0", course.Version);
        Assert.Single(course.Packages);
        Assert.Equal("informational", course.Packages[0].ContentType);
    }

    [Fact]
    public void Convert_InformationalContent_ParsesHeadingParagraphAndList()
    {
        var course = MarkdownConverter.Convert(MinimalCourse);
        var content = course.Packages[0].Content;

        Assert.Contains(content, i => i is Heading h && h.Level == "h3" && h.Text == "Welcome");
        Assert.Contains(content, i => i is Paragraph p && p.Text.Contains("introductory paragraph"));
        Assert.Contains(content, i => i is BulletedList bl && bl.Items.Count == 2);
    }

    [Fact]
    public void Convert_GradedPackageWithQuestion_ParsesMultipleChoiceAndPassingScore()
    {
        const string md = """
            ---
            course_id: GRADED001
            title: Graded Course
            version: 1.0
            ---

            ## Package: PKG1_QUIZ | graded | Assessment
            passing_score: 0.75

            **Question:** What color is the sky?

            - A) Red
            - B) Blue
            - C) Green
            - D) Purple

            Correct: B
            Explanation: The sky appears blue due to Rayleigh scattering.
            """;

        var course = MarkdownConverter.Convert(md);
        var pkg = course.Packages[0];

        Assert.Equal("graded", pkg.ContentType);
        Assert.Equal(0.75, pkg.PassingScore);

        var mc = Assert.IsType<MultipleChoice>(pkg.Content[0]);
        Assert.Equal("B", mc.CorrectAnswer);
        Assert.Equal(4, mc.Options.Count);
        Assert.Contains("Rayleigh", mc.Explanation);
    }

    [Fact]
    public void Convert_UngradedScenario_ParsesScenarioBlock()
    {
        const string md = """
            ---
            course_id: SCEN001
            title: Scenario Course
            version: 1.0
            ---

            ## Package: PKG1_SCEN | ungraded | Scenario Practice

            **Scenario:** The Difficult Conversation

            **Situation:** Your colleague is consistently late to meetings.

            - A) Ignore it
              **Analysis:** Avoidance rarely solves interpersonal issues.
            - B) Speak privately with them
              **Analysis:** A direct, respectful conversation is usually best.

            Correct: B
            Key Insight: Early intervention prevents escalation.
            """;

        var course = MarkdownConverter.Convert(md);
        var pkg = course.Packages[0];

        var scenario = Assert.IsType<Scenario>(pkg.Content[0]);
        Assert.Equal("The Difficult Conversation", scenario.Name);
        Assert.Equal("B", scenario.CorrectOption);
        Assert.Equal(2, scenario.Options.Count);
        Assert.Contains("escalation", scenario.KeyInsight);
    }

    [Fact]
    public void Convert_MissingFrontmatter_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => MarkdownConverter.Convert("## Package: test | informational | Title"));
    }

    [Fact]
    public void Convert_MissingCourseId_ThrowsFormatException()
    {
        const string md = """
            ---
            title: No ID Course
            ---
            """;
        Assert.Throws<FormatException>(() => MarkdownConverter.Convert(md));
    }

    [Fact]
    public void Convert_UnclosedFrontmatter_ThrowsFormatException()
    {
        const string md = """
            ---
            course_id: X
            title: T
            """;
        Assert.Throws<FormatException>(() => MarkdownConverter.Convert(md));
    }
}
