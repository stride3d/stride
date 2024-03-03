using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Core.Reflection;

[assembly: InternalsVisibleTo("Stride.BepuPhysics._2D")]
[assembly: InternalsVisibleTo("Stride.BepuPhysics.DebugRender")]
[assembly: InternalsVisibleTo("Stride.BepuPhysics.Navigation")]
[assembly: InternalsVisibleTo("Stride.BepuPhysics.Soft")]

namespace Stride.BepuPhysics;

internal class Module
{
    [Stride.Core.ModuleInitializer]
    public static void Initialize()
    {
        AssemblyRegistry.Register(typeof(Module).GetTypeInfo().Assembly, AssemblyCommonCategories.Assets);
    }
}