// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Quantum.References
{
    public interface IReference : IEquatable<IReference>
    {
        /// <summary>
        /// Gets the data object targeted by this reference, if available.
        /// </summary>
        object ObjectValue { get; }
    }
}
