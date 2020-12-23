// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1025 // Code must not contain multiple whitespace in a row
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Stride.Core.Annotations;
using Stride.Core.Storage;

namespace Stride.Core.Serialization
{
    /// <summary>
    /// Describes how to serialize and deserialize an object without knowing its type.
    /// Used as a common base class for all data serializers.
    /// </summary>
    partial class DataSerializer
    {
        // Binary format version, needs to be bumped in case of big changes in serialization formats (i.e. primitive types).
        public const int BinaryFormatVersion = 4 * 1000000 // Major version: any number is ok
                                             + 0 * 10000   // Minor version: supported range: 0-99
                                             + 0 * 100     // Patch version: supported range: 0-99
                                             + 1;          // Bump ID: supported range: 0-99
    }
}
