// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Runtime.CompilerServices;

#pragma warning disable 436 // Stride.PublicKeys is defined in multiple assemblies

[assembly: InternalsVisibleTo("Stride.Assets.Presentation.Wpf" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.GameStudio.AutoTesting" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.GameStudio.Tests" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Assets.Presentation.Common.Tests" + Stride.PublicKeys.Default)]
