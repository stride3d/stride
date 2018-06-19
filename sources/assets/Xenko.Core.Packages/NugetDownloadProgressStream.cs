using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Xenko.Core.Packages
{
    class NugetDownloadProgressStream : Stream
    {
        private Stream innerStream;
        private long contentPosition;
        private long contentLength;
        private INugetDownloadProgress downloadProgress;

        public NugetDownloadProgressStream(Stream innerStream, long contentLength, INugetDownloadProgress downloadProgress)
        {
            this.innerStream = innerStream;
            this.contentLength = contentLength;
            this.downloadProgress = downloadProgress;
        }

        protected override void Dispose(bool disposing)
        {
            innerStream.Dispose();
        }

        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override bool CanSeek => false;

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                return ReadAsync(buffer, offset, count, CancellationToken.None).Result;
            }
            catch (AggregateException e)
            {
                throw e.InnerException;
            }
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            var result = await innerStream.ReadAsync(buffer, offset, count);
            contentPosition += result;
            downloadProgress.DownloadProgress(contentPosition, contentLength);

            return result;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
