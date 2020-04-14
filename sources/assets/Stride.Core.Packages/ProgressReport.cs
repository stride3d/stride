// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using NuGet;

namespace Stride.Core.Packages
{
    public class ProgressReport : IDisposable
    {
        private readonly NugetStore store;
        //private readonly string version;
        private ProgressAction action;
        private int progress;

        public ProgressReport(NugetStore store, NugetPackage package)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            this.store = store;
            //version = package.Version.ToSemanticVersion().ToNormalizedString();

            //foreach (var progressProvider in store.SourceRepository.Repositories.OfType<IProgressProvider>())
            //{
            //    progressProvider.ProgressAvailable += OnProgressAvailable;
            //}
        }

        public event Action<ProgressAction, int> ProgressChanged;

        public void UpdateProgress(ProgressAction action, int progress)
        {
            if (this.action != action || this.progress != progress)
            {
                this.action = action;
                this.progress = progress;
                ProgressChanged?.Invoke(action, progress);
            }
        }

        public void Dispose()
        {
            //foreach (var progressProvider in store.SourceRepository.Repositories.OfType<IProgressProvider>())
            //{
            //    progressProvider.ProgressAvailable -= OnProgressAvailable;
            //}
        }

        //private void OnProgressAvailable(object sender, ProgressEventArgs e)
        //{
        //    if (version == null)
        //        return;
        //
        //    if (e.Operation == null || e.Operation.Contains(version))
        //    {
        //        var percentComplete = e.PercentComplete;
        //        if (progress != percentComplete)
        //        {
        //            progress = percentComplete;
        //            // seems like NuGet is only returning download progress so far
        //            ProgressChanged?.Invoke(ProgressAction.Download, progress);
        //        }
        //    }         
        //}
    }
}
