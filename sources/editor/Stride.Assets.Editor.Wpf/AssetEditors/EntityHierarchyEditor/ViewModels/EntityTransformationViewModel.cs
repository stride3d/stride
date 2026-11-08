// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Editor.AssetEditors.GameEditor;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.EditorGame.Tools;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    /// <summary>
    /// The transformation toolbar of the WPF scene and prefab editors: the active transformation gizmo, its space, its snaps
    /// and its size. It drives the shared tool service of the game (<see cref="IEditorGameToolViewModelService"/>), whose keys
    /// (W, E, R, Space, Q) it follows.
    /// </summary>
    public sealed class EntityTransformationViewModel : DispatcherViewModel
    {
        private readonly IEditorGameToolViewModelService service;
        private Transformation? activeTransformation = Transformation.Translation;
        private TransformationSpace space = TransformationSpace.WorldSpace;
        private double gizmoSize = 1.0;
        private IReadOnlyCollection<TransformationSpace> availableSpaces;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityTransformationViewModel"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for this view model.</param>
        /// <param name="controller">The controller of the editor game, which has the tool service.</param>
        public EntityTransformationViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] IEditorGameController controller)
            : base(serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(controller);
            service = controller.GetService<IEditorGameToolViewModelService>() ?? throw new InvalidOperationException("The game of the editor has no tool service.");
            TranslationSnap = new SnapInfoViewModel(ServiceProvider, service, Transformation.Translation);
            RotationSnap = new SnapInfoViewModel(ServiceProvider, service, Transformation.Rotation);
            ScaleSnap = new SnapInfoViewModel(ServiceProvider, service, Transformation.Scale);
            service.ActiveToolChanged += ServiceActiveToolChanged;
            UpdateSpace();
        }

        /// <summary>
        /// Gets or sets the active transformation, or <c>null</c> when the active tool has no transformation gizmo (the select
        /// tool, Q, or a tool of a plugin).
        /// </summary>
        public Transformation? ActiveTransformation
        {
            get => activeTransformation;
            set
            {
                // The list of the toolbar clears its selection while it changes
                if (value is not { } transformation || activeTransformation == value)
                    return;

                SetValue(ref activeTransformation, value, UpdateSpace);
                if (FindTool(transformation) is { } tool)
                    service.ActivateAsync(tool).Forget();
            }
        }

        /// <summary>
        /// Gets or sets the space of the translation and rotation gizmos.
        /// </summary>
        public TransformationSpace Space
        {
            get => space;
            set => SetValue(ref space, value, () => service.SetSpaceAsync(value).Forget());
        }

        /// <summary>
        /// Gets the spaces the active transformation works in: the scale works in the object space only.
        /// </summary>
        public IReadOnlyCollection<TransformationSpace> AvailableSpaces { get => availableSpaces; private set => SetValue(ref availableSpaces, value); }

        /// <summary>
        /// Gets the snap of the translations.
        /// </summary>
        public SnapInfoViewModel TranslationSnap { get; }

        /// <summary>
        /// Gets the snap of the rotations.
        /// </summary>
        public SnapInfoViewModel RotationSnap { get; }

        /// <summary>
        /// Gets the snap of the scales.
        /// </summary>
        public SnapInfoViewModel ScaleSnap { get; }

        /// <summary>
        /// Gets or sets the size of the transformation gizmos.
        /// </summary>
        public double GizmoSize
        {
            get => gizmoSize;
            set => SetValue(Math.Abs(gizmoSize - value) > MathUtil.ZeroTolerance, () =>
            {
                gizmoSize = value;
                service.SetGizmoSizeAsync((float)value).Forget();
            });
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(EntityTransformationViewModel));
            service.ActiveToolChanged -= ServiceActiveToolChanged;
            base.Destroy();
        }

        /// <summary>
        /// Shows the snaps and the gizmo size saved with the scene, and gives them to the game.
        /// </summary>
        public void LoadSettings([NotNull] SceneSettingsData sceneSettings)
        {
            TranslationSnap.Value = sceneSettings.TranslationSnapValue;
            TranslationSnap.IsActive = sceneSettings.TranslationSnapActive;
            RotationSnap.Value = sceneSettings.RotationSnapValue;
            RotationSnap.IsActive = sceneSettings.RotationSnapActive;
            ScaleSnap.Value = sceneSettings.ScaleSnapValue;
            ScaleSnap.IsActive = sceneSettings.ScaleSnapActive;
            GizmoSize = sceneSettings.TransformationGizmoSize;
            // The snaps that didn't change still go to the game, which starts with its own defaults
            TranslationSnap.Apply();
            RotationSnap.Apply();
            ScaleSnap.Apply();
            service.SetGizmoSizeAsync((float)GizmoSize).Forget();
            UpdateSpace();
        }

        /// <summary>
        /// Writes the snaps and the gizmo size in the settings of the scene.
        /// </summary>
        public void SaveSettings([NotNull] SceneSettingsData sceneSettings)
        {
            sceneSettings.TranslationSnapValue = TranslationSnap.Value;
            sceneSettings.TranslationSnapActive = TranslationSnap.IsActive;
            sceneSettings.RotationSnapValue = RotationSnap.Value;
            sceneSettings.RotationSnapActive = RotationSnap.IsActive;
            sceneSettings.ScaleSnapValue = ScaleSnap.Value;
            sceneSettings.ScaleSnapActive = ScaleSnap.IsActive;
            sceneSettings.TransformationGizmoSize = GizmoSize;
        }

        private IEditorViewTool FindTool(Transformation transformation)
        {
            return service.Tools.OfType<TransformationTool>().FirstOrDefault(x => x.Transformation == transformation);
        }

        private void ServiceActiveToolChanged(object sender, IEditorViewTool tool)
        {
            // A key of the game changed the tool (game thread)
            Dispatcher.InvokeAsync(() =>
            {
                if (IsDestroyed)
                    return;

                var transformation = (tool as TransformationTool)?.Transformation;
                if (activeTransformation != transformation)
                    SetValue(ref activeTransformation, transformation, UpdateSpace, nameof(ActiveTransformation));
            });
        }

        private void UpdateSpace()
        {
            var spaces = new List<TransformationSpace>();
            if (ActiveTransformation == Transformation.Scale)
            {
                spaces.Add(TransformationSpace.ObjectSpace);
            }
            else if (ActiveTransformation is not null)
            {
                spaces.Add(TransformationSpace.WorldSpace);
                spaces.Add(TransformationSpace.ObjectSpace);
                spaces.Add(TransformationSpace.ViewSpace);
            }
            AvailableSpaces = spaces;

            if (spaces.Count > 0 && !spaces.Contains(Space))
            {
                Space = spaces.First();
            }
        }
    }
}
