// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Analysis
{
    /// <summary>
    /// Updatable reference link returned by <see cref="AssetReferenceAnalysis.Visit"/>.
    /// </summary>
    [DebuggerDisplay("{Path}")]
    public class AssetReferenceLink
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetReferenceLink" /> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="reference">The reference.</param>
        /// <param name="updateReference">The update reference.</param>
        public AssetReferenceLink(MemberPath path, object reference, Func<AssetId?, string, object> updateReference)
        {
            Path = path;
            this.reference = reference;
            this.updateReference = updateReference;
        }

        /// <summary>
        /// The path to the member holding this reference.
        /// </summary>
        public readonly MemberPath Path;

        /// <summary>
        /// A <see cref="IReference"/> or <see cref="UFile"/>.
        /// </summary>
        public object Reference
        {
            get
            {
                return reference;
            }
        }

        /// <summary>
        /// Updates the reference.
        /// </summary>
        /// <param name="guid">The unique identifier.</param>
        /// <param name="location">The location.</param>
        public void UpdateReference(AssetId? guid, string location)
        {
            reference = updateReference(guid, location);
        }

        /// <summary>
        /// A specialized method to update the reference (guid, and location).
        /// </summary>
        private readonly Func<AssetId?, string, object> updateReference;

        private object reference;
    }
}
