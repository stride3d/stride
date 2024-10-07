using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Spirv.Core;

/// <summary>
/// Represents the index bound for instructions in a spirv module
/// </summary>
public struct Bound
{
    public int Offset { get; set; }
    public int Count { get; set; }

    public Bound()
    {
        Offset = 0;
        Count = 0;
    }

    public int Next()
    {
        Count += 1;
        return Offset + Count;
    }
}
