// using Stride.Shaders.Spirv.Core;
// using Stride.Shaders.Spirv.Core.Buffers;
// using Stride.Shaders.Spirv.Processing;
// using static Stride.Shaders.Spirv.Specification;

// namespace Stride.Shaders.Spirv.PostProcessing;

// public struct SDSLVariableReplace : INanoPass
// {
//     public void Apply(SpirvBuffer buffer)
//     {
//         foreach (var i in buffer.Declarations.UnorderedInstructions)
//         {
//             if (i.OpCode == Op.OpSDSLIOVariable)
//             {

//                 var sclassv = i.GetOperand<LiteralInteger>("storageclass");
//                 var sclass = StorageClass.Private;
//                 if (sclassv != null)
//                     sclass = (StorageClass)sclassv.Value.Words;
//                 var variable = buffer.AddOpVariable(i.GetOperand<IdResultType>("resultType") ?? -1, sclass, i.GetOperand<IdRef>("initializer"));
//                 variable.Operands.Span[1] = i.ResultId ?? -1;
//                 buffer.AddOpName(variable, i.GetOperand<LiteralString>("name") ?? $"var{Guid.NewGuid()}");
//                 SetOpNop(i.Words.Span);
//             }
//             else if (i.OpCode == Op.OpSDSLVariable)
//             {
//                 var sclassv = i.GetOperand<LiteralInteger>("storageclass");
//                 var sclass = StorageClass.Private;
//                 if (sclassv != null)
//                     sclass = (StorageClass)sclassv.Value.Words;
//                 var variable = buffer.AddOpVariable(i.GetOperand<IdResultType>("resultType") ?? -1, sclass, i.GetOperand<IdRef>("initializer"));
//                 variable.Operands.Span[1] = i.ResultId ?? -1;
//                 buffer.AddOpName(variable, i.GetOperand<LiteralString>("name") ?? $"var{Guid.NewGuid()}");
//                 SetOpNop(i.Words.Span);
//             }
//         }
//         foreach (var (n, f) in buffer.Functions)
//         {
//             foreach (var i in f.UnorderedInstructions)
//             {
//                 if(i.OpCode == Op.OpSDSLFunctionParameter)
//                 {
//                     var name = i.GetOperand<LiteralString>("name");
//                     var resultType = i.ResultType ?? -1;
//                     var variable = f.AddOpFunctionParameter(resultType);
//                     variable.Operands.Span[1] = i.ResultId ?? -1;
//                     buffer.AddOpName(variable, name ?? $"var{Guid.NewGuid()}");
//                     SetOpNop(i.Words.Span);
//                 }
//                 else if (i.OpCode == Op.OpSDSLVariable)
//                 {

//                     var sclassv = i.GetOperand<LiteralInteger>("storageclass");
//                     var sclass = StorageClass.Private;
//                     if (sclassv != null)
//                         sclass = (StorageClass)sclassv.Value.Words;
//                     var name = i.GetOperand<LiteralString>("name");
//                     var resultType = i.ResultType ?? -1;
//                     var initializer = i.GetOperand<IdRef>("initializer");
//                     var variable = f.AddOpVariable(resultType, sclass, initializer);
//                     variable.Operands.Span[1] = i.ResultId ?? -1;
//                     buffer.AddOpName(variable, name ?? $"var{Guid.NewGuid()}");
//                     SetOpNop(i.Words.Span);
//                 }
//             }
//         }
//     }
//     static void SetOpNop(Span<int> words)
//     {
//         words[0] = words.Length << 16;
//         words[1..].Clear();
//     }
// }