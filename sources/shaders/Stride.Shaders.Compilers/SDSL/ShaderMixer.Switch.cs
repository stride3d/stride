// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL;

public partial class ShaderMixer
{
    /// <summary>
    /// Turns every OpSwitchIdSDSL into an OpSwitch. The labels of OpSwitch are values, so a switch whose labels depend
    /// on a generic of its shader is compiled with the constants themselves, which are all resolved once mixing.
    /// </summary>
    private static bool LowerSwitchIds(SpirvContext context, SpirvBuffer buffer, ILogger log)
    {
        // Note: most mixes have no OpSwitchIdSDSL, so this only remembers the last OpLine and resolves it for an error
        (int File, int Line, int Column)? currentLine = null;
        var success = true;

        foreach (var i in buffer)
        {
            if (i.Op == Op.OpLine)
            {
                OpLine line = i;
                currentLine = (line.File, line.Line, line.Column);
            }
            else if (i.Op == Op.OpFunction)
            {
                currentLine = null;
            }
            else if (i.Op == Op.OpSwitchIdSDSL && !TryLowerSwitchId(context, i, out var error))
            {
                var location = currentLine is { } l && context.GetBuffer().TryGetInstructionById(l.File, out var file) && file.Op == Op.OpString
                    ? $"{((OpString)file).Value}({l.Line},{l.Column}): "
                    : null;
                log.Error($"{location}{error}");
                success = false;
            }
        }

        return success;
    }

    // Both instructions have the same layout: [header, selector, default, (label, block)...]
    private static bool TryLowerSwitchId(SpirvContext context, OpDataIndex i, out string? error)
    {
        var words = i.Data.Memory.Span;
        var values = new List<int>();
        for (var labelIndex = 3; labelIndex < words.Length; labelIndex += 2)
        {
            if (!context.TryGetConstantValue(words[labelIndex], out var constantValue, out _) || constantValue is not (int or uint))
            {
                error = "the value of a switch case label could not be computed";
                return false;
            }

            var value = constantValue is uint u ? unchecked((int)u) : (int)constantValue;
            if (values.Contains(value))
            {
                error = $"a switch has two case labels with the same value ({value})";
                return false;
            }
            values.Add(value);
        }

        for (var index = 0; index < values.Count; index++)
            words[3 + index * 2] = values[index];
        words[0] = (int)Op.OpSwitch | (words.Length << 16);

        error = null;
        return true;
    }
}
