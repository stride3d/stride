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
    /// We automatically mark input: a variable read before it's written to, or an output without a write.
    /// A read or an output after a write that might not have run needs the input too, if there is one to get.
    /// </summary>
    public bool Input => Read || (Output && !Write) || (HasProducer && (MaybeRead || (Output && !DefiniteWrite)));
    public bool Output { get => field; set { field = value; UsedAnyStage = true; } }
    public bool UsedThisStage => Input || Output || InternalPatchConstantOutput || Read || Write;

    /// <summary>
    /// When true, this variable is only used to transfer data from the hull shader main function
    /// to the patch constant function (via inter-invocation Output read after barrier).
    /// It still needs a StorageClass.Output variable with a Location, but the next stage (DS)
    /// doesn't consume it, so a dummy matching Input variable is added to DS to satisfy
    /// Vulkan interface matching requirements.
    /// </summary>
    public bool InternalPatchConstantOutput { get => field; set { field = value; UsedAnyStage = true; } }
    public bool Read { get => field; set { field = value; UsedAnyStage = true; } }
    public bool Write { get => field; set { field = value; UsedAnyStage = true; } }

    /// <summary>
    /// Written whole, on <c>streams</c> itself and not on a copy, by code that always runs: no later read can see
    /// the value from the previous stage. Two writes in the two branches of an <c>if</c> don't count.
    /// </summary>
    public bool DefiniteWrite { get; set; }

    /// <summary>
    /// Read after a <see cref="Write"/> that isn't a <see cref="DefiniteWrite"/>. Unlike <see cref="Read"/>, it
    /// doesn't need a producer: without one, the stream starts at zero.
    /// </summary>
    public bool MaybeRead { get; set; }

    /// <summary>
    /// A stage before this one accesses this stream, or this stage is the vertex shader and the stream is one of
    /// its inputs. Only meaningful during the stage being processed.
    /// </summary>
    public bool HasProducer { get; set; }

    public bool UsedAnyStage { get; private set; }
    public int? InputStructFieldIndex { get; internal set; }
    public int? OutputStructFieldIndex { get; internal set; }

    /// <summary>
    /// Name of the first method that reads this stream before any write (set during read/write analysis).
    /// Used to produce a helpful diagnostic when such a stream ends up with no producer and no semantic.
    /// </summary>
    public string? ReadByMethod { get; set; }

    /// <summary>
    /// Where that first read is in the source (<c>file(line,column): </c>), if known.
    /// </summary>
    public string? ReadAt { get; set; }

    // Note: if Patch is true, it will be index in CONSTANTS struct, otherwise STREAMS struct
    public int StreamStructFieldIndex { get; internal set; }

    public override string ToString()
    {
        var result = $"{Type} {Name} {(Read ? "R" : "")} {(Write ? "W" : "")}";
        if (Semantic != null)
            result += $" : {Semantic}";
        return result;
    }
}
