// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets;

/// <summary>
/// What an undo does to a deleted project asset.
/// </summary>
public enum ProjectAssetUndoAction
{
    /// <summary>The asset stays as it is.</summary>
    Nothing,
    /// <summary>The asset comes back, and its file is already there.</summary>
    Restore,
    /// <summary>The asset comes back, and its file is written again from its content in memory.</summary>
    RestoreAndWriteFile,
}

/// <summary>
/// Decides what undo and redo do to the delete of a project asset. The disk may have changed since the delete,
/// so they follow the disk, and they never delete a file that the user may still want.
/// </summary>
public static class ProjectAssetDeletion
{
    /// <summary>
    /// Decides what an undo of the delete does.
    /// </summary>
    /// <param name="isDeleted">Whether the asset is deleted now. A file that came back may have restored it already.</param>
    /// <param name="fileHasAnotherAsset">Whether another asset now has the file or the location of this asset.</param>
    /// <param name="fileExists">Whether the file of this asset exists.</param>
    public static ProjectAssetUndoAction Undo(bool isDeleted, bool fileHasAnotherAsset, bool fileExists)
    {
        if (!isDeleted || fileHasAnotherAsset)
            return ProjectAssetUndoAction.Nothing;

        return fileExists ? ProjectAssetUndoAction.Restore : ProjectAssetUndoAction.RestoreAndWriteFile;
    }

    /// <summary>
    /// Decides whether a redo of the delete removes the asset.
    /// </summary>
    /// <param name="isDeleted">Whether the asset is deleted now.</param>
    /// <param name="deletedOutside">Whether the delete came from outside Game Studio.</param>
    /// <param name="fileExists">Whether the file of this asset exists.</param>
    /// <returns>False if the asset stays: a file that is back after a delete from outside keeps its asset, or a save could delete it.</returns>
    public static bool Redo(bool isDeleted, bool deletedOutside, bool fileExists)
    {
        return !isDeleted && !(deletedOutside && fileExists);
    }
}
