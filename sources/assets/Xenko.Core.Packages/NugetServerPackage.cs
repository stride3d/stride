using NuGet.Protocol.Core.Types;
using Xenko.Core.Annotations;

namespace Xenko.Core.Packages
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
