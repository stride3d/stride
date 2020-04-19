// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable 436 // Stride.PublicKeys is defined in multiple assemblies

// Make internals Stride visible to Stride assemblies
[assembly: InternalsVisibleTo("Stride.Graphics.Serializers" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Graphics.ShaderCompiler" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Engine" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Rendering" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Games" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.UI" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Graphics.Tests" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("StrideGraphicsTests" + Stride.PublicKeys.Default)] // iOS removes dot
[assembly: InternalsVisibleTo("Stride.Engine.Tests" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("StrideEngineTests" + Stride.PublicKeys.Default)] // iOS removes dot
[assembly: InternalsVisibleTo("Stride.Graphics.Regression" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Assets" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.VirtualReality" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Video" + Stride.PublicKeys.Default)]

#if !STRIDE_SIGNED
[assembly: InternalsVisibleTo("Stride.Assets.Presentation")]
#endif
