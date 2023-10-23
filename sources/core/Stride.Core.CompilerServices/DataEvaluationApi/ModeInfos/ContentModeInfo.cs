using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI;

namespace Stride.Core.CompilerServices.DataEvaluationApi.ModeInfos;
internal class ContentModeInfo : IContentModeInfo
{
    public string DataMemberMode { get; set; } = "Stride.Core.DataMemberMode.Content";
    public bool IsContentMode { get; set; }
    public string GenerationInvocation { get; }
    public bool NeedsFinalAssignment { get; }

    public override bool Equals(object? obj)
    {
        return obj is ContentModeInfo info &&
               DataMemberMode == info.DataMemberMode &&
               IsContentMode == info.IsContentMode &&
               GenerationInvocation == info.GenerationInvocation &&
               NeedsFinalAssignment == info.NeedsFinalAssignment;
    }

    public override int GetHashCode()
    {
        var hashCode = -1398128883;
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DataMemberMode);
        hashCode = hashCode * -1521134295 + IsContentMode.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(GenerationInvocation);
        hashCode = hashCode * -1521134295 + NeedsFinalAssignment.GetHashCode();
        return hashCode;
    }
}
