// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable 436 // Stride.PublicKeys is defined in multiple assemblies

// Alphabetic sort
[assembly: InternalsVisibleTo("Stride" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Assets.Models" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Audio" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Core.Assets" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Core.Serialization" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Core.Serializers" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Core.IO" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Engine" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Games" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Graphics" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Particles" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Rendering" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Shaders" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.TextureConverter" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.UI" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Video" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.VirtualReality" + Stride.PublicKeys.Default)]

// Test projects
[assembly: InternalsVisibleTo("Stride.Core.Tests" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("StrideCoreTests" + Stride.PublicKeys.Default)]
