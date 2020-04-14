// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;

namespace Stride.Core.Assets.Compiler
{
    /// <summary>
    /// The context used when compiling an asset in a Package.
    /// </summary>
    public class AssetCompilerContext : CompilerContext
    {
        /// <summary>
        /// Gets or sets the name of the profile being built.
        /// </summary>
        public string Profile { get; set; }

        /// <summary>
        /// Gets or sets the build configuration (Debug, Release, AppStore, Testing)
        /// </summary>
        public string BuildConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the entry package this build was called upon.
        /// </summary>
        public Package Package { get; set; }

        /// <summary>
        /// Gets or sets the target platform for compiler is being used for.
        /// </summary>
        /// <value>The platform.</value>
        public PlatformType Platform { get; set; }

        /// <summary>
        /// The compilation context type of this compiler context
        /// </summary>
        public Type CompilationContext;
    }
}
