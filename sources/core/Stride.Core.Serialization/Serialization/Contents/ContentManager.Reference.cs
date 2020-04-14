// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

namespace Stride.Core.Serialization.Contents
{
    partial class ContentManager
    {
        /// <summary>
        /// Internal object that represents a loaded asset, with its url and reference counts.
        /// </summary>
        internal class Reference
        {
            /// <summary>
            /// The next item in the linked list.
            /// </summary>
            public Reference Next;

            public Reference Prev;

            public bool Deserialized;

            /// <summary>
            /// The object being referenced.
            /// </summary>
            public object Object;

            /// <summary>
            /// The URL.
            /// </summary>
            public readonly string Url;

            /// <summary>
            /// The public reference count (corresponding to ContentManager.Load/Unload).
            /// </summary>
            public int PublicReferenceCount;

            /// <summary>
            /// The private reference count (corresponding to an object being referenced indirectly by other loaded objects).
            /// </summary>
            public int PrivateReferenceCount;

            // Used internally for GC (maybe we could just use higher byte of PrivateReferenceCount or something like that?)
            public uint CollectIndex;

            // TODO: Lazily create this list?
            public HashSet<Reference> References = new HashSet<Reference>();

            public Reference(string url, bool publicReference)
            {
                Url = url;
                PublicReferenceCount = publicReference ? 1 : 0;
                PrivateReferenceCount = publicReference ? 0 : 1;
                CollectIndex = uint.MaxValue;
            }

            public override string ToString()
            {
                return $"{Object}, references: {PublicReferenceCount} public(s), {PrivateReferenceCount} private(s)";
            }
        }
    }
}
