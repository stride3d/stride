// using Stride.Shaders.Spirv.Core;
// using Stride.Shaders.Spirv.Core.Buffers;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;

// namespace Stride.Shaders.Spirv.Processing;


// /// <summary>
// /// Checks for duplicate memory models in case of multiple entry points
// /// </summary>
// public struct MemoryModelDuplicatesRemover : INanoPass
// {

//     public void Apply(SpirvBuffer buffer)
//     {
//         var found = false;
//         var wid = 0;
//         var span = buffer.Declarations.InstructionSpan;
//         while(wid < buffer.Declarations.Length)
//         {
//             if ((span[wid] & 0xFFFF) == (int)SDSLOp.OpMemoryModel)
//             {
//                 if (!found)
//                     found = true;
//                 else
//                     SetOpNop(span.Slice(wid, span[wid] >> 16));
//             }
//             wid += span[wid] >> 16;
//         }
//     }

//     static void SetOpNop(Span<int> words)
//     {
//         words[0] = words.Length << 16;
//         words[1..].Clear();
//     }
    
// }
