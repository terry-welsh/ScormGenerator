using System.Text.Json.Serialization;

namespace ScormGen.Core.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Heading), "heading")]
[JsonDerivedType(typeof(Paragraph), "paragraph")]
[JsonDerivedType(typeof(BulletedList), "bulleted_list")]
[JsonDerivedType(typeof(Scenario), "scenario")]
[JsonDerivedType(typeof(MultipleChoice), "multiple_choice")]
public interface IContentItem { }

public record Heading(string Level, string Text) : IContentItem;

public record Paragraph(string Text) : IContentItem;

public record BulletedList(List<string> Items) : IContentItem;

public record ScenarioOption(string Letter, string Text, string Analysis);

public record Scenario(
    string Name,
    string Situation,
    List<ScenarioOption> Options,
    string CorrectOption,
    string KeyInsight) : IContentItem;

public record MultipleChoiceOption(string Letter, string Text);

public record MultipleChoice(
    string Question,
    List<MultipleChoiceOption> Options,
    string CorrectAnswer,
    string Explanation) : IContentItem;
