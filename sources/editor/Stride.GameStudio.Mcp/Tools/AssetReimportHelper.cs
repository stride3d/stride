// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;

namespace Stride.GameStudio.Mcp.Tools;

/// <summary>
/// Shared helper for reimporting assets whose source files have changed on disk.
/// Used by both <see cref="BuildProjectTool"/> and <see cref="SaveProjectTool"/>.
/// Must be called on the UI thread.
/// </summary>
internal static class AssetReimportHelper
{
    /// <summary>
    /// Reimports all assets whose source files have been modified on disk.
    /// Returns the list of reimported asset URLs.
    /// </summary>
    public static async Task<List<string>> ReimportModifiedAssets(SessionViewModel session)
    {
        var assetsToReimport = session.AllAssets
            .Where(a => a.Sources.NeedUpdateFromSource)
            .ToList();

        if (assetsToReimport.Count == 0)
            return [];

        var logger = new LoggerResult();
        var undoRedoService = session.ServiceProvider.Get<IUndoRedoService>();
        using (var transaction = undoRedoService.CreateTransaction())
        {
            var tasks = assetsToReimport
                .Select(a => a.Sources.UpdateAssetFromSource(logger))
                .ToList();
            await Task.WhenAll(tasks);
            undoRedoService.SetName(transaction, $"Reimport {tasks.Count} asset(s) from source");
        }

        return assetsToReimport.Select(a => a.Url).ToList();
    }
}
