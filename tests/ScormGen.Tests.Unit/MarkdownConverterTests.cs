using ScormGen.Core.Conversion;

namespace ScormGen.Tests.Unit;

// Markdown converter tests will be implemented once MarkdownConverter is fully
// ported from the Python md_converter.py source.
public class MarkdownConverterTests
{
    [Fact(Skip = "Awaiting Python source port")]
    public void Convert_ValidScormMd_ReturnsCourse() { }
}
