using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using ScormGen.Web.Components.Pages;

namespace ScormGen.Tests.Components;

public class HomeTests : BunitContext
{
    public HomeTests()
    {
        Services.AddMemoryCache();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void InitialRender_H1_IsGenerate()
    {
        var cut = Render<Home>();
        Assert.Contains("Generate SCORM Package", cut.Find("h1").TextContent);
    }

    [Fact]
    public void InitialRender_GenerateButton_IsDisabled()
    {
        var cut = Render<Home>();
        var button = cut.Find("button");
        Assert.True(button.HasAttribute("disabled"));
    }

    [Fact]
    public void InitialRender_GenerateButton_HasCorrectLabel()
    {
        var cut = Render<Home>();
        var button = cut.Find("button");
        Assert.Equal("Generate SCORM Package", button.TextContent.Trim());
    }

    [Fact]
    public void InitialRender_ErrorMessage_IsNotRendered()
    {
        var cut = Render<Home>();
        Assert.Empty(cut.FindAll("p.text-danger"));
    }

    [Fact]
    public void InitialRender_ProgressBar_IsNotRendered()
    {
        var cut = Render<Home>();
        Assert.Empty(cut.FindAll(".animate-pulse"));
    }

    [Fact]
    public void InitialRender_DownloadBanner_IsNotRendered()
    {
        var cut = Render<Home>();
        Assert.DoesNotContain("Package ready", cut.Markup);
    }

    [Fact]
    public void InitialRender_FileInput_IsPresent()
    {
        var cut = Render<Home>();
        var input = cut.Find("input[type=file]");
        Assert.Equal(".json", input.GetAttribute("accept"));
    }
}
