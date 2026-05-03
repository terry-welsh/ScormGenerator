using System.Reflection;

namespace ScormGen.Core.Templates;

internal static class TemplateResources
{
    internal static readonly string ScormApiJs = Load("scorm-api.js");
    internal static readonly string BaseStyles = "<style>\n" + Load("base-styles.css") + "\n    </style>";

    private static string Load(string name)
    {
        var asm = typeof(TemplateResources).Assembly;
        using var stream = asm.GetManifestResourceStream($"ScormGen.Core.Templates.Resources.{name}")
            ?? throw new InvalidOperationException($"Embedded resource '{name}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
