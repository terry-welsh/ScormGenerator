using System.Text.Json;
using ScormGen.Core.Models;

namespace ScormGen.Core.Loading;

public class CourseLoader
{
    private static readonly HashSet<string> ValidContentTypes =
        new(StringComparer.OrdinalIgnoreCase) { "informational", "ungraded", "graded" };

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = false,
        MaxDepth = 32,
    };

    public static Course Load(string json)
    {
        Course course;
        try
        {
            course = JsonSerializer.Deserialize<Course>(json, Options)
                ?? throw new ArgumentException("Course JSON must be a top-level object, not null.");
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Malformed course JSON: {ex.Message}", ex);
        }

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("format", out var fmtEl))
        {
            var format = fmtEl.GetString()?.ToLowerInvariant() == "scorm_12"
                ? Models.ScormFormat.Scorm12
                : Models.ScormFormat.Scorm2004;
            course = course with { Format = format };
        }

        Validate(course);
        return course;
    }

    public static Course LoadFromFile(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Course file not found: {fullPath}", fullPath);
        return Load(File.ReadAllText(fullPath, System.Text.Encoding.UTF8));
    }

    private static void Validate(Course course)
    {
        if (string.IsNullOrWhiteSpace(course.Title))
            throw new ArgumentException("Course 'title' is required and must not be empty.");

        if (course.Packages is null || course.Packages.Count == 0)
            throw new ArgumentException("Course must contain at least one package.");

        for (int i = 0; i < course.Packages.Count; i++)
        {
            var pkg = course.Packages[i];
            int pkgNum = i + 1;

            if (string.IsNullOrWhiteSpace(pkg.FileName))
                throw new ArgumentException(
                    $"Package {pkgNum}: 'file_name' is required and must not be empty.");

            if (string.IsNullOrWhiteSpace(pkg.Title))
                throw new ArgumentException(
                    $"Package {pkgNum}: 'title' is required and must not be empty.");

            if (!ValidContentTypes.Contains(pkg.ContentType ?? ""))
                throw new ArgumentException(
                    $"Package {pkgNum}: invalid content_type '{pkg.ContentType}'. " +
                    "Valid values: informational, ungraded, graded.");
        }
    }
}
