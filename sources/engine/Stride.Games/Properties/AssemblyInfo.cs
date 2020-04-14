// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable 436 // Xenko.PublicKeys is defined in multiple assemblies

[assembly: InternalsVisibleTo("Xenko.Games.Serializers" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Input" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Engine" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.UI" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Debugger" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Graphics.Regression" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.VirtualReality" + Xenko.PublicKeys.Default)]
