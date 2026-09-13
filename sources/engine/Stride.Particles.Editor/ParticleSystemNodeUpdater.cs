// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core.Extensions;
using Stride.Engine;
using Stride.Particles.Initializers;
using Stride.Particles.Modules;

namespace Stride.Particles.Editor
{
    /// <summary>
    /// In the property grid of a particle transform (an initializer or an updater), the Display* toggles hide
    /// themselves and show or hide the position, rotation and scale members they stand for.
    /// </summary>
    public class ParticleSystemNodeUpdater : AssetNodePresenterUpdaterBase
    {
        protected override void FinalizeTree(IAssetNodePresenter root)
        {
            // Particle systems are entity components
            if (root.Value is not Entity)
                return;

            foreach (var node in root.Children.BreadthFirst(x => x.Children))
            {
                if ((node.Parent?.Type == typeof(ParticleInitializer) || node.Parent?.Type == typeof(ParticleUpdater)) && node.Parent?.Value is ParticleTransform)
                {
                    if (node.Name == nameof(ParticleTransform.DisplayParticlePosition))
                    {
                        node.IsVisible = false;
                        var isVisible = (bool)node.Value;

                        var attrPosition = node.Parent[nameof(ParticleTransform.Position)];
                        if (attrPosition != null)
                            attrPosition.IsVisible = isVisible;

                        var attrInheritPos = node.Parent[nameof(ParticleTransform.InheritPosition)];
                        if (attrInheritPos != null)
                            attrInheritPos.IsVisible = isVisible;
                    }

                    if (node.Name == nameof(ParticleTransform.DisplayParticleRotation))
                    {
                        node.IsVisible = false;
                        var isVisible = (bool)node.Value;

                        var attrRotation = node.Parent[nameof(ParticleTransform.Rotation)];
                        if (attrRotation != null)
                            attrRotation.IsVisible = isVisible;

                        var attrInheritRot = node.Parent[nameof(ParticleTransform.InheritRotation)];
                        if (attrInheritRot != null)
                            attrInheritRot.IsVisible = isVisible;
                    }

                    if (node.Name == nameof(ParticleTransform.DisplayParticleScale))
                    {
                        node.IsVisible = false;
                        var isVisible = (bool)node.Value;
                        var isVisibleInheritance = isVisible;

                        var attrVisibleUniform = node.Parent[nameof(ParticleTransform.DisplayParticleScaleUniform)];
                        if (attrVisibleUniform != null)
                            isVisibleInheritance |= (bool)attrVisibleUniform.Value;

                        var attrScale = node.Parent[nameof(ParticleTransform.Scale)];
                        if (attrScale != null)
                            attrScale.IsVisible = isVisible;

                        var attrInheritScl = node.Parent[nameof(ParticleTransform.InheritScale)];
                        if (attrInheritScl != null)
                            attrInheritScl.IsVisible = isVisibleInheritance;
                    }

                    if (node.Name == nameof(ParticleTransform.DisplayParticleScaleUniform))
                    {
                        node.IsVisible = false;
                        var isVisible = (bool)node.Value;
                        var isVisibleInheritance = isVisible;

                        // This attribute can be missing
                        var attrVisible = node.Parent[nameof(ParticleTransform.DisplayParticleScale)];
                        if (attrVisible != null)
                            isVisibleInheritance |= (bool)attrVisible.Value;

                        var attrScale = node.Parent[nameof(ParticleTransform.ScaleUniform)];
                        if (attrScale != null)
                            attrScale.IsVisible = isVisible;

                        var attrInheritScl = node.Parent[nameof(ParticleTransform.InheritScale)];
                        if (attrInheritScl != null)
                            attrInheritScl.IsVisible = isVisibleInheritance;
                    }
                }
            }
        }
    }
}
