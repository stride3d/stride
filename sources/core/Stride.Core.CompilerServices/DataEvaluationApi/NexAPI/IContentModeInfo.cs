namespace StrideSourceGenerator.NexAPI;
internal interface IContentModeInfo
{
    public string DataMemberMode { get; set; }
    public bool IsContentMode { get; set; }
    public string GenerationInvocation { get; }
    public bool NeedsFinalAssignment { get; }
}
