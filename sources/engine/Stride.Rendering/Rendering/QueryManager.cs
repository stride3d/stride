// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Rendering
{
    public class QueryManager : IDisposable
    {
        private struct QueryEvent
        {
            public QueryPool Pool;

            public int Index;

            public ProfilingKey ProfilingKey;
        }

        private const int TimestampQueryPoolCapacity = 64;

        private readonly CommandList commandList;
        private readonly GraphicsResourceAllocator allocator;      
        private readonly long[] queryResults = new long[TimestampQueryPoolCapacity];
        private readonly Queue<QueryEvent> queryEvents = new Queue<QueryEvent>();
        private readonly Stack<QueryEvent> queries = new Stack<QueryEvent>();
        private readonly Stack<ProfilingState> profilingStates = new Stack<ProfilingState>();

        private QueryPool currentQueryPool;
        private int currentQueryIndex;

        public QueryManager(CommandList commandList, GraphicsResourceAllocator allocator)
        {
            this.commandList = commandList;
            this.allocator = allocator;

            Profiler.GpuTimestampFrequencyRatio = commandList.GraphicsDevice.TimestampFrequency / 1000.0;
        }

        /// <summary>
        /// Begins profile.
        /// </summary>
        /// <param name="profileColor">The profile event color.</param>
        /// <param name="profilingKey">The <see cref="ProfilingKey"/></param>
        public Scope BeginProfile(Color4 profileColor, ProfilingKey profilingKey)
        {
            if (!Profiler.IsEnabled(profilingKey))
            {
                return new Scope(this, profilingKey);
            }

            EnsureQueryPoolSize();

            // Push the current query range onto the stack 
            var query = new QueryEvent
            {
                ProfilingKey = profilingKey,
                Pool = currentQueryPool,
                Index = currentQueryIndex++,
            };
            queries.Push(query);

            // Query the timestamp at the beginning of the range
            commandList.WriteTimestamp(currentQueryPool, query.Index);

            // Add the queries to the list of queries to proceess
            queryEvents.Enqueue(query);

            // Sets a debug marker if debug mode is enabled
            if (commandList.GraphicsDevice.IsDebugMode)
            {
                commandList.BeginProfile(profileColor, profilingKey.Name);
            }

            return new Scope(this, profilingKey);
        }

        /// <summary>
        /// Ends profile.
        /// </summary>
        public void EndProfile(ProfilingKey profilingKey)
        {
            if (!Profiler.IsEnabled(profilingKey))
            {
                return;
            }

            if (queries.Count == 0)
            {
                throw new InvalidOperationException();
            }

            EnsureQueryPoolSize();

            // Get the current query
            var query = queries.Pop();
            query.Pool = currentQueryPool;
            query.Index = currentQueryIndex++;
            query.ProfilingKey = null;

            // Query the timestamp at the end of the range
            commandList.WriteTimestamp(query.Pool, query.Index);

            // Add the queries to the list of queries to proceess
            queryEvents.Enqueue(query);

            // End the debug marker
            if (commandList.GraphicsDevice.IsDebugMode)
            {
                commandList.EndProfile();
            }
        }

        public void Flush()
        {
            QueryPool pool = null;

            while (queryEvents.Count > 0)
            {
                var query = queryEvents.Peek();

                // If the query is allocated from a new pool, read back it's data
                if (query.Pool != pool)
                {
                    // Don't read back the pool we are currently recording to
                    if (query.Pool == currentQueryPool)
                        return;

                    // If the pool is not ready yet, wait until next time
                    if (!query.Pool.TryGetData(queryResults))
                        return;

                    // Recycle the pool
                    pool = query.Pool;
                    allocator.ReleaseReference(pool);
                }

                // Remove successful queries
                queryEvents.Dequeue();

                // Profile
                // An event with a key is a begin event
                if (query.ProfilingKey != null)
                {
                    var profilingState = Profiler.New(query.ProfilingKey);
                    profilingState.Begin(queryResults[query.Index]);
                    profilingStates.Push(profilingState);
                }
                else
                {
                    var profilingState = profilingStates.Pop();
                    profilingState.End(queryResults[query.Index]);
                }
            }
        }

        public void Dispose()
        {
            QueryPool pool = null;

            while (queryEvents.Count > 0)
            {
                var query = queryEvents.Dequeue();
                if (query.Pool != pool)
                {
                    pool = query.Pool;
                    allocator.ReleaseReference(pool);
                }
            }

            if (currentQueryPool != pool)
            {
                allocator.ReleaseReference(currentQueryPool);
            }

            currentQueryPool = null;
        }

        private void EnsureQueryPoolSize()
        {
            // Allocate one timestamp query
            if (currentQueryPool == null || currentQueryIndex >= currentQueryPool.QueryCount)
            {
                currentQueryPool = allocator.GetQueryPool(QueryType.Timestamp, TimestampQueryPoolCapacity);
                currentQueryIndex = 0;
            }
        }

        public struct Scope : IDisposable
        {
            private readonly QueryManager queryManager;
            private readonly ProfilingKey profilingKey;

            public Scope(QueryManager queryManager, ProfilingKey profilingKey)
            {
                this.queryManager = queryManager;
                this.profilingKey = profilingKey;
            }

            public void Dispose()
            {
                queryManager.EndProfile(profilingKey);
            }
        }
    }
}
