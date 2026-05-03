using ScormGen.Core.Templates;

namespace ScormGen.Core.Packaging;

public sealed class Scorm2004PackageGenerator : ScormPackageGeneratorBase
{
    protected override string ApiJs => TemplateResources.ScormApiJs;

    protected override string BuildManifest(string identifier, string title, string contentType, double passingScore)
        => HtmlTemplates.GetManifest(identifier, title, contentType, passingScore);
}
