using Stride.Shaders.Core;

namespace Stride.Shaders.Spirv.Processing.Interfaces.Models;

/// <summary>
/// Groups all stream-related data for a shader stage: interface variables, struct types, and array sizes.
/// </summary>
internal record struct StageStreamLayout(
    List<(StreamVariableInfo Info, int Id, SymbolType InterfaceType)> InputStreams,
    List<(StreamVariableInfo Info, int Id, SymbolType InterfaceType)> OutputStreams,
    List<(StreamVariableInfo Info, int Id, SymbolType InterfaceType)> PatchInputStreams,
    List<(StreamVariableInfo Info, int Id, SymbolType InterfaceType)> PatchOutputStreams,
    StructType InputType,
    StructType OutputType,
    StructType StreamsType,
    StructType? ConstantsType,
    int? ArrayInputSize,
    int? ArrayOutputSize,
    int StreamsVariableId);
