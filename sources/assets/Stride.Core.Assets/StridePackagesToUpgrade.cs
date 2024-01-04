using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Core.Assets;
public static class StridePackagesToUpgrade
{
    public static string[] PackageNames =
    [
        #region default packages in a Stride project
        "Stride.Engine",
        "Stride.Navigation",
        "Stride.Particles",
        "Stride.Physics",
        "Stride.UI",
        "Stride.Video",
        "Stride.Core.Assets.CompilerApp",
        #endregion

        "Stride.Core",
        "Stride.Graphics",
        "Stride.Input",
        "Stride.Rendering",
    ];
}
