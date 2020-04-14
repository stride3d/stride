// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;

namespace Stride.Core.IO
{
    public partial class TemporaryFile : IDisposable
    {
        private bool isDisposed;
        private string path;

        public TemporaryFile()
        {
            path = VirtualFileSystem.GetTempFileName();
        }

        public string Path
        {
            get { return path; }
        }

#if !NETFX_CORE
        ~TemporaryFile()
        {
            Dispose(false);
        }
#endif

        public void Dispose()
        {
            Dispose(false);
#if !NETFX_CORE
            GC.SuppressFinalize(this);
#endif
        }

        private void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
                TryDelete();
            }
        }

        private void TryDelete()
        {
            try
            {
                VirtualFileSystem.FileDelete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
