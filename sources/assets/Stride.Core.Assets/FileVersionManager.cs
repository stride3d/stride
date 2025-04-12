// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Storage;

namespace Stride.Core.Assets;

public class FileVersionManager
{
    private static readonly object TrackerLock = new();
    private static FileVersionManager? instance;
    private readonly FileVersionTracker tracker;
    private readonly Thread asyncRunner;
    private readonly AutoResetEvent asyncRequestAvailable;
    private readonly ConcurrentQueue<AsyncRequest> asyncRequests;
    private readonly HashSet<AsyncRequest> requestsToProcess = [];
    private bool isDisposing;
    private bool isDisposed;
    private long requestsInFlight;

    private FileVersionManager()
    {
        // Environment.SpecialFolder.ApplicationData
        asyncRequestAvailable = new AutoResetEvent(false);
        asyncRequests = new ConcurrentQueue<AsyncRequest>();

        // Loads the file version cache
        tracker = FileVersionTracker.GetDefault();
        asyncRunner = new Thread(SafeAction.Wrap(ComputeFileHashAsyncRunner)) { Name = "File Version Manager", IsBackground = true };
        asyncRunner.Start();
    }

    /// <summary>
    /// Returns the amount of items scheduled left to process
    /// </summary>
    /// <remarks>
    /// It may already be out of date as soon as it returns.
    /// Do not rely on this to check for completion, use the callbacks instead.
    /// </remarks>
    public long PeekAsyncRequestsLeft
    {
        get
        {
            return Interlocked.Read(ref requestsInFlight);
        }
    }

    public static FileVersionManager Instance
    {
        get
        {
            lock (TrackerLock)
            {
                if (instance != null)
                    return instance;

                instance = new FileVersionManager();
                AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;
                return instance;
            }
        }
    }

    public static void Shutdown()
    {
        lock (TrackerLock)
        {
            if (instance == null)
                return;
            instance.Dispose();
            instance = null;
        }
    }

    public ObjectId ComputeFileHash(UFile path)
    {
        if (!File.Exists(path))
            return ObjectId.Empty;

        return tracker.ComputeFileHash(path);
    }

    public void ComputeFileHashAsync(UFile path, Action<UFile, ObjectId>? fileHashCallback = null, CancellationToken? cancellationToken = null)
    {
        ArgumentNullException.ThrowIfNull(path);

        lock (asyncRequests)
        {
            asyncRequests.Enqueue(new AsyncRequest(path, fileHashCallback, cancellationToken));
            Interlocked.Increment(ref requestsInFlight);
        }
        asyncRequestAvailable.Set();
    }

    public void ComputeFileHashAsync(IEnumerable<UFile> paths, Action<UFile, ObjectId>? fileHashCallback = null, CancellationToken? cancellationToken = null)
    {
        ArgumentNullException.ThrowIfNull(paths);

        int itemCount = 0;
        lock (asyncRequests)
        {
            foreach (var path in paths)
            {
                asyncRequests.Enqueue(new AsyncRequest(path, fileHashCallback, cancellationToken));
                itemCount++;
            }
        }
        Interlocked.Add(ref requestsInFlight, itemCount);
        asyncRequestAvailable.Set();
    }

    private void ComputeFileHashAsyncRunner()
    {
        while (!isDisposing)
        {
            if (asyncRequestAvailable.WaitOne())
            {
                lock (asyncRequests)
                {
                    // Dequeue as much as possible in a single row
                    while (asyncRequests.TryDequeue(out var asyncRequest))
                    {
                        requestsToProcess.Add(asyncRequest);
                    }
                }
            }

            // Early exit
            if (isDisposing)
            {
                return;
            }

            foreach (var request in requestsToProcess)
            {
                if (isDisposing)
                {
                    return;
                }

                if (request.CancellationToken.HasValue && request.CancellationToken.Value.IsCancellationRequested)
                {
                    continue;
                }

                var hash = ComputeFileHash(request.File);

                if (request.CancellationToken.HasValue && request.CancellationToken.Value.IsCancellationRequested)
                {
                    continue;
                }

                if (isDisposing)
                {
                    return;
                }

                request.FileHashCallback?.Invoke(request.File, hash);
            }
            Interlocked.Add(ref requestsInFlight, -requestsToProcess.Count);
            // Once we have processed the list, we can clear it
            requestsToProcess.Clear();
        }
    }

    private static void CurrentDomainProcessExit(object? sender, EventArgs e)
    {
        Shutdown();
    }

    private void Dispose()
    {
        if (isDisposed)
            return;

        // Set to true and let the async runner thread terminates
        isDisposing = true;
        asyncRequestAvailable.Set();

        asyncRunner.Join();

        isDisposed = true;
    }

    private readonly struct AsyncRequest : IEquatable<AsyncRequest>
    {
        public AsyncRequest(UFile file, Action<UFile, ObjectId>? fileHashCallback, CancellationToken? cancellationToken)
        {
            File = file;
            FileHashCallback = fileHashCallback;
            CancellationToken = cancellationToken;
        }

        public readonly UFile File;

        public readonly Action<UFile, ObjectId>? FileHashCallback;

        public readonly CancellationToken? CancellationToken;

        public readonly bool Equals(AsyncRequest other)
        {
            return Equals(File, other.File) &&
                   Equals(FileHashCallback, other.FileHashCallback) &&
                   CancellationToken.Equals(other.CancellationToken);
        }

        public override readonly bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is AsyncRequest asyncRequest && Equals(asyncRequest);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(File, FileHashCallback, CancellationToken);
        }

        public static bool operator ==(AsyncRequest left, AsyncRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AsyncRequest left, AsyncRequest right)
        {
            return !left.Equals(right);
        }
    }
}
