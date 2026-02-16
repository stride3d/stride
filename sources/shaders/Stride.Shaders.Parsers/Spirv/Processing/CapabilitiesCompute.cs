// using Stride.Shaders.Spirv.Core;
// using Stride.Shaders.Spirv.Core.Buffers;
// using Stride.Shaders.Spirv.Core.Parsing;
// using static Stride.Shaders.Spirv.Specification;

// namespace Stride.Shaders.Spirv.Processing;

// public struct CapabilitiesCompute : INanoPass
// {
//     public void Apply(SpirvBuffer buffer)
//     {
//         throw new NotImplementedException("Needs to finish checking the spec");
//     }

//     public static void AddCapabilities(Instruction instruction)
//     {
//         if(instruction.OpCode == Op.OpEntryPoint)
//         {
//             if(instruction.GetOperand<LiteralInteger>("executionModel")?.Words == (int)ExecutionModel.Geometry)
//             {
//                 //Add capability geometry
//             }
//             else if (instruction.GetOperand<LiteralInteger>("executionModel")?.Words == (int)ExecutionModel.TessellationControl)
//             {
//                 //Add capability tess

//             }
//             else if (instruction.GetOperand<LiteralInteger>("executionModel")?.Words == (int)ExecutionModel.TessellationEvaluation)
//             {
//                 //Add capability tess
//             }
//         }
//         else if(instruction.OpCode == Op.OpTypeFloat && instruction.Words.Span[2] == 16)
//         {
//             // Add capability Float16
//         }
//         else if (instruction.OpCode == Op.OpTypeFloat && instruction.Words.Span[2] == 64)
//         {
//             // Add capability Float64
//         }
//         else if (instruction.OpCode == Op.OpTypeInt && instruction.Words.Span[2] == 64)
//         {
//             // Add capability Float64
//         }
//         else if (instruction.OpCode == Op.OpTypeInt && instruction.Words.Span[2] == 16)
//         {
//             // Add capability Float64
//         }
//         else if (instruction.OpCode == Op.OpTypeInt && instruction.Words.Span[2] == 8)
//         {
//             // Add capability Float64
//         }
        
//         // TODO : Check if any atomic instructions operates on integers
//         // else if (instruction.OpCode == Op.OpAtomic && instruction.Words.Span[2] == 64)
//         // {
//         //     // Add capability Float64
//         // }


//     }
// }
