// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Importer.Common
{
    /// <summary>
    /// Where the normal or tangent deltas of imported morph targets come from.
    /// </summary>
    [DataContract]
    public enum MorphDeltaSource
    {
        /// <summary>
        /// Use the deltas from the source file, and generate them for targets that have none.
        /// </summary>
        ImportOrGenerate,

        /// <summary>
        /// Use only the deltas from the source file.
        /// </summary>
        Import,

        /// <summary>
        /// Always generate the deltas from the target positions.
        /// </summary>
        Generate,

        /// <summary>
        /// Zero deltas: morph targets only move positions. The entries keep their normal and tangent components, set to zero.
        /// </summary>
        Zero,
    }
}
