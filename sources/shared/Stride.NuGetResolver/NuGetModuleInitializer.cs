using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Stride.Core.Assets
{
    class NuGetModuleInitializer
    {
        [ModuleInitializer(-100000)]
        internal static void __Initialize__()
        {
            // Only perform this for entry assembly
            if (!(Assembly.GetEntryAssembly() == null // .NET FW: null during module .ctor
                || Assembly.GetEntryAssembly() == Assembly.GetCallingAssembly())) // .NET Core: check against calling assembly (note: if using Stride.NuGetLoader, it will be skipped as well which is what we want)
                return;

            NuGetAssemblyResolver.SetupNuGet(Assembly.GetExecutingAssembly().GetName().Name, StrideVersion.NuGetVersion);
        }
    }
}
