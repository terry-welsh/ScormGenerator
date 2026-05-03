using System.IO.Compression;
using ScormGen.Core.Loading;
using ScormGen.Core.Packaging;

namespace ScormGen.Tests.Unit;

public class PackagingTests
{

    private static string SampleJson =>
        File.ReadAllText(Path.Combine("..", "..", "..", "..", "..", "templates", "sample_course.json"));

    [Fact]
    public void PackageCourse_ProducesOuterZipWith4Entries()
    {
        var course = CourseLoader.Load(SampleJson);
        var bytes = ScormPackager.PackageCourse(course);

        using var outer = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        Assert.Equal(4, outer.Entries.Count);
        Assert.All(outer.Entries, e => Assert.EndsWith(".zip", e.Name));
    }

    [Fact]
    public void PackageCourse_EachInnerZipHasRequiredFiles()
    {
        var course = CourseLoader.Load(SampleJson);
        var bytes = ScormPackager.PackageCourse(course);

        using var outer = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        foreach (var outerEntry in outer.Entries)
        {
            using var innerStream = new MemoryStream();
            outerEntry.Open().CopyTo(innerStream);
            innerStream.Position = 0;

            using var inner = new ZipArchive(innerStream, ZipArchiveMode.Read);
            var names = inner.Entries.Select(e => e.Name).ToHashSet();
            Assert.Contains("index.html", names);
            Assert.Contains("imsmanifest.xml", names);
            Assert.Contains("scorm_api.js", names);
        }
    }

    [Fact]
    public void PackageCourse_ManifestContainsScorm2004SchemaVersion()
    {
        var course = CourseLoader.Load(SampleJson);
        var bytes = ScormPackager.PackageCourse(course);

        using var outer = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        var firstEntry = outer.Entries[0];
        using var innerStream = new MemoryStream();
        firstEntry.Open().CopyTo(innerStream);
        innerStream.Position = 0;

        using var inner = new ZipArchive(innerStream, ZipArchiveMode.Read);
        var manifest = inner.Entries.First(e => e.Name == "imsmanifest.xml");
        using var reader = new StreamReader(manifest.Open());
        var xml = reader.ReadToEnd();

        Assert.Contains("2004 3rd Edition", xml);
        Assert.Contains("imsss", xml);
    }
}
