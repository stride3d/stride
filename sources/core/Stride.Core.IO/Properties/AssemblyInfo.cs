// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable 436 // Stride.PublicKeys is defined in multiple assemblies

[assembly: InternalsVisibleTo("Stride.Core.IO.Serializers" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Engine" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Engine.Step1" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Core.Tests" + Stride.PublicKeys.Default)]

#if STRIDE_PLATFORM_IOS
[assembly: InternalsVisibleTo("StrideCoreTests" + Stride.PublicKeys.Default)]
#endif
