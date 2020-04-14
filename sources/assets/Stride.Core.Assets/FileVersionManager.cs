// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Stride.Core.BuildEngine;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Storage;

namespace Stride.Core.Assets
{
    public class FileVersionManager
    {
        private static readonly object TrackerLock = new object();
        private static FileVersionManager instance;
        private readonly FileVersionTracker tracker;
        private readonly Thread asyncRunner;
        private readonly AutoResetEvent asyncRequestAvailable;
        private readonly ConcurrentQueue<AsyncRequest> asyncRequests;
        private bool isDisposing;
        private bool isDisposed;

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

        [NotNull]
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

        public void ComputeFileHashAsync([NotNull] UFile path, Action<UFile, ObjectId> fileHashCallback = null, CancellationToken? cancellationToken = null)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            lock (asyncRequests)
            {
                asyncRequests.Enqueue(new AsyncRequest(path, fileHashCallback, cancellationToken));
            }
            asyncRequestAvailable.Set();
        }

        public void ComputeFileHashAsync([NotNull] IEnumerable<UFile> paths, Action<UFile, ObjectId> fileHashCallback = null, CancellationToken? cancellationToken = null)
        {
            if (paths == null) throw new ArgumentNullException(nameof(paths));

            lock (asyncRequests)
            {
                foreach(var path in paths)
                    asyncRequests.Enqueue(new AsyncRequest(path, fileHashCallback, cancellationToken));
            }
            asyncRequestAvailable.Set();
        }

        private readonly HashSet<AsyncRequest> requestsToProcess = new HashSet<AsyncRequest>();

        private void ComputeFileHashAsyncRunner()
        {
            while (!isDisposing)
            {
                if (asyncRequestAvailable.WaitOne())
                {
                    lock (asyncRequests)
                    {
                        // Dequeue as much as possible in a single row
                        while (true)
                        {
                            AsyncRequest asyncRequest;
                            if (asyncRequests.TryDequeue(out asyncRequest))
                            {
                                requestsToProcess.Add(asyncRequest);
                            }
                            else
                            {
                                break;
                            }
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
                // Once we have processed the list, we can clear it
                requestsToProcess.Clear();
            }
        }

        private static void CurrentDomainProcessExit(object sender, EventArgs e)
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

        private struct AsyncRequest : IEquatable<AsyncRequest>
        {
            public AsyncRequest(UFile file, Action<UFile, ObjectId> fileHashCallback, CancellationToken? cancellationToken)
            {
                File = file;
                FileHashCallback = fileHashCallback;
                CancellationToken = cancellationToken;
            }

            public readonly UFile File;

            public readonly Action<UFile, ObjectId> FileHashCallback;

            public readonly CancellationToken? CancellationToken;

            public bool Equals(AsyncRequest other)
            {
                return Equals(File, other.File) &&
                       Equals(FileHashCallback, other.FileHashCallback) &&
                       CancellationToken.Equals(other.CancellationToken);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is AsyncRequest && Equals((AsyncRequest)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = File?.GetHashCode() ?? 0;
                    hashCode = (hashCode*397) ^ (FileHashCallback?.GetHashCode() ?? 0);
                    hashCode = (hashCode*397) ^ CancellationToken.GetHashCode();
                    return hashCode;
                }
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
}
