namespace Stride.Core.Packages;

interface INugetDownloadProgress
{
    /// <summary>Additional bytes were read from an in-flight package download.</summary>
    void DownloadAdvanced(long bytesRead);
}
