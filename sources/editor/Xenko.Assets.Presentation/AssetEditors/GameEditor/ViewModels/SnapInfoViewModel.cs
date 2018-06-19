// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Services;

namespace Xenko.Assets.Presentation.AssetEditors.GameEditor.ViewModels
{
    /// <summary>
    /// A view model representing the snapping parameters of an entity transformation.
    /// </summary>
    public class SnapInfoViewModel : DispatcherViewModel
    {
        private readonly IEditorGameController controller;
        private readonly Transformation transformation;
        private bool isActive;
        private float value;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapInfoViewModel"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for this view model.</param>
        /// <param name="controller">The controller of the editor game that can give access to a <see cref="IEditorGameTransformViewModelService"/>.</param>
        /// <param name="transformation">The transformation related to this view model.</param>
        public SnapInfoViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] IEditorGameController controller, Transformation transformation)
            : base(serviceProvider)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            this.controller = controller;
            this.transformation = transformation;
        }

        /// <summary>
        /// Gets or sets whether the snapping is currently active for the related transformation.
        /// </summary>
        public bool IsActive { get { return isActive; } set { SetValue(ref isActive, value, UpdateSnap); } }

        /// <summary>
        /// Gets or sets the snapping value for the related transformation.
        /// </summary>
        public float Value { get { return value; } set { SetValue(ref this.value, value, UpdateSnap); } }

        private void UpdateSnap()
        {
            controller.GetService<IEditorGameTransformViewModelService>().UpdateSnap(transformation, Value, IsActive);
        }
    }
}
