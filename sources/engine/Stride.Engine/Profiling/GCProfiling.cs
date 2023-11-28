// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Diagnostics;

namespace Stride.Profiling
{
    public class GcProfiling : IDisposable
    {
        private const string CollectionCountMessage = "Garbage collections> Gen0: {0}, Gen1: {1}, Gen2: {2}";
        private const string BeginMemoryMessage = "Allocated memory> Total: {0:0.00}MB";
        private const string EndMemoryMessage = "Allocated memory> Total: {0:0.00}MB Peak: {1:0.00}MB";
        private const string MarkMemoryMessage = "Allocated memory> Total: {0:0.00}MB Peak: {1:0.00}MB Allocations: {2:0.00}KB";

        private const float kB = 1 << 10;
        private const float MB = 1 << 20;

        public static ProfilingKey GcCollectionCountKey = new ProfilingKey("GC Collection Count");
        public static ProfilingKey GcMemoryKey = new ProfilingKey("GC Memory");

        private ProfilingState collectionCountState;
        private int gen0Count;
        private int gen1Count;
        private int gen2Count;

        private ProfilingState gcMemoryState;
        private long lastFrameMemory;
        private long memoryPeak;

        public GcProfiling()
        {
            collectionCountState = Profiler.New(GcCollectionCountKey);
            gen0Count = GC.CollectionCount(0);
            gen1Count = GC.CollectionCount(1);
            gen2Count = GC.CollectionCount(2);
            collectionCountState.Begin(CollectionCountMessage, gen0Count, gen1Count, gen2Count);

            gcMemoryState = Profiler.New(GcMemoryKey);
            memoryPeak = lastFrameMemory = GC.GetTotalMemory(false);
            gcMemoryState.Begin(BeginMemoryMessage, lastFrameMemory / MB);
        }

        public void Tick()
        {
            //memory
            var totalMem = GC.GetTotalMemory(false);
            memoryPeak = Math.Max(totalMem, memoryPeak);
            var diff = totalMem - lastFrameMemory;
            if (Math.Abs(diff) > 0)
            {
                gcMemoryState.Mark(MarkMemoryMessage, totalMem / MB, memoryPeak / MB, diff / kB);
                lastFrameMemory = totalMem;
            }

            //gens collections
            var gen0 = GC.CollectionCount(0);
            var gen1 = GC.CollectionCount(1);
            var gen2 = GC.CollectionCount(2);
            if (gen0 != gen0Count || gen1 != gen1Count || gen2 != gen2Count)
            {
                gen0Count = gen0;
                gen1Count = gen1;
                gen2Count = gen2;
                collectionCountState.Mark(CollectionCountMessage, gen0Count, gen1Count, gen2Count);
            }
        }

        public void Dispose()
        {
            //memory
            var totalMem = GC.GetTotalMemory(false);
            memoryPeak = Math.Max(totalMem, memoryPeak);
            gcMemoryState.End(EndMemoryMessage, totalMem / MB, memoryPeak / MB);

            //gens count
            gen0Count = GC.CollectionCount(0);
            gen1Count = GC.CollectionCount(1);
            gen2Count = GC.CollectionCount(2);
            collectionCountState.End(CollectionCountMessage, gen0Count, gen1Count, gen2Count);
        }

        public void Enable()
        {
            Profiler.Enable(GcCollectionCountKey);
            Profiler.Enable(GcMemoryKey);
            gcMemoryState.CheckIfEnabled();
            collectionCountState.CheckIfEnabled();
        }

        public void Disable()
        {
            Profiler.Disable(GcCollectionCountKey);
            Profiler.Disable(GcMemoryKey);
            gcMemoryState.CheckIfEnabled();
            collectionCountState.CheckIfEnabled();
        }
    }
}
