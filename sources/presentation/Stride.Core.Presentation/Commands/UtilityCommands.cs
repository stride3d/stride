// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Stride.Core.Annotations;
using Stride.Core.Presentation.View;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Presentation.Commands
{
    public static class UtilityCommands
    {
        private static readonly Lazy<ICommandBase> LazyOpenHyperlinkCommand = new Lazy<ICommandBase>(OpenHyperlinkCommandFactory);

        public static ICommandBase OpenHyperlinkCommand => LazyOpenHyperlinkCommand.Value;

        [NotNull]
        private static ICommandBase OpenHyperlinkCommandFactory()
        {
            // TODO: have a proper way to initialize the services (maybe at application startup)
            var serviceProvider = new ViewModelServiceProvider(new[] { new DispatcherService(Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher) });
            return new AnonymousCommand<string>(serviceProvider, OpenHyperlink, CanOpenHyperlink);
        }

        private static bool CanOpenHyperlink(string url)
        {
            return !string.IsNullOrEmpty(url) && Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute);
        }

        private static void OpenHyperlink([NotNull] string url)
        {
            // see https://support.microsoft.com/en-us/kb/305703
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                if (e.ErrorCode == -2147467259)
                    MessageBox.Show(e.Message);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }
}
