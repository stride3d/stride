using System.Reflection;
using Stride.Core.Reflection;

namespace Stride.BepuPhysics.DebugRender
{
    internal class Module
    {
        [Core.ModuleInitializer]
        public static void Initialize()
        {
            AssemblyRegistry.Register(typeof(Module).GetTypeInfo().Assembly, AssemblyCommonCategories.Assets);
        }
    }
}
