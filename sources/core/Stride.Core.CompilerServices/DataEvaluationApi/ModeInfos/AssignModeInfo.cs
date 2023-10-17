using StrideSourceGenerator.NexAPI;

namespace Stride.Core.CompilerServices.CodeFixes.ModeInfos;
internal class AssignModeInfo : IContentModeInfo
{
    public bool IsContentMode { get; set; }
    public string GenerationInvocation { get; }
    public bool NeedsFinalAssignment { get; }
    public string DataMemberMode { get; set; } = "Stride.Core.DataMemberMode.Assign";
}
