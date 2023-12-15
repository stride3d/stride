using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Reflection;

namespace Stride.BepuPhysics
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
