// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using Stride.Core.Diagnostics;

namespace Stride.Shaders.Parser.Performance
{
    public static class MixPerformance
    {
        internal static Logger Logger = GlobalLogger.GetLogger("StrideShaderPerformance"); // Global logger for shader profiling

        private static Stopwatch Global = new Stopwatch();
        private static Stopwatch AddDefaultCompositions = new Stopwatch();
        private static Stopwatch CreateReferencesStructures = new Stopwatch();
        private static Stopwatch RegenKeys = new Stopwatch();
        private static Stopwatch BuildMixinInheritance = new Stopwatch();
        private static Stopwatch ComputeMixinOccurrence = new Stopwatch();
        private static Stopwatch BuildStageInheritance = new Stopwatch();
        private static Stopwatch LinkVariables = new Stopwatch();
        private static Stopwatch ProcessExterns = new Stopwatch();
        private static Stopwatch PatchAllMethodInferences = new Stopwatch();
        private static Stopwatch MergeReferences = new Stopwatch();
        private static Stopwatch RenameAllVariables = new Stopwatch();
        private static Stopwatch RenameAllMethods = new Stopwatch();
        private static Stopwatch GenerateShader = new Stopwatch();
        
        public static void Start(MixStage stage)
        {
            switch (stage)
            {
                case MixStage.Global:
                    Global.Start();
                    break;
                case MixStage.AddDefaultCompositions:
                    AddDefaultCompositions.Start();
                    break;
                case MixStage.CreateReferencesStructures:
                    CreateReferencesStructures.Start();
                    break;
                case MixStage.RegenKeys:
                    RegenKeys.Start();
                    break;
                case MixStage.BuildMixinInheritance:
                    BuildMixinInheritance.Start();
                    break;
                case MixStage.ComputeMixinOccurrence:
                    ComputeMixinOccurrence.Start();
                    break;
                case MixStage.BuildStageInheritance:
                    BuildStageInheritance.Start();
                    break;
                case MixStage.LinkVariables:
                    LinkVariables.Start();
                    break;
                case MixStage.ProcessExterns:
                    ProcessExterns.Start();
                    break;
                case MixStage.PatchAllMethodInferences:
                    PatchAllMethodInferences.Start();
                    break;
                case MixStage.MergeReferences:
                    MergeReferences.Start();
                    break;
                case MixStage.RenameAllVariables:
                    RenameAllVariables.Start();
                    break;
                case MixStage.RenameAllMethods:
                    RenameAllMethods.Start();
                    break;
                case MixStage.GenerateShader:
                    GenerateShader.Start();
                    break;
            }
        }

        public static void Pause(MixStage stage)
        {
            switch (stage)
            {
                case MixStage.Global:
                    Global.Stop();
                    break;
                case MixStage.AddDefaultCompositions:
                    AddDefaultCompositions.Stop();
                    break;
                case MixStage.CreateReferencesStructures:
                    CreateReferencesStructures.Stop();
                    break;
                case MixStage.RegenKeys:
                    RegenKeys.Stop();
                    break;
                case MixStage.BuildMixinInheritance:
                    BuildMixinInheritance.Stop();
                    break;
                case MixStage.ComputeMixinOccurrence:
                    ComputeMixinOccurrence.Stop();
                    break;
                case MixStage.BuildStageInheritance:
                    BuildStageInheritance.Stop();
                    break;
                case MixStage.LinkVariables:
                    LinkVariables.Stop();
                    break;
                case MixStage.ProcessExterns:
                    ProcessExterns.Stop();
                    break;
                case MixStage.PatchAllMethodInferences:
                    PatchAllMethodInferences.Stop();
                    break;
                case MixStage.MergeReferences:
                    MergeReferences.Stop();
                    break;
                case MixStage.RenameAllVariables:
                    RenameAllVariables.Stop();
                    break;
                case MixStage.RenameAllMethods:
                    RenameAllMethods.Stop();
                    break;
                case MixStage.GenerateShader:
                    GenerateShader.Stop();
                    break;
            }
        }

        public static void Reset()
        {
            Global.Reset();
            AddDefaultCompositions.Reset();
            CreateReferencesStructures.Reset();
            RegenKeys.Reset();
            BuildMixinInheritance.Reset();
            ComputeMixinOccurrence.Reset();
            BuildStageInheritance.Reset();
            LinkVariables.Reset();
            ProcessExterns.Reset();
            PatchAllMethodInferences.Reset();
            MergeReferences.Reset();
            RenameAllVariables.Reset();
            RenameAllMethods.Reset();
            GenerateShader.Reset();
        }

        public static void PrintResult()
        {
            Logger.Info(@"---------------------------------MIX ANALYZER-----------------------------------");
            Logger.Info($"Whole mix took {Global.ElapsedMilliseconds} ms");
            Logger.Info($"AddDefaultCompositions took {AddDefaultCompositions.ElapsedMilliseconds} ms");
            Logger.Info($"CreateReferencesStructures took {CreateReferencesStructures.ElapsedMilliseconds} ms");
            Logger.Info($"RegenKeys took {RegenKeys.ElapsedMilliseconds} ms");
            Logger.Info($"BuildMixinInheritance took {BuildMixinInheritance.ElapsedMilliseconds} ms");
            Logger.Info($"ComputeMixinOccurrence took {ComputeMixinOccurrence.ElapsedMilliseconds} ms");
            Logger.Info($"BuildStageInheritance took {BuildStageInheritance.ElapsedMilliseconds} ms");
            Logger.Info($"LinkVariables took {LinkVariables.ElapsedMilliseconds} ms");
            Logger.Info($"ProcessExterns took {ProcessExterns.ElapsedMilliseconds} ms");
            Logger.Info($"PatchAllMethodInferences took {PatchAllMethodInferences.ElapsedMilliseconds} ms");
            Logger.Info($"MergeReferences took {MergeReferences.ElapsedMilliseconds} ms");
            Logger.Info($"RenameAllVariables took {RenameAllVariables.ElapsedMilliseconds} ms");
            Logger.Info($"RenameAllMethods took {RenameAllMethods.ElapsedMilliseconds} ms");
            Logger.Info($"GenerateShader took {GenerateShader.ElapsedMilliseconds} ms");
            Logger.Info(@"-------------------------------------------------------------------------------");
        }
    }

    public enum MixStage
    {
        Global,
        AddDefaultCompositions,
        CreateReferencesStructures,
        RegenKeys,
        BuildMixinInheritance,
        ComputeMixinOccurrence,
        BuildStageInheritance,
        LinkVariables,
        ProcessExterns,
        PatchAllMethodInferences,
        MergeReferences,
        RenameAllVariables,
        RenameAllMethods,
        GenerateShader
    }
}
