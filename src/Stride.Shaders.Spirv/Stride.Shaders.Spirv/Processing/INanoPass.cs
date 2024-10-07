using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Spirv.Processing;

/// <summary>
/// Nano pass for the mixin compiler
/// </summary>
public interface INanoPass
{
    void Apply(MultiBuffer buffer);
}
