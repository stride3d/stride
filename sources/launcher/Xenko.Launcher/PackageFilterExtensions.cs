using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core;
using Xenko.Core.Packages;

namespace Xenko.LauncherApp
{
    static class PackageFilterExtensions
    {
        public static IEnumerable<T> FilterXenkoMainPackages<T>(this IEnumerable<T> packages) where T : NugetPackage
        {
            // Xenko up to 3.0 package is Xenko, after it's Xenko.GameStudio
            return packages.Where(x => x.Id != "Xenko" || x.Version < new PackageVersion(3, 1, 0, 0));
        }
    }
}
