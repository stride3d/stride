namespace Stride.Core.Packages
{
    interface INugetDownloadProgress
    {
        void DownloadProgress(long contentPosition, long contentLength);
    }
}
