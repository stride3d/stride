// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("a9968d1f-7e75-4d89-8411-27390a47e4d0")]

#pragma warning disable 436 // Xenko.PublicKeys is defined in multiple assemblies

// Make internals Xenko visible to Xenko assemblies
// TODO: Needed for ParameterCollection getters, but it would be better to avoid this kind of dependency.
[assembly: InternalsVisibleTo("Xenko.Assets" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Assets.Presentation" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Editor" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Engine.Serializers" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Engine.Shaders" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Assets.Tests" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Graphics.Regression" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Debugger" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Audio.Tests" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("XenkoAudioTests" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Engine.Audio.Tests" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Graphics.Tests" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("XenkoGraphicsTests" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.Engine.Tests" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("XenkoEngineTests" + Xenko.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Xenko.VirtualReality" + Xenko.PublicKeys.Default)]
