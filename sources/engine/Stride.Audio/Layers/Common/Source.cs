namespace Stride.Audio;

public sealed unsafe partial class Source
{
    public int SampleRate { get; internal set; }
    public bool Mono { get; internal set; }
    public bool Streamed { get; internal set; }
}
