// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable 436 // Stride.PublicKeys is defined in multiple assemblies

// Make internals Stride visible to Stride assemblies
[assembly: InternalsVisibleTo("Stride.Serializers" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Graphics.ShaderCompiler" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Engine" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Rendering " + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Graphics" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Audio" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Games" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Graphics.Regression" + Stride.PublicKeys.Default)]

#if !STRIDE_SIGNED
[assembly: InternalsVisibleTo("Stride.ImageComparerService")]
#endif
