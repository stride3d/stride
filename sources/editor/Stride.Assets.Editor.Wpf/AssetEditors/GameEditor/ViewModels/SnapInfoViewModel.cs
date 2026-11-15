// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Editor.AssetEditors.GameEditor;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels
{
    /// <summary>
    /// A view model representing the snapping parameters of an entity transformation.
    /// </summary>
    public sealed class SnapInfoViewModel : DispatcherViewModel
    {
        private readonly IEditorGameToolViewModelService service;
        private readonly Transformation transformation;
        private bool isActive;
        private float value;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapInfoViewModel"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for this view model.</param>
        /// <param name="service">The tool service of the editor game, which snaps the gizmos.</param>
        /// <param name="transformation">The transformation related to this view model.</param>
        public SnapInfoViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] IEditorGameToolViewModelService service, Transformation transformation)
            : base(serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(service);
            this.service = service;
            this.transformation = transformation;
        }

        /// <summary>
        /// Gets or sets whether the snapping is currently active for the related transformation.
        /// </summary>
        public bool IsActive { get { return isActive; } set { SetValue(ref isActive, value, Apply); } }

        /// <summary>
        /// Gets or sets the snapping value for the related transformation.
        /// </summary>
        public float Value { get { return value; } set { SetValue(ref this.value, value, Apply); } }

        /// <summary>
        /// Gives the snap to the game.
        /// </summary>
        public void Apply()
        {
            service.SetSnapAsync(transformation, Value, IsActive).Forget();
        }
    }
}
