// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Assets.Quantum;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Quantum;
using Stride.Engine;
using Stride.Particles;
using Stride.Particles.Components;
using Stride.Particles.Editor;
using Stride.Particles.Initializers;

namespace Stride.Assets.Editor.Avalonia.Tests;

/// <summary>
/// An updater that shows or hides members from the value of another member still does it when only that value changes.
/// </summary>
public sealed class ValueDrivenUpdaterTests
{
    [Fact]
    public void TheDisplayTogglesOfAParticleTransformStillShowAndHideTheirMembers()
    {
        var emitter = new ParticleEmitter();
        var initializer = new InitialPositionSeed();
        emitter.Initializers.Add(initializer);
        var particles = new ParticleSystemComponent();
        particles.ParticleSystem.Emitters.Add(emitter);
        var entity = new Entity("Particles") { particles };
        // The nodes of any object, without an asset (no property graph), as the grid builds them for an asset part
        var container = new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } };
        var factory = new AssetNodePresenterFactory(container.NodeBuilder, [], [new ParticleSystemNodeUpdater()]);
        var rootNode = container.GetOrCreateNode(entity);
        var root = factory.CreateNodeHierarchy(rootNode, new GraphNodePath(rootNode), null);
        try
        {
            INodePresenter Member(string name) => root.Children.DepthFirst(x => x.Children)
                .Single(x => x.Name == name && x.Parent?.Type == typeof(ParticleInitializer));

            var toggle = Member(nameof(ParticleTransform.DisplayParticlePosition));
            Assert.False(toggle.IsVisible);
            Assert.Equal(initializer.DisplayParticlePosition, Member(nameof(ParticleTransform.Position)).IsVisible);

            toggle.UpdateValue(!initializer.DisplayParticlePosition);
            Assert.Equal(initializer.DisplayParticlePosition, Member(nameof(ParticleTransform.Position)).IsVisible);
            Assert.Equal(initializer.DisplayParticlePosition, Member(nameof(ParticleTransform.InheritPosition)).IsVisible);

            toggle.UpdateValue(!initializer.DisplayParticlePosition);
            Assert.Equal(initializer.DisplayParticlePosition, Member(nameof(ParticleTransform.Position)).IsVisible);
            Assert.Equal(initializer.DisplayParticlePosition, Member(nameof(ParticleTransform.InheritPosition)).IsVisible);
        }
        finally
        {
            foreach (var presenter in root.Children.DepthFirst(x => x.Children).ToList())
                presenter.Dispose();
            root.Dispose();
        }
    }
}
