// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

#pragma warning disable 436 // Stride.PublicKeys is defined in multiple assemblies

[assembly: InternalsVisibleTo("Stride.Assets.Editor.Avalonia" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Assets.Editor.Tests" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Assets.Editor.Avalonia.Tests" + Stride.PublicKeys.Default)]
