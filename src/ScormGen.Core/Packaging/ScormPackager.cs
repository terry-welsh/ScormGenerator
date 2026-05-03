using ScormGen.Core.Models;

namespace ScormGen.Core.Packaging;

public static class ScormPackager
{
    public static byte[] PackageCourse(Course course)
    {
        IPackageGenerator generator = course.Format == ScormFormat.Scorm12
            ? new Scorm12PackageGenerator()
            : new Scorm2004PackageGenerator();
        return generator.PackageCourse(course);
    }
}
