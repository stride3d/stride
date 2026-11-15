// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;

namespace Stride.Editor.Build
{
    /// <summary>
    /// Arguments of the <see cref="GameStudioBuilderService.ShaderSourcesChanged"/> event.
    /// </summary>
    public sealed class ShaderSourcesChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderSourcesChangedEventArgs"/> class.
        /// </summary>
        /// <param name="files">The full paths of the shader files that changed.</param>
        /// <param name="dependents">The build units whose last build read one of these files.</param>
        public ShaderSourcesChangedEventArgs(IReadOnlyCollection<string> files, IReadOnlyCollection<AssetBuildUnitIdentifier> dependents)
        {
            Files = files ?? throw new ArgumentNullException(nameof(files));
            Dependents = dependents ?? throw new ArgumentNullException(nameof(dependents));
        }

        /// <summary>
        /// Gets the full paths of the shader files that changed.
        /// </summary>
        public IReadOnlyCollection<string> Files { get; }

        /// <summary>
        /// Gets the build units whose last build read one of the changed files: building them again gives a different result.
        /// </summary>
        public IReadOnlyCollection<AssetBuildUnitIdentifier> Dependents { get; }

        /// <summary>
        /// Gets the assets of the dependent build units of one context, such as the thumbnails or the editor games.
        /// </summary>
        /// <param name="contextIdentifier">The context of the build units.</param>
        /// <returns>The identifiers of the assets.</returns>
        public IEnumerable<AssetId> GetDependentAssets(Guid contextIdentifier)
        {
            return Dependents.Where(x => x.ContextIdentifier == contextIdentifier).Select(x => x.AssetIdentifier);
        }
    }
}
