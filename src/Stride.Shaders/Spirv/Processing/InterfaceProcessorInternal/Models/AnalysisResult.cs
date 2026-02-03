namespace Stride.Shaders.Spirv.Processing.InterfaceProcessorInternal.Models;

internal record struct AnalysisResult(
    Dictionary<int, string> Names,
    Dictionary<int, StreamVariableInfo> Streams,
    Dictionary<int, VariableInfo> Variables,
    Dictionary<int, CBufferInfo> CBuffers,
    Dictionary<int, ResourceGroup> ResourceGroups,
    Dictionary<int, ResourceInfo> Resources);
