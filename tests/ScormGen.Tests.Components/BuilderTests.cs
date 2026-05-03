using AngleSharp.Dom;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using ScormGen.Web.Components.Pages;

namespace ScormGen.Tests.Components;

public class BuilderTests : BunitContext
{
    public BuilderTests()
    {
        Services.AddMemoryCache();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void InitialRender_H1_IsCourseBuilder()
    {
        var cut = Render<Builder>();
        Assert.Contains("Course Builder", cut.Find("h1").TextContent);
    }

    [Fact]
    public void InitialRender_GenerateButton_IsDisabled()
    {
        var cut = Render<Builder>();
        var generate = cut.FindAll("button")
            .First(b => b.TextContent.Contains("Generate SCORM Package"));
        Assert.True(generate.HasAttribute("disabled"));
    }

    [Fact]
    public void InitialRender_ExportJsonButton_IsDisabled()
    {
        var cut = Render<Builder>();
        var export = cut.FindAll("button")
            .First(b => b.TextContent.Contains("Export JSON"));
        Assert.True(export.HasAttribute("disabled"));
    }

    [Fact]
    public void InitialRender_AddPackageButton_IsPresent()
    {
        var cut = Render<Builder>();
        var addBtn = cut.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Add Package"));
        Assert.NotNull(addBtn);
    }

    [Fact]
    public void InitialRender_NoPackages_PackageListIsEmpty()
    {
        var cut = Render<Builder>();
        Assert.DoesNotContain("(untitled package)", cut.Markup);
    }

    [Fact]
    public void InitialRender_ErrorMessage_IsNotRendered()
    {
        var cut = Render<Builder>();
        Assert.DoesNotContain("bg-danger", cut.Markup);
    }

    [Fact]
    public void InitialRender_DownloadBanner_IsNotRendered()
    {
        var cut = Render<Builder>();
        Assert.DoesNotContain("Package ready", cut.Markup);
    }

    [Fact]
    public void AddPackage_Click_AddsPackageToList()
    {
        var cut = Render<Builder>();

        cut.FindAll("button").First(b => b.TextContent.Contains("Add Package")).Click();

        Assert.Contains("(untitled package)", cut.Markup);
    }

    [Fact]
    public void AddPackage_Click_EnablesExportJsonButton()
    {
        var cut = Render<Builder>();

        cut.FindAll("button").First(b => b.TextContent.Contains("Add Package")).Click();

        var export = cut.FindAll("button")
            .First(b => b.TextContent.Contains("Export JSON"));
        Assert.False(export.HasAttribute("disabled"));
    }

    [Fact]
    public void AddPackage_Click_ShowsContentTypeDropdown()
    {
        var cut = Render<Builder>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Add Package")).Click();

        var selects = cut.FindAll("select");
        Assert.NotEmpty(selects);
    }

    [Fact]
    public void AddTwoPackages_ThenRemoveFirst_LeavesOnePackage()
    {
        var cut = Render<Builder>();

        var addBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Add Package"));
        addBtn.Click();
        addBtn.Click();

        var removeButtons = cut.FindAll("button")
            .Where(b => b.TextContent.Contains("Remove"))
            .ToList();
        Assert.Equal(2, removeButtons.Count);

        removeButtons[0].Click();

        Assert.Single(cut.FindAll("button"), b => b.TextContent.Contains("Remove"));
    }

    [Fact]
    public void AddPackage_DefaultContentType_IsInformational()
    {
        var cut = Render<Builder>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Add Package")).Click();

        Assert.Contains("informational", cut.Markup);
    }

    [Fact]
    public void AddPackage_InformationalType_ShowsContentAddButtons()
    {
        var cut = Render<Builder>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Add Package")).Click();

        var buttonTexts = cut.FindAll("button").Select(b => b.TextContent).ToList();
        Assert.Contains(buttonTexts, t => t.Contains("Heading"));
        Assert.Contains(buttonTexts, t => t.Contains("Paragraph"));
        Assert.Contains(buttonTexts, t => t.Contains("Bullet List"));
    }

    [Fact]
    public void AddHeadingContent_Click_AddsHeadingToPackage()
    {
        var cut = Render<Builder>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Add Package")).Click();

        cut.FindAll("button").First(b => b.TextContent.Contains("Heading")).Click();

        var typeLabels = cut.FindAll("span")
            .Select(s => s.TextContent.Trim())
            .Where(t => t.Equals("Heading", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(typeLabels);
    }
}
