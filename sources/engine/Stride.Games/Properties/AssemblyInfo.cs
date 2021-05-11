// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable 436 // Stride.PublicKeys is defined in multiple assemblies

[assembly: InternalsVisibleTo("Stride.Games.Serializers" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Input" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Engine" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.UI" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Debugger" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Graphics.Regression" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.VirtualReality" + Stride.PublicKeys.Default)]
