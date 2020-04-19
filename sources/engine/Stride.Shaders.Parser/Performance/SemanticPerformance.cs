// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using Stride.Core.Diagnostics;

namespace Stride.Shaders.Parser.Performance
{
    public static class SemanticPerformance
    {
        internal static Logger Logger = GlobalLogger.GetLogger("StrideShaderPerformance"); // Global logger for shader profiling

        private static Stopwatch TotalTime = new Stopwatch();

        private static Stopwatch VisitVariable = new Stopwatch();
        private static Stopwatch CommonVisit = new Stopwatch();
        private static Stopwatch FindDeclarationScope = new Stopwatch();
        private static Stopwatch FindDeclarationsFromObject = new Stopwatch();
        private static Stopwatch FindDeclarations = new Stopwatch();
        private static Stopwatch ProcessMethodInvocation = new Stopwatch();
        private static Stopwatch CheckNameConflict = new Stopwatch();
        private static Stopwatch HasExternQualifier = new Stopwatch();

        private static int VisitVariableCount = 0;
        private static int CommonVisitCount = 0;
        private static int FindDeclarationScopeCount = 0;
        private static int FindDeclarationsFromObjectCount = 0;
        private static int FindDeclarationsCount = 0;
        private static int ProcessMethodInvocationCount = 0;
        private static int CheckNameConflictCount = 0;
        private static int HasExternQualifierCount = 0;

        private static int nbShaders = 0;

        public static void Start(SemanticStage stage)
        {
            switch (stage)
            {
                case SemanticStage.Global:
                    TotalTime.Start();
                    break;
                case SemanticStage.VisitVariable:
                    VisitVariable.Start();
                    ++VisitVariableCount;
                    break;
                case SemanticStage.CommonVisit:
                    CommonVisit.Start();
                    ++CommonVisitCount;
                    break;
                case SemanticStage.FindDeclarationScope:
                    FindDeclarationScope.Start();
                    ++FindDeclarationScopeCount;
                    break;
                case SemanticStage.FindDeclarationsFromObject:
                    FindDeclarationsFromObject.Start();
                    ++FindDeclarationsFromObjectCount;
                    break;
                case SemanticStage.FindDeclarations:
                    FindDeclarations.Start();
                    ++FindDeclarationsCount;
                    break;
                case SemanticStage.ProcessMethodInvocation:
                    ProcessMethodInvocation.Start();
                    ++ProcessMethodInvocationCount;
                    break;
                case SemanticStage.CheckNameConflict:
                    CheckNameConflict.Start();
                    ++CheckNameConflictCount;
                    break;
                case SemanticStage.HasExternQualifier:
                    HasExternQualifier.Start();
                    ++HasExternQualifierCount;
                    break;
            }
        }

        public static void Pause(SemanticStage stage)
        {
            switch (stage)
            {
                case SemanticStage.Global:
                    TotalTime.Stop();
                    break;
                case SemanticStage.VisitVariable:
                    VisitVariable.Stop();
                    break;
                case SemanticStage.CommonVisit:
                    CommonVisit.Stop();
                    break;
                case SemanticStage.FindDeclarationScope:
                    FindDeclarationScope.Stop();
                    break;
                case SemanticStage.FindDeclarationsFromObject:
                    FindDeclarationsFromObject.Stop();
                    break;
                case SemanticStage.FindDeclarations:
                    FindDeclarations.Stop();
                    break;
                case SemanticStage.ProcessMethodInvocation:
                    ProcessMethodInvocation.Stop();
                    break;
                case SemanticStage.CheckNameConflict:
                    CheckNameConflict.Stop();
                    break;
                case SemanticStage.HasExternQualifier:
                    HasExternQualifier.Stop();
                    break;
            }
        }

        public static void IncrShader()
        {
            ++nbShaders;
        }

        public static void Reset()
        {
            nbShaders = 0;
            
            TotalTime.Reset();
            VisitVariable.Reset();
            CommonVisit.Reset();
            FindDeclarationScope.Reset();
            FindDeclarationsFromObject.Reset();
            FindDeclarations.Reset();
            ProcessMethodInvocation.Reset();
            CheckNameConflict.Reset();
            HasExternQualifier.Reset();

            VisitVariableCount = 0;
            CommonVisitCount = 0;
            FindDeclarationScopeCount = 0;
            FindDeclarationsFromObjectCount = 0;
            FindDeclarationsCount = 0;
            ProcessMethodInvocationCount = 0;
            CheckNameConflictCount = 0;
            HasExternQualifierCount = 0;
        }

        public static void PrintResult()
        {
            Logger.Info(@"--------------------------TOTAL SEMANTIC ANALYZER---------------------------");
            Logger.Info($"{nbShaders} shader(s) analyzed in {TotalTime.ElapsedMilliseconds} ms, {(nbShaders == 0 ? 0 : TotalTime.ElapsedMilliseconds / nbShaders)} ms per shader");
            Logger.Info($"VisitVariable {VisitVariable.ElapsedMilliseconds} ms for {VisitVariableCount} calls");
            Logger.Info($"CommonVisit took {CommonVisit.ElapsedMilliseconds} ms for {CommonVisitCount} calls");
            Logger.Info($"FindDeclarationScope took {FindDeclarationScope.ElapsedMilliseconds} ms for {FindDeclarationScopeCount} calls");
            Logger.Info($"FindDeclarationsFromObject took {FindDeclarationsFromObject.ElapsedMilliseconds} ms for {FindDeclarationsFromObjectCount} calls");
            Logger.Info($"FindDeclarations took {FindDeclarations.ElapsedMilliseconds} ms for {FindDeclarationsCount} calls");
            Logger.Info($"ProcessMethodInvocation took {ProcessMethodInvocation.ElapsedMilliseconds} ms for {ProcessMethodInvocationCount} calls");
            Logger.Info($"CheckNameConflict took {CheckNameConflict.ElapsedMilliseconds} ms for {CheckNameConflictCount} calls");
            Logger.Info($"HasExternQualifier took {HasExternQualifier.ElapsedMilliseconds} ms for {HasExternQualifierCount} calls");
            Logger.Info(@"-------------------------------------------------------------------------------");
        }
    }

    public enum SemanticStage
    {
        Global,
        VisitVariable,
        CommonVisit,
        FindDeclarationScope,
        FindDeclarationsFromObject,
        FindDeclarations,
        ProcessMethodInvocation,
        CheckNameConflict,
        HasExternQualifier
    }
}
