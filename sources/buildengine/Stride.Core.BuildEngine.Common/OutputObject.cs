// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;

namespace Stride.Core.BuildEngine
{
    /// <summary>
    /// A build output object, as exposed by <see cref="ListBuildStep"/>. This object represents the hash of an output object and its associated url.
    /// </summary>
    public class OutputObject
    {
        /// <summary>
        /// The url of the object.
        /// </summary>
        public readonly ObjectUrl Url;
        /// <summary>
        /// The hash of the object.
        /// </summary>
        public ObjectId ObjectId;
        /// <summary>
        /// The tags associated to the object.
        /// </summary>
        public readonly HashSet<string> Tags;
        /// <summary>
        /// An internal counter used to manage output object merging.
        /// </summary>
        protected internal int Counter;
        /// <summary>
        /// The command that generated this object.
        /// </summary>
        protected internal Command Command;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputObject"/> class.
        /// </summary>
        /// <param name="url">The url of the output object.</param>
        /// <param name="objectId">The hash of the output object.</param>
        public OutputObject(ObjectUrl url, ObjectId objectId)
        {
            Url = url;
            ObjectId = objectId;
            Tags = new HashSet<string>();
        }
    }
}
