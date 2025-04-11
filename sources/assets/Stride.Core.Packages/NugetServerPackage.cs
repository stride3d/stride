using NuGet.Protocol.Core.Types;

namespace Stride.Core.Packages;

public class NugetServerPackage : NugetPackage
{
    public NugetServerPackage(IPackageSearchMetadata package, string source) : base(package)
    {
        Source = source;
    }

    public string Source { get; }
}
