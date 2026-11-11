// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;
using Stride.GameStudio.Remote;
using Stride.GameStudio.Services;

namespace Stride.GameStudio.Deployment;

/// <summary>
/// The deploy step of the Linux and macOS games: the game is copied to the remote host of the editor settings over SSH, and started
/// there (<see cref="RemoteFacilities"/>), after asking for the credentials when the settings say so.
/// </summary>
internal sealed class RemoteGameDeployer : IGameDeployer
{
    private readonly IViewModelServiceProvider serviceProvider;

    public RemoteGameDeployer(IViewModelServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public Task<GameProjectBuildRequest?> PrepareBuildAsync(ProjectViewModel project, GameProjectBuildRequest request, bool startProject, ILogger logger, CancellationToken cancellationToken)
    {
        // Built as it is: the deployment is after the build
        return Task.FromResult<GameProjectBuildRequest?>(request);
    }

    /// <inheritdoc/>
    public async Task<bool> DeployAsync(ProjectViewModel project, IGameProjectBuild build, ILogger logger, CancellationToken cancellationToken)
    {
        var assemblyPath = build.AssemblyPath;
        // Sanity check to verify executable was compiled properly
        if (string.Equals(Path.GetExtension(assemblyPath), ".exe", StringComparison.InvariantCultureIgnoreCase) && !File.Exists(assemblyPath))
        {
            logger.Error(string.Format(Tr._p("Message", "Unable to reach to output executable: {0}"), assemblyPath));
            return false;
        }

        // Ask for credentials if requested, otherwise we use what we have stored.
        if (StrideEditorSettings.AskForCredentials.GetValue())
        {
            var prompt = serviceProvider.Get<IStrideDialogService>().CreateCredentialsDialog();
            await prompt.ShowModal();
            if (!prompt.AreCredentialsValid)
            {
                logger.Error(Tr._p("Message", "No credentials provided. To allow deployment, add your credentials."));
                return false;
            }
        }
        cancellationToken.ThrowIfCancellationRequested();

        // Launch game on remote host
        var launched = await Task.Run(() => RemoteFacilities.Launch(logger, new UFile(assemblyPath)), cancellationToken);
        if (!launched)
        {
            logger.Error(string.Format(Tr._p("Message", "Unable to launch project {0}"), new UFile(assemblyPath).GetFileName()));
            return false;
        }
        return true;
    }
}
