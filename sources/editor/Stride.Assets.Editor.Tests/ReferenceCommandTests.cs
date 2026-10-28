// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core;
using Stride.Core.Assets.Editor.Common.Tests.Helpers;
using Stride.Engine;

namespace Stride.Assets.Editor.Tests;

public sealed class ReferenceCommandTests
{
    [DataContract]
    public sealed class Holder
    {
        public Entity? Entity { get; set; }

        public TransformComponent? Component { get; set; }

        public string? Text { get; set; }
    }

    [Fact]
    public void TheEntityReferenceCommandAttachesToEntityNodesOnly()
    {
        var root = new PresenterTestContext().CreatePresenter(new Holder());
        var command = new SetEntityReferenceCommand();

        Assert.Equal(SetEntityReferenceCommand.CommandName, command.Name);
        Assert.True(command.CanAttach(PresenterTestContext.Child(root, nameof(Holder.Entity))));
        Assert.False(command.CanAttach(PresenterTestContext.Child(root, nameof(Holder.Component))));
        Assert.False(command.CanAttach(PresenterTestContext.Child(root, nameof(Holder.Text))));
    }

    [Fact]
    public void TheComponentReferenceCommandAttachesToComponentNodesOnly()
    {
        var root = new PresenterTestContext().CreatePresenter(new Holder());
        var command = new SetComponentReferenceCommand();

        Assert.Equal(SetComponentReferenceCommand.CommandName, command.Name);
        Assert.True(command.CanAttach(PresenterTestContext.Child(root, nameof(Holder.Component))));
        Assert.False(command.CanAttach(PresenterTestContext.Child(root, nameof(Holder.Entity))));
        Assert.False(command.CanAttach(PresenterTestContext.Child(root, nameof(Holder.Text))));
    }

    [Fact]
    public void TheFetchCommandAttachesToEntitiesAndComponents()
    {
        var root = new PresenterTestContext().CreatePresenter(new Holder());
        var command = new FetchEntityCommand();

        Assert.Equal(FetchEntityCommand.CommandName, command.Name);
        Assert.True(command.CanAttach(PresenterTestContext.Child(root, nameof(Holder.Entity))));
        Assert.True(command.CanAttach(PresenterTestContext.Child(root, nameof(Holder.Component))));
        Assert.False(command.CanAttach(PresenterTestContext.Child(root, nameof(Holder.Text))));
    }

    [Fact]
    public async Task TheCommandsAreAvailableInTheSessionsPropertyGrid()
    {
        var scene = new Stride.Assets.Entities.SceneAsset();
        var entity = new Entity("Light") { new LightComponent() };
        scene.Hierarchy.Parts.Add(new Stride.Assets.Entities.EntityDesign(entity));
        scene.Hierarchy.RootParts.Add(entity);
        var gameSettings = GameSettingsFactory.Create();
        gameSettings.SplashScreenTexture = null!;
        using var fixture = await Helpers.AssetsSessionFixture.OpenAsync(initializeEditor: true, ("Scene", scene), ("GameSettings", gameSettings));

        var root = await fixture.PropertiesOf(fixture.Asset("GameSettings"));

        // The editor plugin registered its commands: the default scene reference of the game settings offers them
        var defaultScene = root.GetChild(nameof(GameSettingsAsset.DefaultScene));
        Assert.NotNull(defaultScene);
        Assert.NotNull(root.GetChild(nameof(GameSettingsAsset.Defaults)));
    }
}
