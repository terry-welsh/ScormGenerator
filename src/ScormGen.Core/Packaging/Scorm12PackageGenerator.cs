using ScormGen.Core.Templates;

namespace ScormGen.Core.Packaging;

public sealed class Scorm12PackageGenerator : ScormPackageGeneratorBase
{
    protected override string ApiJs => TemplateResources.Scorm12ApiJs;

    protected override string BuildManifest(string identifier, string title, string contentType, double passingScore)
    {
        var sid = System.Security.SecurityElement.Escape(identifier) ?? identifier;
        var st = System.Security.SecurityElement.Escape(title) ?? title;

        var masteryScore = string.Equals(contentType, "graded", StringComparison.OrdinalIgnoreCase)
            ? $"\n                <adlcp:masteryscore>{(int)(passingScore * 100)}</adlcp:masteryscore>"
            : string.Empty;

        return $$"""
<?xml version="1.0" encoding="UTF-8"?>
<manifest identifier="{{sid}}" version="1.0"
    xmlns="http://www.imsproject.org/xsd/imscp_rootv1p1p2"
    xmlns:adlcp="http://www.adlnet.org/xsd/adlcp_rootv1p2"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.imsproject.org/xsd/imscp_rootv1p1p2 imscp_rootv1p1p2.xsd
                        http://www.adlnet.org/xsd/adlcp_rootv1p2 adlcp_rootv1p2.xsd">

    <metadata>
        <schema>ADL SCORM</schema>
        <schemaversion>1.2</schemaversion>
    </metadata>

    <organizations default="ORG-{{sid}}">
        <organization identifier="ORG-{{sid}}">
            <title>{{st}}</title>
            <item identifier="ITEM-{{sid}}" identifierref="RES-{{sid}}">
                <title>{{st}}</title>{{masteryScore}}
            </item>
        </organization>
    </organizations>

    <resources>
        <resource identifier="RES-{{sid}}" type="webcontent" adlcp:scormtype="sco" href="index.html">
            <file href="index.html"/>
            <file href="scorm_api.js"/>
        </resource>
    </resources>
</manifest>
""";
    }
}
