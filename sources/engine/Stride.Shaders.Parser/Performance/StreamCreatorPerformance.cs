// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using Stride.Core.Diagnostics;

namespace Stride.Shaders.Parser.Performance
{
    public static class StreamCreatorPerformance
    {
        internal static Logger Logger = GlobalLogger.GetLogger("StrideShaderPerformance"); // Global logger for shader profiling

        private static Stopwatch Global = new Stopwatch();
        private static Stopwatch StreamAnalyzer = new Stopwatch();
        private static Stopwatch FindEntryPoint = new Stopwatch();
        private static Stopwatch StreamAnalysisPerShader = new Stopwatch();
        private static Stopwatch BubbleUpStreamUsages = new Stopwatch();
        private static Stopwatch ComputeShaderStreamAnalysis = new Stopwatch();
        private static Stopwatch TagCleaner = new Stopwatch();
        private static Stopwatch GenerateStreams = new Stopwatch();
        private static Stopwatch RemoveUselessAndSortMethods = new Stopwatch();
        private static Stopwatch PropagateStreamsParameter = new Stopwatch();
        private static Stopwatch TransformStreamsAssignments = new Stopwatch();
        private static Stopwatch AssignSearch = new Stopwatch();
        private static Stopwatch CreateOutputFromStream = new Stopwatch();
        private static Stopwatch CreateStreamFromInput = new Stopwatch();
        private static Stopwatch StreamFieldVisitor = new Stopwatch();
        private static Stopwatch StreamFieldVisitorClone = new Stopwatch();
        
        private static int StreamFieldVisitorCount;

        public static void Start(StreamCreatorStage stage)
        {
            switch (stage)
            {
                case StreamCreatorStage.Global:
                    Global.Start();
                    break;
                case StreamCreatorStage.StreamAnalyzer:
                    StreamAnalyzer.Start();
                    break;
                case StreamCreatorStage.FindEntryPoint:
                    FindEntryPoint.Start();
                    break;
                case StreamCreatorStage.StreamAnalysisPerShader:
                    StreamAnalysisPerShader.Start();
                    break;
                case StreamCreatorStage.BubbleUpStreamUsages:
                    BubbleUpStreamUsages.Start();
                    break;
                case StreamCreatorStage.ComputeShaderStreamAnalysis:
                    ComputeShaderStreamAnalysis.Start();
                    break;
                case StreamCreatorStage.TagCleaner:
                    TagCleaner.Start();
                    break;
                case StreamCreatorStage.GenerateStreams:
                    GenerateStreams.Start();
                    break;
                case StreamCreatorStage.RemoveUselessAndSortMethods:
                    RemoveUselessAndSortMethods.Start();
                    break;
                case StreamCreatorStage.PropagateStreamsParameter:
                    PropagateStreamsParameter.Start();
                    break;
                case StreamCreatorStage.TransformStreamsAssignments:
                    TransformStreamsAssignments.Start();
                    break;
                case StreamCreatorStage.AssignSearch:
                    AssignSearch.Start();
                    break;
                case StreamCreatorStage.CreateOutputFromStream:
                    CreateOutputFromStream.Start();
                    break;
                case StreamCreatorStage.CreateStreamFromInput:
                    CreateStreamFromInput.Start();
                    break;
                case StreamCreatorStage.StreamFieldVisitor:
                    StreamFieldVisitor.Start();
                    ++StreamFieldVisitorCount;
                    break;
                case StreamCreatorStage.StreamFieldVisitorClone:
                    StreamFieldVisitorClone.Start();
                    break;
            }
        }

        public static void Pause(StreamCreatorStage stage)
        {
            switch (stage)
            {
                case StreamCreatorStage.Global:
                    Global.Stop();
                    break;
                case StreamCreatorStage.StreamAnalyzer:
                    StreamAnalyzer.Stop();
                    break;
                case StreamCreatorStage.FindEntryPoint:
                    FindEntryPoint.Stop();
                    break;
                case StreamCreatorStage.StreamAnalysisPerShader:
                    StreamAnalysisPerShader.Stop();
                    break;
                case StreamCreatorStage.BubbleUpStreamUsages:
                    BubbleUpStreamUsages.Stop();
                    break;
                case StreamCreatorStage.ComputeShaderStreamAnalysis:
                    ComputeShaderStreamAnalysis.Stop();
                    break;
                case StreamCreatorStage.TagCleaner:
                    TagCleaner.Stop();
                    break;
                case StreamCreatorStage.GenerateStreams:
                    GenerateStreams.Stop();
                    break;
                case StreamCreatorStage.RemoveUselessAndSortMethods:
                    RemoveUselessAndSortMethods.Stop();
                    break;
                case StreamCreatorStage.PropagateStreamsParameter:
                    PropagateStreamsParameter.Stop();
                    break;
                case StreamCreatorStage.TransformStreamsAssignments:
                    TransformStreamsAssignments.Stop();
                    break;
                case StreamCreatorStage.AssignSearch:
                    AssignSearch.Stop();
                    break;
                case StreamCreatorStage.CreateOutputFromStream:
                    CreateOutputFromStream.Stop();
                    break;
                case StreamCreatorStage.CreateStreamFromInput:
                    CreateStreamFromInput.Stop();
                    break;
                case StreamCreatorStage.StreamFieldVisitor:
                    StreamFieldVisitor.Stop();
                    break;
                case StreamCreatorStage.StreamFieldVisitorClone:
                    StreamFieldVisitorClone.Stop();
                    break;
            }
        }

        public static void Reset()
        {
            Global.Reset();
            StreamAnalyzer.Reset();
            FindEntryPoint.Reset();
            StreamAnalysisPerShader.Reset();
            BubbleUpStreamUsages.Reset();
            ComputeShaderStreamAnalysis.Reset();
            TagCleaner.Reset();
            GenerateStreams.Reset();
            RemoveUselessAndSortMethods.Reset();
            PropagateStreamsParameter.Reset();
            TransformStreamsAssignments.Reset();
            AssignSearch.Reset();
            CreateOutputFromStream.Reset();
            CreateStreamFromInput.Reset();
            StreamFieldVisitor.Reset();
            StreamFieldVisitorClone.Reset();

            StreamFieldVisitorCount = 0;
        }

        public static void PrintResult()
        {
            Logger.Info(@"----------------------------STREAM CREATOR ANALYZER-----------------------------");
            Logger.Info($"Stream creation took {Global.ElapsedMilliseconds} ms");
            Logger.Info($"StreamAnalyzer took {StreamAnalyzer.ElapsedMilliseconds} ms");
            Logger.Info($"FindEntryPoint took {FindEntryPoint.ElapsedMilliseconds} ms");
            Logger.Info($"StreamAnalysisPerShader took {StreamAnalysisPerShader.ElapsedMilliseconds} ms");
            Logger.Info($"BubbleUpStreamUsages took {BubbleUpStreamUsages.ElapsedMilliseconds} ms");
            Logger.Info($"ComputeShaderStreamAnalysis took {ComputeShaderStreamAnalysis.ElapsedMilliseconds} ms");
            Logger.Info($"TagCleaner took {TagCleaner.ElapsedMilliseconds} ms");
            Logger.Info($"GenerateStreams took {GenerateStreams.ElapsedMilliseconds} ms");
            Logger.Info($"RemoveUselessAndSortMethods took {RemoveUselessAndSortMethods.ElapsedMilliseconds} ms");
            Logger.Info($"PropagateStreamsParameter took {PropagateStreamsParameter.ElapsedMilliseconds} ms");
            Logger.Info($"TransformStreamsAssignments took {TransformStreamsAssignments.ElapsedMilliseconds} ms");
            Logger.Info($"AssignSearch took {AssignSearch.ElapsedMilliseconds} ms");
            Logger.Info($"CreateOutputFromStream took {CreateOutputFromStream.ElapsedMilliseconds} ms");
            Logger.Info($"CreateStreamFromInput took {CreateStreamFromInput.ElapsedMilliseconds} ms");
            Logger.Info($"StreamFieldVisitor took {StreamFieldVisitor.ElapsedMilliseconds} ms for {StreamFieldVisitorCount} calls");
            Logger.Info($"StreamFieldVisitorClone took {StreamFieldVisitorClone.ElapsedMilliseconds} ms");
            Logger.Info(@"-------------------------------------------------------------------------------");
        }
    }

    public enum StreamCreatorStage
    {
        Global,
        StreamAnalyzer,
        FindEntryPoint,
        StreamAnalysisPerShader,
        BubbleUpStreamUsages,
        ComputeShaderStreamAnalysis,
        TagCleaner,
        GenerateStreams,
        RemoveUselessAndSortMethods,
        PropagateStreamsParameter,
        TransformStreamsAssignments,
        AssignSearch,
        CreateOutputFromStream,
        CreateStreamFromInput,
        StreamFieldVisitor,
        StreamFieldVisitorClone
    }
}
