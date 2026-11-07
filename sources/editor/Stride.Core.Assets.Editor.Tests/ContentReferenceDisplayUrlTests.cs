// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The unfocused display of a reference field in the property grid: the dimmed location, then the name of the asset
/// (<see cref="ContentReferenceDisplayUrl"/>).
/// </summary>
public sealed class ContentReferenceDisplayUrlTests
{
    private const string DifferentValues = "(Different values)";

    [Fact]
    public async Task TheUrlOfTheGameIsShownWithoutItsNamespace()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Materials/Wood", "wood"), ("Stone", "stone"));
        var wood = fixture.Session.GetAssetByUrl(EditorSessionFixture.Url("Materials/Wood"))!;
        var stone = fixture.Session.GetAssetByUrl(EditorSessionFixture.Url("Stone"))!;

        // The game's own namespace is in scope: its assets resolve by their bare URL, as in WPF
        Assert.Equal(("Materials", "Wood"), Split(fixture, new AssetReference(wood.Id, wood.Url)));
        Assert.Equal(("", "Stone"), Split(fixture, new AssetReference(stone.Id, stone.Url)));
    }

    [Fact]
    public async Task EmptyBrokenAndDifferentReferencesShowAText()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Wood", "wood"));

        Assert.Equal(("", ContentReferenceHelper.EmptyReference), Split(fixture, null));
        Assert.Equal(("", ContentReferenceHelper.BrokenReference), Split(fixture, new AssetReference(AssetId.New(), "Gone")));
        Assert.Equal(("", DifferentValues), Split(fixture, NodeViewModel.DifferentValues));
    }

    private static (string, string) Split(EditorSessionFixture fixture, object? value) => ContentReferenceDisplayUrl.Split(value, fixture.Session, DifferentValues);
}
