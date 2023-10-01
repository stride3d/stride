// This file together with the property in csproj looks at LAUNCH_DEBUGGER constant
// If it is present it will launch the debugger when the assembly is loaded into MSBuild
// Note that MSBuild can sometimes load multiple instances of the analyzer.
//
// The ModuleInitializerAttribute had to be defined as it's not present normally for netstandard2.0
// but the C# compiler understands it anyways.

namespace System.Runtime.CompilerServices
{
    using System;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ModuleInitializerAttribute : Attribute { }
}

namespace Stride.Core.CompilerServices
{
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    public class DebugAttacher
    {
        [ModuleInitializer]
        public static void Attach()
        {
#if LAUNCH_DEBUGGER
            Debugger.Launch();
#endif
        }
    }
}
