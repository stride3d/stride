// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;

namespace Stride.Core
{
    /// <summary>
    /// Base interface for all identifiable instances.
    /// </summary>
    public interface IIdentifiable
    {
        /// <summary>
        /// Gets the id of this instance
        /// </summary>
        [NonOverridable]
        Guid Id { get; set; }
    }
}
