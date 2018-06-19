// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Assets.Compiler;

namespace Xenko.Core.Assets.Analysis
{
    [Flags]
    public enum BuildDependencyType
    {
        /// <summary>
        /// The content generated during compilation needs the content compiled from the target asset to be loaded at runtime.
        /// </summary>
        Runtime = 0x1,
        /// <summary>
        /// The uncompiled target asset is accessed during compilation.
        /// </summary>
        CompileAsset = 0x2,
        /// <summary>
        /// The content compiled from the target asset is needed during compilation.
        /// </summary>
        CompileContent = 0x4
    }
}
