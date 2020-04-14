// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable 436 // Xenko.PublicKeys is defined in multiple assemblies

// Make internals Xenko.Framework.visible to all Xenko.Framework.assemblies
[assembly: InternalsVisibleTo("Xenko.Core.Serializers" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Core.IO" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Core.Assets" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.UI" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Engine" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Rendering" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Graphics" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Games" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Audio" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.VirtualReality" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Video" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Core.Tests" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("XenkoCoreTests" + Xenko.PublicKeys.Default)]
