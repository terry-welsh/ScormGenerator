namespace ScormGen.Core;

public static class JsonKeys
{
    // Course
    public const string CourseId    = "course_id";
    public const string Title       = "title";
    public const string Version     = "version";
    public const string Packages    = "packages";

    // Package
    public const string FileNumber   = "file_number";
    public const string FileName     = "file_name";
    public const string ContentType  = "content_type";
    public const string Content      = "content";
    public const string PassingScore = "passing_score";

    // Content item discriminator
    public const string Type = "type";

    // Type discriminator values
    public const string TypeHeading        = "heading";
    public const string TypeParagraph      = "paragraph";
    public const string TypeBulletedList   = "bulleted_list";
    public const string TypeMultipleChoice = "multiple_choice";
    public const string TypeScenario       = "scenario";

    // Shared fields
    public const string Text  = "text";
    public const string Level = "level";
    public const string Items = "items";
    public const string Name  = "name";

    // Scenario fields
    public const string Situation     = "situation";
    public const string Options       = "options";
    public const string Letter        = "letter";
    public const string Analysis      = "analysis";
    public const string CorrectOption = "correct_option";
    public const string KeyInsight    = "key_insight";

    // Multiple choice fields
    public const string Question      = "question";
    public const string CorrectAnswer = "correct_answer";
    public const string Explanation   = "explanation";
}
