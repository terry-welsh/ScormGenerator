using System.IO.Compression;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ScormGen.Tests.Integration;

public class EndToEndTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EndToEndTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostGenerate_WithSampleCourse_Returns200WithZip()
    {
        var samplePath = Path.Combine("..", "..", "..", "..", "..", "templates", "sample_course.json");
        var json = await File.ReadAllBytesAsync(samplePath);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(json);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Add(fileContent, "course", "sample_course.json");

        var response = await _client.PostAsync("/generate", content);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);

        var zipBytes = await response.Content.ReadAsByteArrayAsync();
        using var zip = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read);
        Assert.Equal(4, zip.Entries.Count);

        Assert.True(response.Headers.TryGetValues("X-Package-Count", out var values));
        Assert.Equal("4", values.First());
    }

    [Fact]
    public async Task PostGenerate_NonMultipart_Returns400()
    {
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/generate", content);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostGenerate_MissingCourseField_Returns400()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("hello"), "other_field");
        var response = await _client.PostAsync("/generate", content);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostGenerate_MalformedJson_Returns400()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("{ not valid json }"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Add(fileContent, "course", "bad.json");
        var response = await _client.PostAsync("/generate", content);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostGenerate_InvalidContentType_Returns400()
    {
        var badJson = """{"course_id":"X","title":"T","version":"1.0","packages":[{"file_number":1,"file_name":"F","content_type":"bogus","title":"T","content":[]}]}""";
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(badJson));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Add(fileContent, "course", "bad.json");
        var response = await _client.PostAsync("/generate", content);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDownload_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/download/nonexistent-id-that-does-not-exist");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostGenerate_XssInTitle_HtmlIsEscapedInOutput()
    {
        var json = """
            {
              "course_id": "XSS_TEST",
              "title": "Test Course",
              "version": "1.0",
              "packages": [{
                "file_number": 1,
                "file_name": "PKG1",
                "content_type": "informational",
                "title": "<script>alert('xss')</script>",
                "content": []
              }]
            }
            """;

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(json));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Add(fileContent, "course", "xss.json");

        var response = await _client.PostAsync("/generate", content);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var zipBytes = await response.Content.ReadAsByteArrayAsync();
        using var outer = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read);
        using var innerStream = new MemoryStream();
        outer.Entries[0].Open().CopyTo(innerStream);
        innerStream.Position = 0;
        using var inner = new ZipArchive(innerStream, ZipArchiveMode.Read);
        var indexEntry = inner.Entries.First(e => e.Name == "index.html");
        using var reader = new StreamReader(indexEntry.Open());
        var html = await reader.ReadToEndAsync();

        Assert.DoesNotContain("<script>alert('xss')</script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }
}
