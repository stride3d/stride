// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable 436 // Xenko.PublicKeys is defined in multiple assemblies

[assembly: InternalsVisibleTo("Xenko.Audio.Serializers" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Engine" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Audio.Tests" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("XenkoAudioTests" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Assets" + Xenko.PublicKeys.Default)]
