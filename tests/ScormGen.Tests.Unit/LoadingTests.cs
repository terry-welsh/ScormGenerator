using ScormGen.Core.Loading;
using ScormGen.Core.Models;

namespace ScormGen.Tests.Unit;

public class LoadingTests
{

    [Fact]
    public void Load_ValidJson_ReturnsCourse()
    {
        var json = """
            {
              "course_id": "TEST",
              "title": "Test Course",
              "version": "1.0",
              "packages": [
                {
                  "file_number": 1,
                  "file_name": "TEST_PKG1",
                  "content_type": "informational",
                  "title": "Package 1",
                  "content": []
                }
              ]
            }
            """;

        var course = CourseLoader.Load(json);

        Assert.Equal("Test Course", course.Title);
        Assert.Equal("TEST", course.CourseId);
        Assert.Single(course.Packages);
        Assert.Equal("TEST_PKG1", course.Packages[0].FileName);
    }

    [Fact]
    public void Load_SampleCourseJson_Returns4Packages()
    {
        var json = File.ReadAllText(
            Path.Combine("..", "..", "..", "..", "..", "templates", "sample_course.json"));
        var course = CourseLoader.Load(json);

        Assert.Equal(4, course.Packages.Count);
        Assert.Equal("informational", course.Packages[0].ContentType);
        Assert.Equal("graded", course.Packages[3].ContentType);
        Assert.Equal(0.8, course.Packages[3].PassingScore);
    }

    [Fact]
    public void Load_MissingTitle_Throws()
    {
        var json = """{"course_id":"X","title":"","version":"1.0","packages":[]}""";
        Assert.Throws<ArgumentException>(() => CourseLoader.Load(json));
    }

    [Fact]
    public void Load_EmptyPackages_Throws()
    {
        var json = """{"course_id":"X","title":"T","version":"1.0","packages":[]}""";
        Assert.Throws<ArgumentException>(() => CourseLoader.Load(json));
    }

    [Fact]
    public void Load_InvalidContentType_Throws()
    {
        var json = """
            {
              "course_id": "X", "title": "T", "version": "1.0",
              "packages": [{ "file_number": 1, "file_name": "F", "content_type": "bogus",
                             "title": "T", "content": [] }]
            }
            """;
        Assert.Throws<ArgumentException>(() => CourseLoader.Load(json));
    }

    [Fact]
    public void Load_MissingFileName_Throws()
    {
        var json = """
            {
              "course_id": "X", "title": "T", "version": "1.0",
              "packages": [{ "file_number": 1, "file_name": "", "content_type": "informational",
                             "title": "T", "content": [] }]
            }
            """;
        Assert.Throws<ArgumentException>(() => CourseLoader.Load(json));
    }
}
