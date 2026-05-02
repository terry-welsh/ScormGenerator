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
}
