// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Core.Assets;

namespace Stride.Samples.Tests
{
    internal static class TestServerResolver
    {
        [ModuleInitializer]
        public static void ResolveReferences() =>
            NuGetAssemblyResolver.SetupNuGet("Stride.SamplesTestServer", StrideVersion.NuGetVersion, Assembly.GetCallingAssembly());
    }
}
