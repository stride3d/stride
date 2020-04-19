// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Shaders.Parser.Performance
{
    public static class PerformanceLogger
    {
        internal static Logger Logger = GlobalLogger.GetLogger("StrideShaderPerformance"); // Global logger for shader profiling

        private static int globalCount;
        private static int loadingCount;
        private static int typeAnalysisCount;
        private static int semanticAnalysisCount;
        private static int mixCount;
        private static int deepCloneCount;
        private static int astParsingCount;

        private static readonly List<long> GlobalTimes = new List<long>();
        private static readonly List<long> LoadingTimes = new List<long>();
        private static readonly List<long> TypeAnalysisTimes = new List<long>();
        private static readonly List<long> SemanticAnalysisTimes = new List<long>();
        private static readonly List<long> MixTimes = new List<long>();
        private static readonly List<long> DeepcloneTimes = new List<long>();
        private static readonly List<long> AstParsingTimes = new List<long>();
        
        private static Stopwatch globalWatch = new Stopwatch();
        private static Stopwatch loadingWatch = new Stopwatch();
        private static Stopwatch typeAnalysisWatch = new Stopwatch();
        private static Stopwatch semanticAnalysisWatch = new Stopwatch();
        private static Stopwatch mixWatch = new Stopwatch();
        private static Stopwatch deepCloneWatch = new Stopwatch();
        private static Stopwatch astParsingWatch = new Stopwatch();

        public static void Start(PerformanceStage stage)
        {
            switch (stage)
            {
                case PerformanceStage.Global:
                    globalWatch.Start();
                    break;
                case PerformanceStage.Loading:
                    loadingWatch.Start();
                    break;
                case PerformanceStage.TypeAnalysis:
                    typeAnalysisWatch.Start();
                    break;
                case PerformanceStage.SemanticAnalysis:
                    semanticAnalysisWatch.Start();
                    break;
                case PerformanceStage.Mix:
                    mixWatch.Start();
                    break;
                case PerformanceStage.DeepClone:
                    deepCloneWatch.Start();
                    break;
                case PerformanceStage.AstParsing:
                    astParsingWatch.Start();
                    break;
            }
        }

        public static void Pause(PerformanceStage stage)
        {
            switch (stage)
            {
                case PerformanceStage.Global:
                    globalWatch.Stop();
                    break;
                case PerformanceStage.Loading:
                    loadingWatch.Stop();
                    break;
                case PerformanceStage.TypeAnalysis:
                    typeAnalysisWatch.Stop();
                    break;
                case PerformanceStage.SemanticAnalysis:
                    semanticAnalysisWatch.Stop();
                    break;
                case PerformanceStage.Mix:
                    mixWatch.Stop();
                    break;
                case PerformanceStage.DeepClone:
                    deepCloneWatch.Stop();
                    break;
                case PerformanceStage.AstParsing:
                    astParsingWatch.Stop();
                    break;
            }
        }

        public static void Stop(PerformanceStage stage)
        {
            switch (stage)
            {
                case PerformanceStage.Global:
                    globalWatch.Stop();
                    GlobalTimes.Add(globalWatch.ElapsedMilliseconds);
                    ++globalCount;
                    break;
                case PerformanceStage.Loading:
                    loadingWatch.Stop();
                    LoadingTimes.Add(loadingWatch.ElapsedMilliseconds);
                    ++loadingCount;
                    break;
                case PerformanceStage.TypeAnalysis:
                    typeAnalysisWatch.Stop();
                    TypeAnalysisTimes.Add(typeAnalysisWatch.ElapsedMilliseconds);
                    ++typeAnalysisCount;
                    break;
                case PerformanceStage.SemanticAnalysis:
                    semanticAnalysisWatch.Stop();
                    SemanticAnalysisTimes.Add(semanticAnalysisWatch.ElapsedMilliseconds);
                    ++semanticAnalysisCount;
                    break;
                case PerformanceStage.Mix:
                    mixWatch.Stop();
                    MixTimes.Add(mixWatch.ElapsedMilliseconds);
                    ++mixCount;
                    break;
                case PerformanceStage.DeepClone:
                    deepCloneWatch.Stop();
                    DeepcloneTimes.Add(deepCloneWatch.ElapsedMilliseconds);
                    ++deepCloneCount;
                    break;
                case PerformanceStage.AstParsing:
                    astParsingWatch.Stop();
                    AstParsingTimes.Add(astParsingWatch.ElapsedMilliseconds);
                    ++astParsingCount;
                    break;
            }
        }

        public static void Reset()
        {
            globalWatch.Reset();
            loadingWatch.Reset();
            typeAnalysisWatch.Reset();
            semanticAnalysisWatch.Reset();
            mixWatch.Reset();
            deepCloneWatch.Reset();
            astParsingWatch.Reset();
        }

        public static void PrintResult()
        {
            Logger.Info(@"--------------------------TOTAL PERFORMANCE ANALYZER---------------------------");
            Logger.Info($"Loading took {LoadingTimes.Sum()} ms for {loadingCount} shader(s)");
            Logger.Info($"Type analysis took {TypeAnalysisTimes.Sum()} ms for {typeAnalysisCount} shader(s)");
            Logger.Info($"Semantic analysis took {SemanticAnalysisTimes.Sum()} ms for {semanticAnalysisCount} shader(s)");
            Logger.Info($"Mix took {MixTimes.Sum()} ms for {mixCount} shader(s)");
            Logger.Info($"DeepClone took {DeepcloneTimes.Sum()} ms for {deepCloneCount} shader(s)");
            Logger.Info($"Ast parsing took {AstParsingTimes.Sum()} ms for {astParsingCount} shader(s)");
            Logger.Info(@"-------------------------------------------------------------------------------");

        }
        public static void PrintLastResult()
        {

            Logger.Info(@"--------------------------LAST PERFORMANCE ANALYZER---------------------------");
            Logger.Info($"Process took {globalWatch.ElapsedMilliseconds} ms");
            Logger.Info($"Loading took {loadingWatch.ElapsedMilliseconds} ms");
            Logger.Info($"Type analysis took {typeAnalysisWatch.ElapsedMilliseconds} ms");
            Logger.Info($"Semantic analysis took {semanticAnalysisWatch.ElapsedMilliseconds} ms");
            Logger.Info($"Mix took {mixWatch.ElapsedMilliseconds} ms");
            Logger.Info($"DeepClone took {deepCloneWatch.ElapsedMilliseconds} ms");
            Logger.Info($"Ast parsing took {astParsingWatch.ElapsedMilliseconds} ms");
            Logger.Info(@"------------------------------------------------------------------------------");

        }


        public static void WriteOut(int limit)
        {
            if (globalCount == limit)
            {
                PrintResult();
                TextWriter tw = new StreamWriter(VirtualFileSystem.ApplicationLocal.OpenStream("performance.csv", VirtualFileMode.Append, VirtualFileAccess.Write));
                tw.WriteLine("loading,type,semantic,mix,deepclone,global");

                for (var i = 0; i < limit; ++i)
                {
                    tw.WriteLine("{0},{1},{2},{3},{4},{5}", LoadingTimes[i], TypeAnalysisTimes[i], SemanticAnalysisTimes[i], MixTimes[i], DeepcloneTimes[i], GlobalTimes[i]);
                }
                tw.Dispose();
            }
        }
    }

    public enum PerformanceStage
    {
        Global,
        Loading,
        TypeAnalysis,
        SemanticAnalysis,
        Mix,
        DeepClone,
        AstParsing
    }
}
