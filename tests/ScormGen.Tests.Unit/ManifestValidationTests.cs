using System.IO.Compression;
using System.Xml.Linq;
using ScormGen.Core.Loading;
using ScormGen.Core.Packaging;
using ScormGen.Core.Templates;

namespace ScormGen.Tests.Unit;

/// <summary>
/// Validates imsmanifest.xml against the structural requirements of SCORM 2004 3rd Edition.
/// Checks namespace declarations, required elements, required attributes, and sequencing rules
/// without requiring the full 5-file ADL XSD bundle.
/// </summary>
public class ManifestValidationTests
{
    private static readonly XNamespace Imscp  = "http://www.imsglobal.org/xsd/imscp_v1p1";
    private static readonly XNamespace Adlcp  = "http://www.adlnet.org/xsd/adlcp_v1p3";
    private static readonly XNamespace Adlseq = "http://www.adlnet.org/xsd/adlseq_v1p3";
    private static readonly XNamespace Adlnav = "http://www.adlnet.org/xsd/adlnav_v1p3";
    private static readonly XNamespace Imsss  = "http://www.imsglobal.org/xsd/imsss";
    private static readonly XNamespace Xsi    = "http://www.w3.org/2001/XMLSchema-instance";

    private static XDocument GetManifestXml(string contentType, double passingScore = 0.8)
    {
        var xml = HtmlTemplates.GetManifest("TEST_PKG", "Test Package", contentType, passingScore);
        return XDocument.Parse(xml);
    }

    private static XDocument GetManifestFromPackage(int packageIndex = 0)
    {
        var json = File.ReadAllText(
            Path.Combine("..", "..", "..", "..", "..", "templates", "sample_course.json"));
        var course = CourseLoader.Load(json);
        var bytes = ScormPackager.PackageCourse(course);

        using var outer = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        using var innerStream = new MemoryStream();
        outer.Entries[packageIndex].Open().CopyTo(innerStream);
        innerStream.Position = 0;

        using var inner = new ZipArchive(innerStream, ZipArchiveMode.Read);
        var entry = inner.Entries.First(e => e.Name == "imsmanifest.xml");
        using var reader = new StreamReader(entry.Open());
        return XDocument.Parse(reader.ReadToEnd());
    }

    [Fact]
    public void GetManifest_IsWellFormedXml()
    {
        var ex = Record.Exception(() => GetManifestXml("informational"));
        Assert.Null(ex);
    }

    [Fact]
    public void GetManifest_RootElement_IsManifestInImscpNamespace()
    {
        var doc = GetManifestXml("informational");
        Assert.Equal(Imscp + "manifest", doc.Root!.Name);
    }

    [Theory]
    [InlineData("informational")]
    [InlineData("graded")]
    public void GetManifest_RootElement_HasRequiredAttributes(string contentType)
    {
        var root = GetManifestXml(contentType).Root!;
        Assert.NotNull(root.Attribute("identifier"));
        Assert.NotNull(root.Attribute("version"));
        Assert.Equal("1.0", (string)root.Attribute("version")!);
    }

    [Fact]
    public void GetManifest_DeclaresAllRequiredNamespaces()
    {
        var doc = GetManifestXml("informational");
        var root = doc.Root!;

        // All five SCORM 2004 namespaces must be declared
        Assert.NotNull(root.Attribute(XNamespace.Xmlns + "adlcp"));
        Assert.NotNull(root.Attribute(XNamespace.Xmlns + "adlseq"));
        Assert.NotNull(root.Attribute(XNamespace.Xmlns + "adlnav"));
        Assert.NotNull(root.Attribute(XNamespace.Xmlns + "imsss"));
        Assert.NotNull(root.Attribute(XNamespace.Xmlns + "xsi"));

        Assert.Equal(Adlcp.NamespaceName,  (string)root.Attribute(XNamespace.Xmlns + "adlcp")!);
        Assert.Equal(Adlseq.NamespaceName, (string)root.Attribute(XNamespace.Xmlns + "adlseq")!);
        Assert.Equal(Adlnav.NamespaceName, (string)root.Attribute(XNamespace.Xmlns + "adlnav")!);
        Assert.Equal(Imsss.NamespaceName,  (string)root.Attribute(XNamespace.Xmlns + "imsss")!);
        Assert.Equal(Xsi.NamespaceName,    (string)root.Attribute(XNamespace.Xmlns + "xsi")!);
    }

    [Fact]
    public void GetManifest_Metadata_HasCorrectSchemaAndVersion()
    {
        var doc = GetManifestXml("informational");
        var metadata = doc.Root!.Element(Imscp + "metadata")!;
        Assert.NotNull(metadata);
        Assert.Equal("ADL SCORM", (string)metadata.Element(Imscp + "schema")!);
        Assert.Equal("2004 3rd Edition", (string)metadata.Element(Imscp + "schemaversion")!);
    }

    [Fact]
    public void GetManifest_Organizations_HasDefaultAttributeMatchingOrganizationId()
    {
        var doc = GetManifestXml("informational");
        var organizations = doc.Root!.Element(Imscp + "organizations")!;
        Assert.NotNull(organizations);

        var defaultRef = (string)organizations.Attribute("default")!;
        Assert.NotEmpty(defaultRef);

        var org = organizations.Element(Imscp + "organization")!;
        Assert.NotNull(org);
        Assert.Equal(defaultRef, (string)org.Attribute("identifier")!);
    }

    [Fact]
    public void GetManifest_Organization_HasItemWithIdentifierRef()
    {
        var doc = GetManifestXml("informational");
        var org = doc.Root!
            .Element(Imscp + "organizations")!
            .Element(Imscp + "organization")!;

        var item = org.Element(Imscp + "item")!;
        Assert.NotNull(item);
        Assert.NotEmpty((string)item.Attribute("identifier")!);
        Assert.NotEmpty((string)item.Attribute("identifierref")!);

        var title = item.Element(Imscp + "title");
        Assert.NotNull(title);
        Assert.NotEmpty((string)title!);
    }

    [Fact]
    public void GetManifest_Resources_HasWebcontentScoResource()
    {
        var doc = GetManifestXml("informational");
        var resources = doc.Root!.Element(Imscp + "resources")!;
        Assert.NotNull(resources);

        var resource = resources.Element(Imscp + "resource")!;
        Assert.NotNull(resource);
        Assert.Equal("webcontent", (string)resource.Attribute("type")!);
        Assert.Equal("sco", (string)resource.Attribute(Adlcp + "scormType")!);
        Assert.Equal("index.html", (string)resource.Attribute("href")!);
    }

    [Fact]
    public void GetManifest_Resources_ListsIndexHtmlAndScormApiJs()
    {
        var doc = GetManifestXml("informational");
        var resource = doc.Root!
            .Element(Imscp + "resources")!
            .Element(Imscp + "resource")!;

        var files = resource.Elements(Imscp + "file")
            .Select(f => (string)f.Attribute("href")!)
            .ToHashSet();

        Assert.Contains("index.html", files);
        Assert.Contains("scorm_api.js", files);
    }

    [Fact]
    public void GetManifest_ItemIdentifierRef_MatchesResourceIdentifier()
    {
        var doc = GetManifestXml("informational");
        var root = doc.Root!;

        var itemRef = (string)root
            .Element(Imscp + "organizations")!
            .Element(Imscp + "organization")!
            .Element(Imscp + "item")!
            .Attribute("identifierref")!;

        var resourceId = (string)root
            .Element(Imscp + "resources")!
            .Element(Imscp + "resource")!
            .Attribute("identifier")!;

        Assert.Equal(resourceId, itemRef);
    }

    [Fact]
    public void GetManifest_NonGraded_HasCompletionSetByContentSequencing()
    {
        var doc = GetManifestXml("informational");
        var item = doc.Root!
            .Element(Imscp + "organizations")!
            .Element(Imscp + "organization")!
            .Element(Imscp + "item")!;

        var deliveryControls = item
            .Element(Imsss + "sequencing")!
            .Element(Imsss + "deliveryControls")!;

        Assert.NotNull(deliveryControls);
        Assert.Equal("true", (string)deliveryControls.Attribute("completionSetByContent")!);
        Assert.Null(deliveryControls.Attribute("objectiveSetByContent"));
    }

    [Theory]
    [InlineData(0.8)]
    [InlineData(1.0)]
    [InlineData(0.5)]
    public void GetManifest_Graded_HasObjectiveSequencingWithPassingScore(double passingScore)
    {
        var doc = GetManifestXml("graded", passingScore);
        var item = doc.Root!
            .Element(Imscp + "organizations")!
            .Element(Imscp + "organization")!
            .Element(Imscp + "item")!;

        var sequencing = item.Element(Imsss + "sequencing")!;
        Assert.NotNull(sequencing);

        var deliveryControls = sequencing.Element(Imsss + "deliveryControls")!;
        Assert.Equal("true", (string)deliveryControls.Attribute("completionSetByContent")!);
        Assert.Equal("true", (string)deliveryControls.Attribute("objectiveSetByContent")!);

        var objective = sequencing
            .Element(Imsss + "objectives")!
            .Element(Imsss + "primaryObjective")!;
        Assert.NotNull(objective);
        Assert.Equal("true", (string)objective.Attribute("satisfiedByMeasure")!);

        var minMeasure = (string)objective.Element(Imsss + "minNormalizedMeasure")!;
        Assert.Equal(passingScore, double.Parse(minMeasure, System.Globalization.CultureInfo.InvariantCulture), precision: 10);
    }

    [Fact]
    public void GetManifest_XssInIdentifier_DoesNotBreakXmlStructure()
    {
        var xml = HtmlTemplates.GetManifest("<evil&id>", "Normal Title", "informational", 0.8);
        var ex = Record.Exception(() => XDocument.Parse(xml));
        Assert.Null(ex);
    }

    [Fact]
    public void PackageCourse_AllPackageManifests_AreStructurallyValid()
    {
        for (int i = 0; i < 4; i++)
        {
            var doc = GetManifestFromPackage(i);
            Assert.Equal(Imscp + "manifest", doc.Root!.Name);
            Assert.NotNull(doc.Root.Element(Imscp + "metadata"));
            Assert.NotNull(doc.Root.Element(Imscp + "organizations"));
            Assert.NotNull(doc.Root.Element(Imscp + "resources"));
        }
    }
}
