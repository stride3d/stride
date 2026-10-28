// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Runtime.CompilerServices;

#pragma warning disable 436 // Stride.PublicKeys is defined in multiple assemblies

[assembly: InternalsVisibleTo("Stride.Editor.Wpf" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.GameStudio" + Stride.PublicKeys.Default)]
// The agnostic editor layer and its tests
[assembly: InternalsVisibleTo("Stride.Editor" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Editor.Avalonia" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Editor.Services.Tests" + Stride.PublicKeys.Default)]
