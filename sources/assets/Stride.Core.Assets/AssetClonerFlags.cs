// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;

namespace Xenko.Core.Assets
{
    /// <summary>
    /// Flags used by <see cref="AssetCloner"/>
    /// </summary>
    [Flags]
    public enum AssetClonerFlags
    {
        /// <summary>
        /// No special flags while cloning.
        /// </summary>
        None,

        /// <summary>
        /// Attached references will be cloned as <c>null</c>
        /// </summary>
        ReferenceAsNull = 1,

        /// <summary>
        /// Remove ids attached to item of collections when cloning
        /// </summary>
        RemoveItemIds = 2,

        /// <summary>
        /// Removes invalid objects
        /// </summary>
        RemoveUnloadableObjects = 4,

        /// <summary>
        /// Generates new ids for objects that implement <see cref="IIdentifiable"/>.
        /// </summary>
        GenerateNewIdsForIdentifiableObjects = 8,

        /// <summary>
        /// Clears any external references in the cloned object
        /// </summary>
        ClearExternalReferences = 16,

        /// <summary>
        /// Attached references will be kept as is
        /// </summary>
        KeepReferences = 32,
    }
}
