// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using System;
using System.IO;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Dirtiables;

namespace Stride.Core.Assets.Presentation.ViewModels
{
    /// <summary>
    /// Represents the delete of a project asset. Its file can change outside Game Studio at any time, so undo and redo follow the disk
    /// (see <see cref="ProjectAssetDeletion"/>). It dirties the folders but not the asset, as the content of the asset does not change.
    /// </summary>
    internal sealed class ProjectAssetDeleteOperation : DirtyingOperation
    {
        private static readonly Logger Logger = GlobalLogger.GetLogger(nameof(ProjectAssetDeleteOperation));

        private readonly bool deletedOutside;
        private AssetViewModel asset;
        private DirectoryBaseViewModel directory;

        public ProjectAssetDeleteOperation([NotNull] AssetViewModel asset, [NotNull] DirectoryBaseViewModel directory, bool deletedOutside)
            : base(directory.SafeArgument(nameof(directory)).Dirtiables)
        {
            this.asset = asset ?? throw new ArgumentNullException(nameof(asset));
            this.directory = directory;
            this.deletedOutside = deletedOutside;
        }

        /// <inheritdoc />
        protected override void FreezeContent()
        {
            asset = null;
            directory = null;
        }

        /// <inheritdoc />
        protected override void Undo()
        {
            var filePath = asset.AssetItem.FullPath.ToOSPath();
            var action = ProjectAssetDeletion.Undo(asset.IsDeleted, asset.IsDeleted && asset.IsFileOfAnotherAsset(), File.Exists(filePath));
            if (action == ProjectAssetUndoAction.Nothing)
                return;

            // The file comes first: a restored script with no file counts as changed, and a save would rewrite it
            if (action == ProjectAssetUndoAction.RestoreAndWriteFile && !TryWriteFile(filePath))
                return;

            asset.RestoreProjectAsset(directory);
        }

        /// <inheritdoc />
        protected override void Redo()
        {
            var filePath = asset.AssetItem.FullPath.ToOSPath();
            if (ProjectAssetDeletion.Redo(asset.IsDeleted, deletedOutside, File.Exists(filePath)))
                asset.RemoveProjectAsset(deletedOutside);
        }

        private bool TryWriteFile(string filePath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                if (asset.Asset is SourceCodeAsset sourceCode)
                {
                    File.WriteAllText(filePath, sourceCode.Text);
                    return true;
                }

                return Package.SaveSingleAsset(asset.AssetItem, Logger);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Logger.Error($"Could not write back the file '{filePath}'", e);
                return false;
            }
        }
    }
}
