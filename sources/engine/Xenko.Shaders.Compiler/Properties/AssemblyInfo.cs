// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using System.Runtime.CompilerServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

#pragma warning disable 436 // Xenko.PublicKeys is defined in multiple assemblies

[assembly: InternalsVisibleTo("Xenko.Shaders.Compiler.Serializers" + Xenko.PublicKeys.Default)]
