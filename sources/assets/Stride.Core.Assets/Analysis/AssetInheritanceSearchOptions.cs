// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Xenko.Core.Assets.Analysis
{
    /// <summary>
    /// Possible options used when searching asset inheritance.
    /// </summary>
    [Flags]
    public enum AssetInheritanceSearchOptions
    {
        /// <summary>
        /// Search for inheritances from base (direct object inheritance).
        /// </summary>
        Base = 1,

        /// <summary>
        /// Search for inheritances from compositions.
        /// </summary>
        Composition = 2,

        /// <summary>
        /// Search for all types of inheritances.
        /// </summary>
        All = Base | Composition,
    }
}
