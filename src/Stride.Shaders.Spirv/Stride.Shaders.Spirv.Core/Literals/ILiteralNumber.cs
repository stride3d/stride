using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;

public interface ILiteralNumber : ISpirvElement
{
    public long Words { get; init; }
}
