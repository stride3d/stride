using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI;

namespace Stride.Core.CompilerServices.DataEvaluationApi.ModeInfos;
internal class AssignModeInfo : IContentModeInfo
{
    public bool IsContentMode { get; set; }
    public string GenerationInvocation { get; }
    public bool NeedsFinalAssignment { get; }
    public string DataMemberMode { get; set; } = "Stride.Core.DataMemberMode.Assign";
}
