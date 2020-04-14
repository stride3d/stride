using NuGet.Protocol.Core.Types;
using Stride.Core.Annotations;

namespace Stride.Core.Packages
{
    public class NugetServerPackage : NugetPackage
    {
        public NugetServerPackage([NotNull] IPackageSearchMetadata package, [NotNull] string source) : base(package)
        {
            Source = source;
        }

        [NotNull]
        public string Source { get; }
    }
}
