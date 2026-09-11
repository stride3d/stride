// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Audio;
using Stride.Core.Serialization;
using Stride.Engine.Design;

#pragma warning disable 436 // Stride.PublicKeys is defined in multiple assemblies

// Sounds are shared, not copied, when an entity is cloned
[assembly: DataSerializerGlobal(typeof(CloneSerializer<Sound>), Profile = "Clone")]

[assembly: InternalsVisibleTo("Stride.Audio.Serializers" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Audio.Tests" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("StrideAudioTests" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Assets" + Stride.PublicKeys.Default)]
