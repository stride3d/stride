using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Xenko.Core.Packages
{
    class NugetHttpSourceWithDownloadProgressResourceProvider : HttpSourceResourceProvider
    {
        private INugetDownloadProgress downloadProgress;

        public NugetHttpSourceWithDownloadProgressResourceProvider(INugetDownloadProgress downloadProgress)
        {
            this.downloadProgress = downloadProgress;
        }

        public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository source, CancellationToken token)
        {
            var result = await base.TryCreate(source, token);
            if (result.Item1)
            {
                var httpSourceResource = (HttpSourceResource)result.Item2;
                httpSourceResource.HttpSource.RetryHandler = new NugetHttpRetryHandlerWithDownloadProgress(downloadProgress);
            }
            return result;
        }
    }
}
