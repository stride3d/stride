// using Stride.Shaders.Spirv.Core;
// using Stride.Shaders.Spirv.Core.Buffers;
// using Stride.Shaders.Spirv.Core.Parsing;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;

// namespace Stride.Shaders.Spirv.Processing;


// /// <summary>
// /// Removes SDSL specific instructions
// /// </summary>
// public struct OpRemover : INanoPass
// {

//     public void Apply(SpirvBuffer buffer)
//     {
//         var decl = new InstructionEnumerator(buffer.Declarations);
//         while(decl.MoveNext())
//         {
//             var i = decl.Current;
//             if (InstructionInfo.Operators.Contains(i.OpCode)) 
//                 SetOpNop(i.AsRef());
//         }
//         foreach (var (_, f) in buffer.Functions)
//         {
//             var func = new InstructionEnumerator(f);
//             while(func.MoveNext())
//             {
//                 var i = func.Current;
//                 if (InstructionInfo.Operators.Contains(i.OpCode))
//                     SetOpNop(i.AsRef());
//             }
//         }
//     }

//     static void SetOpNop(RefInstruction i)
//     {
//         i.Words[0] = i.WordCount << 16;
//         i.Operands.Clear();
//     }
    
// }
