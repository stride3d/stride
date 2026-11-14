// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using Stride.Assets.Editor.ViewModels;
using Stride.Rendering.Materials;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Editor.AssetEditors.GameEditor.Game;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.EditorGame;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor
{
    /// <summary>
    /// Describes an editor rendering mode. They will be displayed in scene editor combobox (Game, Editor, Diffuse, etc...)
    /// </summary>
    public class EditorRenderMode
    {
        public static readonly EditorRenderMode DefaultEditor = new EditorRenderMode("Editor");
        public static readonly EditorRenderMode DefaultGamePreview = new EditorRenderMode("Game preview") { PreviewGameGraphicsCompositor = true };

        public EditorRenderMode(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Initializes an <see cref="EditorRenderMode"/> from a <see cref="MaterialStreamDescriptor"/>.
        /// </summary>
        /// <param name="streamDescriptor"></param>
        public EditorRenderMode(MaterialStreamDescriptor streamDescriptor)
        {
            Name = streamDescriptor.Name;
            Mode = SceneRenderMode.SingleStream;
            StreamDescriptor = streamDescriptor;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets whether this item only separates the other render modes in a list (its name is "-").
        /// </summary>
        public bool IsSeparator => Name == "-";

        /// <summary>
        /// Gets the render mode.
        /// </summary>
        public SceneRenderMode Mode { get; private set; } = SceneRenderMode.Lighting;

        /// <summary>
        /// Gets or sets a boolean indicating whether we should use user graphics compositor instead of editor one.
        /// </summary>
        public bool PreviewGameGraphicsCompositor { get; set; }

        /// <summary>
        /// Gets the material stream (if <see cref="Mode"/> is <see cref="SceneRenderMode.SingleStream"/>).
        /// </summary>
        public MaterialStreamDescriptor StreamDescriptor { get; private set; }
    }
}
