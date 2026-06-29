// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Net;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Stride.Core.Packages;

// Injects a delegating handler into NuGet's V3 HTTP stack so .nupkg downloads report byte progress
// through INugetDownloadProgress (aggregated by NugetStore). Registered ahead of the default
// HttpHandlerResourceV3Provider in NugetSourceRepositoryProvider.
internal sealed class DownloadProgressHandlerProvider : ResourceProvider
{
    private readonly INugetDownloadProgress progress;

    public DownloadProgressHandlerProvider(INugetDownloadProgress progress)
        : base(typeof(HttpHandlerResource), nameof(DownloadProgressHandlerProvider), nameof(HttpHandlerResourceV3Provider))
    {
        this.progress = progress;
    }

    public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository source, CancellationToken token)
    {
        var result = await new HttpHandlerResourceV3Provider().TryCreate(source, token);
        if (result.Item1 && result.Item2 is HttpHandlerResourceV3 inner)
        {
            var handler = new DownloadProgressMessageHandler(inner.MessageHandler, progress);
            return new Tuple<bool, INuGetResource>(true, new HttpHandlerResourceV3(inner.ClientHandler, handler));
        }
        return result;
    }
}

// Wraps each successful .nupkg response body so bytes are counted as NuGet streams them to disk.
internal sealed class DownloadProgressMessageHandler : DelegatingHandler
{
    private readonly INugetDownloadProgress progress;

    public DownloadProgressMessageHandler(HttpMessageHandler innerHandler, INugetDownloadProgress progress)
        : base(innerHandler)
    {
        this.progress = progress;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        if (request.Method == HttpMethod.Get
            && response.IsSuccessStatusCode
            && request.RequestUri?.AbsolutePath.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase) == true
            && response.Content is { } content
            && content.Headers.ContentLength is > 0)
        {
            response.Content = new ProgressHttpContent(content, content.Headers.ContentLength.Value, progress);
        }
        return response;
    }
}

internal sealed class ProgressHttpContent : HttpContent
{
    private readonly HttpContent inner;
    private readonly long length;
    private readonly INugetDownloadProgress progress;

    public ProgressHttpContent(HttpContent inner, long length, INugetDownloadProgress progress)
    {
        this.inner = inner;
        this.length = length;
        this.progress = progress;
        foreach (var header in inner.Headers)
            Headers.TryAddWithoutValidation(header.Key, header.Value);
    }

    // NuGet downloads via ReadAsStreamAsync; count bytes as the stream is consumed.
    protected override async Task<Stream> CreateContentReadStreamAsync()
        => new ProgressReadStream(await inner.ReadAsStreamAsync(), length, progress);

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        using var source = await inner.ReadAsStreamAsync();
        var buffer = new byte[81920];
        int read;
        while ((read = await source.ReadAsync(buffer)) > 0)
        {
            await stream.WriteAsync(buffer.AsMemory(0, read));
            progress.DownloadAdvanced(read);
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        length = this.length;
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            inner.Dispose();
        base.Dispose(disposing);
    }
}

internal sealed class ProgressReadStream : Stream
{
    private readonly Stream inner;
    private readonly long length;
    private readonly INugetDownloadProgress progress;

    public ProgressReadStream(Stream inner, long length, INugetDownloadProgress progress)
    {
        this.inner = inner;
        this.length = length;
        this.progress = progress;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = inner.Read(buffer, offset, count);
        if (read > 0)
            progress.DownloadAdvanced(read);
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await inner.ReadAsync(buffer, cancellationToken);
        if (read > 0)
            progress.DownloadAdvanced(read);
        return read;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => length;
    public override long Position { get => inner.Position; set => throw new NotSupportedException(); }
    public override void Flush() => inner.Flush();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            inner.Dispose();
        base.Dispose(disposing);
    }
}
