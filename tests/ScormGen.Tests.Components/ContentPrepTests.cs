using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using ScormGen.Web.Components.Pages;

namespace ScormGen.Tests.Components;

public class ContentPrepTests : BunitContext
{
    public ContentPrepTests()
    {
        Services.AddMemoryCache();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void InitialRender_H1_IsContentPrepUtility()
    {
        var cut = Render<ContentPrep>();

        Assert.Contains("Content Prep Utility", cut.Find("h1").TextContent);
    }

    [Fact]
    public void InitialRender_ShowsHowToUseSteps()
    {
        var cut = Render<ContentPrep>();

        Assert.Contains("Copy the generated prompt", cut.Markup);
        Assert.Contains("Markdown", cut.Markup);
        Assert.Contains("Clean &amp; Validate JSON", cut.Markup);
        Assert.Contains("Download the validated JSON", cut.Markup);
    }

    [Fact]
    public void InitialRender_PromptIncludesSchemaInstructions()
    {
        var cut = Render<ContentPrep>();

        var prompt = cut.Find("textarea[readonly]").GetAttribute("value") ?? cut.Find("textarea[readonly]").TextContent;
        Assert.Contains("The source content is Markdown", prompt);
        Assert.Contains("## Package: FILE_NAME | informational | Package Title", prompt);
        Assert.Contains("Create one SCORM package for each topic under each module", prompt);
        Assert.Contains("Do not create one package per module", prompt);
        Assert.Contains("6 modules and each module contains 4 topics", prompt);
        Assert.Contains("**Scenario:**", prompt);
        Assert.Contains("**Question:**", prompt);
        Assert.Contains("Output only JSON", prompt);
        Assert.Contains("SCORM 2004 3rd Edition", prompt);
        Assert.Contains("\"packages\"", prompt);
        Assert.Contains("\"multiple_choice\"", prompt);
        Assert.Contains("\"scenario\"", prompt);
    }

    [Fact]
    public void InitialRender_DownloadAndGenerateAreDisabled()
    {
        var cut = Render<ContentPrep>();

        var buttons = cut.FindAll("button");
        var download = buttons.First(b => b.TextContent.Contains("Download JSON"));
        var generate = buttons.First(b => b.TextContent.Contains("Generate SCORM Package"));

        Assert.True(download.HasAttribute("disabled"));
        Assert.True(generate.HasAttribute("disabled"));
    }

    [Fact]
    public void InitialRender_JsonFileInput_IsPresent()
    {
        var cut = Render<ContentPrep>();

        var input = cut.Find("input[type=file]");

        Assert.Equal(".json", input.GetAttribute("accept"));
        Assert.Contains("Load Generated JSON File", cut.Markup);
    }

    [Fact]
    public void ValidateJson_EmptyInput_ShowsActionableError()
    {
        var cut = Render<ContentPrep>();

        cut.FindAll("button").First(b => b.TextContent.Contains("Clean & Validate JSON")).Click();

        Assert.Contains("Paste JSON into the Course JSON field or load a generated .json file", cut.Markup);
    }

    [Fact]
    public void ValidateJson_InvalidJson_ShowsError()
    {
        var cut = Render<ContentPrep>();

        cut.Find("textarea[placeholder=\"Paste the model's JSON response here.\"]").Change("{");
        cut.FindAll("button").First(b => b.TextContent.Contains("Clean & Validate JSON")).Click();

        Assert.Contains("Malformed JSON", cut.Markup);
    }

    [Fact]
    public void ValidateJson_FencedValidJson_ShowsSuccessAndEnablesActions()
    {
        var cut = Render<ContentPrep>();
        var json = """
            ```json
            {
              "course_id": "TEST",
              "title": "Test Course",
              "version": "1.0",
              "packages": [
                {
                  "file_number": 1,
                  "file_name": "TEST_INTRO",
                  "content_type": "informational",
                  "title": "Intro",
                  "content": []
                }
              ]
            }
            ```
            """;

        cut.Find("textarea[placeholder=\"Paste the model's JSON response here.\"]").Change(json);
        cut.FindAll("button").First(b => b.TextContent.Contains("Clean & Validate JSON")).Click();

        var buttons = cut.FindAll("button");
        var download = buttons.First(b => b.TextContent.Contains("Download JSON"));
        var generate = buttons.First(b => b.TextContent.Contains("Generate SCORM Package"));

        Assert.Contains("Valid course JSON", cut.Markup);
        Assert.False(download.HasAttribute("disabled"));
        Assert.False(generate.HasAttribute("disabled"));
    }

    [Fact]
    public void LoadExampleJson_Click_LoadsExamplePromptForValidation()
    {
        var cut = Render<ContentPrep>();

        cut.FindAll("button").First(b => b.TextContent.Contains("Load Example JSON")).Click();

        Assert.Contains("Example JSON loaded", cut.Markup);
        Assert.Contains("SAFETY_101", cut.Markup);
    }

    [Fact]
    public void LoadExampleMarkdown_Click_LoadsMarkdownSource()
    {
        var cut = Render<ContentPrep>();

        cut.FindAll("button").First(b => b.TextContent.Contains("Load Example Markdown")).Click();

        Assert.Contains("## Module 1: Safety Basics", cut.Markup);
        Assert.Contains("### Topic 1.1: Why Safety Basics Matter", cut.Markup);
        Assert.Contains("**Question:**", cut.Markup);
    }
}
