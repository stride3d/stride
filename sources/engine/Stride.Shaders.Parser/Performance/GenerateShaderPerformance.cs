// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using Stride.Core.Diagnostics;

namespace Stride.Shaders.Parser.Performance
{
    public static class GenerateShaderPerformance
    {
        internal static Logger Logger = GlobalLogger.GetLogger("StrideShaderPerformance"); // Global logger for shader profiling

        private static Stopwatch Global = new Stopwatch();
        private static Stopwatch GroupByConstantBuffer = new Stopwatch();
        private static Stopwatch StreamCreator = new Stopwatch();
        private static Stopwatch ExpandForEachStatements = new Stopwatch();
        private static Stopwatch RemoveUselessVariables = new Stopwatch();

        public static void Start(GenerateShaderStage stage)
        {
            switch (stage)
            {
                case GenerateShaderStage.Global:
                    Global.Start();
                    break;
                case GenerateShaderStage.GroupByConstantBuffer:
                    GroupByConstantBuffer.Start();
                    break;
                case GenerateShaderStage.StreamCreator:
                    StreamCreator.Start();
                    break;
                case GenerateShaderStage.ExpandForEachStatements:
                    ExpandForEachStatements.Start();
                    break;
                case GenerateShaderStage.RemoveUselessVariables:
                    RemoveUselessVariables.Start();
                    break;
            }
        }

        public static void Pause(GenerateShaderStage stage)
        {
            switch (stage)
            {
                case GenerateShaderStage.Global:
                    Global.Stop();
                    break;
                case GenerateShaderStage.GroupByConstantBuffer:
                    GroupByConstantBuffer.Stop();
                    break;
                case GenerateShaderStage.StreamCreator:
                    StreamCreator.Stop();
                    break;
                case GenerateShaderStage.ExpandForEachStatements:
                    ExpandForEachStatements.Stop();
                    break;
                case GenerateShaderStage.RemoveUselessVariables:
                    RemoveUselessVariables.Start();
                    break;
            }
        }

        public static void Reset()
        {
            Global.Reset();
            GroupByConstantBuffer.Reset();
            StreamCreator.Reset();
            ExpandForEachStatements.Reset();
            RemoveUselessVariables.Reset();
        }

        public static void PrintResult()
        {
            Logger.Info(@"----------------------------GENERATE SHADER ANALYZER-----------------------------");
            Logger.Info($"Whole generation took {Global.ElapsedMilliseconds} ms");
            Logger.Info($"GroupByConstantBuffer took {GroupByConstantBuffer.ElapsedMilliseconds} ms");
            Logger.Info($"StreamCreator took {StreamCreator.ElapsedMilliseconds} ms");
            Logger.Info($"ExpandForEachStatements took {ExpandForEachStatements.ElapsedMilliseconds} ms");
            Logger.Info($"RemoveUselessVariables took {RemoveUselessVariables.ElapsedMilliseconds} ms");
            Logger.Info(@"-------------------------------------------------------------------------------");
        }
    }

    public enum GenerateShaderStage
    {
        Global,
        GroupByConstantBuffer,
        StreamCreator,
        ExpandForEachStatements,
        RemoveUselessVariables
    }
}
