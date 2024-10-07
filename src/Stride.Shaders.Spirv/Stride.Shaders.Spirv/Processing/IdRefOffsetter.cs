using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Spirv.Processing;



/// <summary>
/// Offsets ids for each mixins inherited
/// </summary>
public struct IdRefOffsetter : INanoPass
{
    public IdRefOffsetter() { }

    public void Apply(MultiBuffer buffer)
    {
        //int offset = 0;
        //int nextOffset = 0;
        //foreach (var i in buffer)
        //{
        //    // if we hit a mixin name we reset stuff
        //    if (i.OpCode == SDSLOp.OpSDSLMixinName)
        //    {
        //        offset += nextOffset;
        //        nextOffset = 0;
        //    }
        //    else
        //    {
        //        if (i.ResultId != null)
        //            nextOffset = i.ResultId.Value;
        //        i.AsRef().OffsetIds(offset);
        //    }
        //}
    }
}
