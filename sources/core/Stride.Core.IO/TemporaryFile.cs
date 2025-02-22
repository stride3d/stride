// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;

namespace Stride.Core.IO;

public partial class TemporaryFile : IDisposable
{
    private bool isDisposed;

    public TemporaryFile()
    {
        Path = VirtualFileSystem.GetTempFileName();
    }

    public string Path { get; }

    ~TemporaryFile()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (isDisposed) return;

        isDisposed = true;
        if (disposing)
        {
            TryDelete();
        }
    }

    private void TryDelete()
    {
        try
        {
            VirtualFileSystem.FileDelete(Path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
