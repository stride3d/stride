// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable 436 // Xenko.PublicKeys is defined in multiple assemblies

// Make internals Xenko visible to Xenko assemblies
[assembly: InternalsVisibleTo("Xenko.Graphics.Serializers" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Graphics.ShaderCompiler" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Engine" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Rendering" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Games" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.UI" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Graphics.Tests" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("XenkoGraphicsTests" + Xenko.PublicKeys.Default)] // iOS removes dot
[assembly: InternalsVisibleTo("Xenko.Engine.Tests" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("XenkoEngineTests" + Xenko.PublicKeys.Default)] // iOS removes dot
[assembly: InternalsVisibleTo("Xenko.Graphics.Regression" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Assets" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.VirtualReality" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Video" + Xenko.PublicKeys.Default)]

#if !XENKO_SIGNED
[assembly: InternalsVisibleTo("Xenko.Assets.Presentation")]
#endif
