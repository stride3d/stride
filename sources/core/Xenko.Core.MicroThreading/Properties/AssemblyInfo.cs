// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable 436 // Xenko.PublicKeys is defined in multiple assemblies

// Make internals Xenko.Framework.visible to all Xenko.Framework.assemblies
[assembly: InternalsVisibleTo("Xenko.Core.MicroThreading.Serializers" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Engine" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Debugger" + Xenko.PublicKeys.Default)]
