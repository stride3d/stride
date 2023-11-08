// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.IO;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.ViewModels;

partial class SessionViewModel
{
    public static async Task<SessionViewModel?> OpenSessionAsync(UFile path, PackageSessionResult sessionResult, IViewModelServiceProvider serviceProvider, CancellationToken token = default)
    {
        // Create the service that handles copy/paste
        serviceProvider.RegisterService(new CopyPasteService());

        // Create the undo/redo service for this session. We use an initial size of 0 to prevent asset upgrade to be cancellable.
        var actionService = new UndoRedoService(0);
        serviceProvider.RegisterService(actionService);
        
        var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        token = cancellationSource.Token;
        var workProgress = new WorkProgressViewModel(serviceProvider, sessionResult)
        {
            Title = Tr._p("Title", "Opening session..."),
            KeepOpen = KeepOpen.OnWarningsOrErrors,
            IsIndeterminate = true,
            CancelCommand = new AnonymousCommand(serviceProvider, cancellationSource.Cancel)
        };
        workProgress.RegisterProgressStatus(sessionResult);

        serviceProvider.Get<IEditorDialogService>().ShowProgressWindow(workProgress);

        var sessionViewModel = await Task.Run(() =>
        {
            SessionViewModel? result = null;
            try
            {
                PackageSession.Load(path, sessionResult, CreatePackageLoadParameters(token));
                if (!token.IsCancellationRequested)
                {
                    result = new SessionViewModel(serviceProvider, sessionResult.Session, sessionResult);

                    // Build asset view models
                    result.LoadAssetsFromPackages(workProgress, token);
                }
            }
            catch (Exception ex)
            {
                sessionResult.Error(Tr._p("Log", "There was a problem opening the session."), ex);
                result = null;
            }

            return result;

        }, token);

        sessionViewModel?.AutoSelectCurrentProject();

        // Now resize the undo stack to the correct size.
        actionService.Resize(200);

        // Notify that the task is finished
        sessionResult.OperationCancelled = cancellationSource.IsCancellationRequested;
        await workProgress.NotifyWorkFinished(cancellationSource.IsCancellationRequested, sessionResult.HasErrors);

        // TODO: wait for window closing before returning the session

        return sessionViewModel;
    }

    private static PackageLoadParameters CreatePackageLoadParameters(CancellationToken token)
    {
        return new PackageLoadParameters
        {
            CancelToken = token,
            PackageUpgradeRequested = (package, pendingUpgrades) =>
            {
                // FIXME xplat-editor
                //// Generate message (in markdown, so we need to double line feeds)
                //// Note: ** is markdown
                //var message = new StringBuilder();
                //message.AppendLine(string.Format(Tr._p("Message", "The following dependencies in the **{0}** package need to be upgraded:"), package.Meta.Name));
                //message.AppendLine();

                //foreach (var pendingUpgrade in pendingUpgrades)
                //{
                //    message.AppendLine(string.Format(Tr._p("Message", "- Dependency to **{0}** must be upgraded from version **{1}** to **{2}**"), pendingUpgrade.Dependency.Name, pendingUpgrade.Dependency.Version, pendingUpgrade.PackageUpgrader.Attribute.UpdatedVersionRange.MinVersion));
                //}

                //message.AppendLine();
                //message.AppendLine(string.Format(Tr._p("Message", "Upgrading assets might break them. We recommend you make a manual backup of your project before you upgrade."), package.Meta.Name));

                //var buttons = new[]
                //{
                //    new DialogButtonInfo(Tr._p("Button", "Upgrade"), (int)PackageUpgradeRequestedAnswer.Upgrade),
                //    new DialogButtonInfo(Tr._p("Button", "Skip"), (int)PackageUpgradeRequestedAnswer.DoNotUpgrade),
                //};
                //var checkBoxMessage = Tr._p("Message", "Do this for every package in the solution");
                //var messageBoxResult = workProgress.ServiceProvider.Get<IDialogService>().CheckedMessageBox(message.ToString(), false, checkBoxMessage, buttons).Result;
                //var result = (PackageUpgradeRequestedAnswer)messageBoxResult.Result;
                //if (messageBoxResult.IsChecked == true)
                //{
                //    result = result == PackageUpgradeRequestedAnswer.Upgrade ? PackageUpgradeRequestedAnswer.UpgradeAll : PackageUpgradeRequestedAnswer.DoNotUpgradeAny;
                //}
                //return result;
                return PackageUpgradeRequestedAnswer.UpgradeAll;
            }
        };
    }

}
