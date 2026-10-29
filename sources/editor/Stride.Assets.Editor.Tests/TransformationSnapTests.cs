// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame.Tools;
using Stride.Assets.Presentation.AssetEditors.GameEditor;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The snaps of the transformation gizmos, as the snap toolbar of the WPF scene editor sets them.
/// </summary>
public sealed class TransformationSnapTests
{
    [Fact]
    public void TheSnapsStartInactiveWithTheValuesOfANewWpfScene()
    {
        var settings = new TransformationSettings();

        Assert.Equal(1.0f, settings.GetSnap(Transformation.Translation));
        Assert.Equal(22.5f, settings.GetSnap(Transformation.Rotation));
        Assert.Equal(1.1f, settings.GetSnap(Transformation.Scale));
        Assert.False(settings.IsSnapActive(Transformation.Translation));
        Assert.False(settings.IsSnapActive(Transformation.Rotation));
        Assert.False(settings.IsSnapActive(Transformation.Scale));
    }

    [Fact]
    public void EachTransformationHasItsOwnSnap()
    {
        var settings = new TransformationSettings();

        settings.SetSnap(Transformation.Rotation, 45, true);

        Assert.Equal(45, settings.GetSnap(Transformation.Rotation));
        Assert.True(settings.IsSnapActive(Transformation.Rotation));
        Assert.False(settings.IsSnapActive(Transformation.Translation));
        Assert.Equal(1.0f, settings.GetSnap(Transformation.Translation));
    }
}
