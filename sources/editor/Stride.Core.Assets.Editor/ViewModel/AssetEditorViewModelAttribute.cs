// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Annotations;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// This attribute is used to register an editor view model class and associate it to an asset type and an editor view type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    [BaseTypeRequired(typeof(AssetEditorViewModel))]
    public sealed class AssetEditorViewModelAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetEditorViewModelAttribute"/>.
        /// </summary>
        /// <param name="assetType">The asset type. Must be derivated from <see cref="Asset"/>.</param>
        /// <param name="editorViewType">The type of the editor view.</param>
        public AssetEditorViewModelAttribute(Type assetType, Type editorViewType)
        {
            if (assetType == null) throw new ArgumentNullException(nameof(assetType));
            if (editorViewType == null) throw new ArgumentNullException(nameof(editorViewType));

            if (!typeof(Asset).IsAssignableFrom(assetType))
                throw new ArgumentException($"The asset type must inherits from the {nameof(Asset)} class.");

            if (!editorViewType.HasInterface(typeof(IEditorView)))
                throw new ArgumentException($"The editor view type must implement the {nameof(IEditorView)} interface.");

            AssetType = assetType;
            EditorViewType = editorViewType;
        }

        /// <summary>
        /// Gets or sets the asset type described by this attribute.
        /// </summary>
        public Type AssetType { get; set; }

        /// <summary>
        /// Gets or sets the editor view type associated to the <see cref="AssetType"/>.
        /// </summary>
        public Type EditorViewType { get; set; }
    }
}
