using Stride.Shaders.Core;

namespace Stride.Shaders.Spirv.Processing.Interfaces.Models;

internal enum StreamVariableType
{
    Input,
    Output,
}

internal class StreamVariableInfo(string? semantic, string name, PointerType type, int variableId)
{
    public bool Patch { get; set; }
    public string? Semantic { get; } = semantic;
    public string Name { get; } = name;
    public SymbolType Type { get; } = type.BaseType;
    public int VariableId { get; } = variableId;

    public int? InputId { get; set; }
    public int? OutputId { get; set; }

    public int? InputLayoutLocation { get; set; }
    public int? OutputLayoutLocation { get; set; }

    /// <summary>
    /// We automatically mark input: a variable read before it's written to, or an output without a write
    /// </summary>
    public bool Input => Read || (Output && !Write);
    public bool Output { get => field; set { field = value; UsedAnyStage = true; } }
    public bool UsedThisStage => Input || Output || Read || Write;

    public bool Read { get => field; set { field = value; UsedAnyStage = true; } }
    public bool Write { get => field; set { field = value; UsedAnyStage = true; } }
    public bool UsedAnyStage { get; private set; }
    public int? InputStructFieldIndex { get; internal set; }
    public int? OutputStructFieldIndex { get; internal set; }

    // Note: if Patch is true, it will be index in CONSTANTS struct, otherwise STREAMS struct
    public int StreamStructFieldIndex { get; internal set; }

    public override string ToString() => $"{Type} {Name} {(Read ? "R" : "")} {(Write ? "W" : "")}";
}
