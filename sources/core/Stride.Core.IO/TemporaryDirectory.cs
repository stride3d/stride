// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_DESKTOP
using System.Diagnostics;

namespace Stride.Core.IO;

public class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
        : this(Guid.NewGuid().ToString()[..8])
    {
    }

    public TemporaryDirectory(string path)
    {
        DirectoryPath = Path.GetFullPath(path);

        if (Directory.Exists(DirectoryPath))
        {
            throw new InvalidOperationException($"Directory {path} already exists.");
        }

        Directory.CreateDirectory(this.DirectoryPath);
    }

    public string DirectoryPath { get; }

    public void Dispose()
    {
        DeleteDirectory(DirectoryPath);
        GC.SuppressFinalize(this);
    }

    public static void DeleteDirectory(string directoryPath)
    {
        // From http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true/329502#329502

        if (!Directory.Exists(directoryPath))
        {
            Trace.WriteLine($"Directory '{directoryPath}' is missing and can't be removed.");

            return;
        }

        string[] files = Directory.GetFiles(directoryPath);
        string[] dirs = Directory.GetDirectories(directoryPath);

        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (string dir in dirs)
        {
            DeleteDirectory(dir);
        }

        try
        {
            File.SetAttributes(directoryPath, FileAttributes.Normal);
            Directory.Delete(directoryPath, false);
        }
        catch (IOException)
        {
            Trace.WriteLine(string.Format(
                "{0}The directory '{1}' could not be deleted!" +
                "{0}Most of the time, this is due to an external process accessing the files in the temporary repositories created during the test runs, and keeping a handle on the directory, thus preventing the deletion of those files." +
                "{0}Known and common causes include:" +
                "{0}- Windows Search Indexer (go to the Indexing Options, in the Windows Control Panel, and exclude the bin folder of LibGit2Sharp.Tests)" +
                "{0}- Antivirus (exclude the bin folder of LibGit2Sharp.Tests from the paths scanned by your real-time antivirus){0}",
                Environment.NewLine,
                Path.GetFullPath(directoryPath)));
        }
    }
}
#endif
