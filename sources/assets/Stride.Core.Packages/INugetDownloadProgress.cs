namespace Stride.Core.Packages;

interface INugetDownloadProgress
{
    /// <summary>A package download of <paramref name="length"/> bytes started.</summary>
    void DownloadStarted(long length);

    /// <summary>Additional bytes were read from an in-flight package download.</summary>
    void DownloadAdvanced(long bytesRead);

    /// <summary>An in-flight package download reached its end.</summary>
    void DownloadCompleted();
}
