using System.Reflection;
using Stride.Core.Reflection;

namespace Stride.BepuPhysics._2D
{
    internal class Module
    {
        [Stride.Core.ModuleInitializer]
        public static void Initialize()
        {
            AssemblyRegistry.Register(typeof(Module).GetTypeInfo().Assembly, AssemblyCommonCategories.Assets);
        }
    }
}
