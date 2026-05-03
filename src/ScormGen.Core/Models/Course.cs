using System.Text.Json.Serialization;

namespace ScormGen.Core.Models;

public enum ScormFormat { Scorm2004, Scorm12 }

public record Course(
    string CourseId,
    string Title,
    string Version,
    List<ScormPackage> Packages)
{
    [JsonIgnore]
    public ScormFormat Format { get; init; } = ScormFormat.Scorm2004;
}

public record ScormPackage(
    int FileNumber,
    string FileName,
    string ContentType,
    string Title,
    List<IContentItem> Content,
    double PassingScore = 0.8);
