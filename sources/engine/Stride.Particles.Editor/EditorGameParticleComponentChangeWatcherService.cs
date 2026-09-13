// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Quantum;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Editor.EditorGame.Game;
using Stride.Particles.Components;
using Stride.Particles.Materials;

namespace Stride.Particles.Editor
{
    /// <summary>
    /// Refreshes a particle material when one of its blending properties changes in the property grid.
    /// </summary>
    [EditorGameService(typeof(EntityHierarchyEditorController), Order = 250)]
    public class EditorGameParticleComponentChangeWatcherService : EditorGameComponentChangeWatcherService
    {
        public EditorGameParticleComponentChangeWatcherService(IEditorGameController controller)
            : base(controller)
        {
        }

        public override Type ComponentType => typeof(ParticleSystemComponent);

        protected override void ComponentPropertyChanged(object sender, INodeChangeEventArgs e)
        {
            var memberNode = e.Node as IMemberNode;
            if (memberNode == null)
                return;

            if (memberNode.Name == nameof(ParticleMaterialSimple.AlphaAdditive) ||
                memberNode.Name == nameof(ParticleMaterialSimple.ZOffset) ||
                memberNode.Name == nameof(ParticleMaterialSimple.SoftEdgeDistance))
            {
                (memberNode.Parent.Retrieve() as ParticleMaterialSimple)?.ForceUpdate();
            }
        }
    }
}