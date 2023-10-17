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
}
