namespace ScormGen.Core.Models;

public record Course(
    string CourseId,
    string Title,
    string Version,
    List<ScormPackage> Packages);

public record ScormPackage(
    int FileNumber,
    string FileName,
    string ContentType,
    string Title,
    List<IContentItem> Content,
    double PassingScore = 0.8);
