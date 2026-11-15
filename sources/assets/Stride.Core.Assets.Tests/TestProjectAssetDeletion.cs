// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Tests;

public class TestProjectAssetDeletion
{
    [Theory]
    // A file that came back already restored the asset
    [InlineData(false, false, true, ProjectAssetUndoAction.Nothing)]
    // A new asset has the file or the location: the asset must not take them back
    [InlineData(true, true, true, ProjectAssetUndoAction.Nothing)]
    [InlineData(true, true, false, ProjectAssetUndoAction.Nothing)]
    [InlineData(true, false, true, ProjectAssetUndoAction.Restore)]
    [InlineData(true, false, false, ProjectAssetUndoAction.RestoreAndWriteFile)]
    public void Undo(bool isDeleted, bool fileHasAnotherAsset, bool fileExists, ProjectAssetUndoAction expected)
    {
        Assert.Equal(expected, ProjectAssetDeletion.Undo(isDeleted, fileHasAnotherAsset, fileExists));
    }

    [Theory]
    [InlineData(true, true, false, false)]
    [InlineData(false, true, false, true)]
    // The file is back: without its asset, a save could delete it
    [InlineData(false, true, true, false)]
    // Deleted in Game Studio: redo applies it again, and a save deletes the file as the user asked
    [InlineData(false, false, true, true)]
    public void Redo(bool isDeleted, bool deletedOutside, bool fileExists, bool expected)
    {
        Assert.Equal(expected, ProjectAssetDeletion.Redo(isDeleted, deletedOutside, fileExists));
    }
}
