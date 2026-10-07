// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Packages;

namespace Stride.Core.Assets.Templates;

/// <summary>A template package being downloaded, or one whose download failed.</summary>
/// <param name="PackageId">The package.</param>
/// <param name="DownloadedBytes">Bytes downloaded so far.</param>
/// <param name="TotalBytes">Size of the download, 0 until it starts.</param>
/// <param name="Index">Position of this package in its batch, from 1.</param>
/// <param name="Count">Number of packages in the batch.</param>
/// <param name="Failed">Whether the download failed (the package is not available).</param>
public sealed record TemplateDownload(string PackageId, long DownloadedBytes, long TotalBytes = 0, int Index = 1, int Count = 1, bool Failed = false)
{
    /// <summary>Progress from 0 to 1, or null while the size is not known.</summary>
    public double? Fraction => TotalBytes > 0 ? Math.Clamp((double)DownloadedBytes / TotalBytes, 0, 1) : null;
}

/// <summary>
/// The template package downloads in progress, for the windows that list templates. The template sources report here
/// (<see cref="Report"/>); windows show <see cref="Current"/> and refresh on <see cref="Changed"/>.
/// </summary>
public static class TemplateDownloads
{
    private static readonly object ThisLock = new();
    private static TemplateDownload? current;

    /// <summary>The download in progress (or the last failed one), null when there is none.</summary>
    public static TemplateDownload? Current
    {
        get { lock (ThisLock) return current; }
    }

    /// <summary>Raised when <see cref="Current"/> changes, on the reporting thread (possibly a background one).</summary>
    public static event Action? Changed;

    /// <summary>Sets <see cref="Current"/>: a download's progress, its failure, or null once it is done.</summary>
    public static void Report(TemplateDownload? download)
    {
        lock (ThisLock)
        {
            if (Equals(current, download))
                return;
            current = download;
        }
        Changed?.Invoke();
    }

    /// <summary>A short status line for <paramref name="download"/>, e.g. "Downloading Stride.Templates.Samples (2/2): 45.2 / 131.9 MB".</summary>
    public static string Describe(TemplateDownload download)
    {
        if (download.Failed)
            return $"Could not download {download.PackageId}. Check the network connection, then restart Game Studio.";
        var package = download.Count > 1 ? $"{download.PackageId} ({download.Index}/{download.Count})" : download.PackageId;
        return download.DownloadedBytes > 0
            ? $"Downloading {package}: {DownloadSize.Format(download.DownloadedBytes, download.TotalBytes)}"
            : $"Downloading {package}...";
    }
}
