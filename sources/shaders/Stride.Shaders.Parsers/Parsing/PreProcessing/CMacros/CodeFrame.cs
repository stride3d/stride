using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Parsing.SDSL.PreProcessing.Macros;


public sealed class CodeFrame() : IDisposable
{
    public MemoryOwner<char> Code { get; private set; } = MemoryOwner<char>.Empty;
    public List<CodeFrameSnippets> CodeSpans { get; } = [];
    public int Length => Code.Length;

    public void Resize(int length) => Code = Code.Resize(length);

    public void Add(CodeFrame previousFrame, Range range)
    {
        var span = previousFrame.Code.Span[range];
        Resize(span.Length);
        span.CopyTo(Code.Span[^span.Length..]);
        CodeSpans.Add((previousFrame, range));
    }

    public void Dispose() =>
        Code.Dispose();
}