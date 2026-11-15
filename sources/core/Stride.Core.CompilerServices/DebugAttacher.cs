// This file together with the property in csproj looks at LAUNCH_DEBUGGER constant
// If it is present it will launch the debugger when the assembly is loaded into MSBuild
// Note that MSBuild can sometimes load multiple instances of the analyzer.

namespace Stride.Core.CompilerServices
{
    internal static class DebugAttacher
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void Attach()
        {
#if LAUNCH_DEBUGGER
            System.Diagnostics.Debugger.Launch();
#endif
        }
    }
}
