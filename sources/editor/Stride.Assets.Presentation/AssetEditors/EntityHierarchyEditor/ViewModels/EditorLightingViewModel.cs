// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.LightProbes;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    public class EditorLightingViewModel : DispatcherViewModel
    {
        private readonly IEditorGameController controller;
        private readonly EntityHierarchyEditorViewModel editor;
        private int lightProbeBounces = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorNavigationViewModel"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for this view model.</param>
        /// <param name="controller">The controller object for the related editor game.</param>
        public EditorLightingViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] IEditorGameController controller, [NotNull] EntityHierarchyEditorViewModel editor)
            : base(serviceProvider)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            if (editor == null) throw new ArgumentNullException(nameof(editor));

            this.controller = controller;
            this.editor = editor;

            RequestLightProbesComputeCommand = new AnonymousTaskCommand(ServiceProvider, () => RebuildLightProbes(LightProbeBounces));
            RequestLightProbesResetCommand = new AnonymousTaskCommand(ServiceProvider, () => RebuildLightProbes(0));

            CaptureCubemapCommand = new AnonymousTaskCommand(ServiceProvider, CaptureCubemap);
        }

        public int LightProbeBounces { get { return lightProbeBounces; } set { SetValue(lightProbeBounces != value, () => lightProbeBounces = value); } }

        public bool LightProbeWireframeVisible { get { return LightProbeService.IsLightProbeVolumesVisible; } set { SetValue(LightProbeWireframeVisible != value, () => LightProbeService.IsLightProbeVolumesVisible = value); } }

        public ICommandBase RequestLightProbesComputeCommand { get; }
        public ICommandBase RequestLightProbesResetCommand { get; }

        public ICommandBase CaptureCubemapCommand { get; }

        private IEditorGameLightProbeService LightProbeService => controller.GetService<IEditorGameLightProbeService>();

        private IEditorGameCubemapService CubemapService => controller.GetService<IEditorGameCubemapService>();

        private async Task RebuildLightProbes(int bounces)
        {
            // Reset values
            using (var transaction = editor.UndoRedoService.CreateTransaction())
            {
                foreach (var entity in editor.HierarchyRoot.Children.SelectDeep(x => x.Children).OfType<EntityViewModel>())
                {
                    foreach (var lightProbe in entity.Components.OfType<LightProbeComponent>())
                    {
                        // Find this light probe in Quantum
                        var assetNode = editor.NodeContainer.GetOrCreateNode(lightProbe);
                        var zeroCoefficients = new FastList<Color3>();
                        for (int i = 0; i < LightProbeGenerator.LambertHamonicOrder * LightProbeGenerator.LambertHamonicOrder; ++i)
                            zeroCoefficients.Add(default(Color3));
                        assetNode[nameof(LightProbeComponent.Coefficients)].Update(zeroCoefficients);
                    }
                }

                // Update coefficients (just updated to zero)
                await LightProbeService.UpdateLightProbeCoefficients();

                for (int bounce = 0; bounce < bounces; ++bounce)
                {
                    // Compute coefficients
                    var result = await LightProbeService.RequestLightProbesStep();

                    // Copy coefficients back to view model
                    editor.UndoRedoService.SetName(transaction, "Capture LightProbes");
                    foreach (var entity in editor.HierarchyRoot.Children.SelectDeep(x => x.Children).OfType<EntityViewModel>().Where(x => result.ContainsKey(x.AssetSideEntity.Id)))
                    {
                        // TODO: Use LightProbe Id instead of entity id once copy/paste and duplicate properly remap them
                        var matchingLightProbe = entity.Components.OfType<LightProbeComponent>().FirstOrDefault();
                        if (matchingLightProbe != null)
                        {
                            // Find this light probe in Quantum
                            var assetNode = editor.NodeContainer.GetOrCreateNode(matchingLightProbe);
                            assetNode[nameof(LightProbeComponent.Coefficients)].Update(result[matchingLightProbe.Entity.Id]);
                        }
                    }

                    // Update coefficients
                    await LightProbeService.UpdateLightProbeCoefficients();
                }
            }
        }

        private async Task CaptureCubemap()
        {
            var dialog = ServiceProvider.Get<IEditorDialogService>().CreateFileSaveModalDialog();
            dialog.Filters.Add(new FileDialogFilter("DDS texture", "dds"));
            dialog.DefaultExtension = "dds";
            if (editor.Session.SolutionPath != null)
                dialog.InitialDirectory = editor.Session.SolutionPath.GetFullDirectory().ToWindowsPath();

            var result = await dialog.ShowModal();
            if (result == DialogResult.Ok)
            {
                // Capture cubemap
                using (var image = await CubemapService.CaptureCubemap())
                {
                    // And save it
                    using (var file = File.Create(dialog.FilePath))
                        image.Save(file, ImageFileType.Dds);
                }
            }
        }
    }
}
