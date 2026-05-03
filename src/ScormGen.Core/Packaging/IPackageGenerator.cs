using ScormGen.Core.Models;

namespace ScormGen.Core.Packaging;

public interface IPackageGenerator
{
    byte[] PackageCourse(Course course);
}
