// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace xunit.runner.stride;

// Xunit.SkippableFact looks up {Un,}SupportedOSPlatformAttribute by string name at runtime and
// every [SkippableFact] throws when the type is missing; the iOS Mono AOT trimmer drops the
// Unsupported variant from the BCL because nothing roots it statically. Root both here.
internal static class TrimmerRoots
{
    [ModuleInitializer]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SupportedOSPlatformAttribute))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnsupportedOSPlatformAttribute))]
    internal static void KeepPlatformAttributes() { }
}
