using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Parsing.SDSL.PreProcessing;

/// <summary>
/// Representation of the code where comments have been removed
/// </summary>
public struct CommentProcessedCode : IScannableCode
{
    public ReadOnlyMemory<char> Original { get; init; }
    public MemoryOwner<char> Processed { get; private set; } = MemoryOwner<char>.Empty;
    public MemoryOwner<TextLink> Links { get; private set; } = MemoryOwner<TextLink>.Empty;

    public readonly ReadOnlySpan<char> Span => Memory.Span;

    public readonly ReadOnlyMemory<char> Memory => Processed.Memory;

    public CommentProcessedCode(string originalFile)
    {
        Original = originalFile.AsMemory();
        Process();
    }
    public CommentProcessedCode(ReadOnlyMemory<char> originalFile)
    {
        Original = originalFile;
        Process();
    }

    internal void Process()
    {
        var scanner = new Scanner<ScannableReadOnlyMemory>(new(Original));
        var started = false;
        var lastPos = 0;
        while (!scanner.IsEof)
        {
            CommonParsers.Until(ref scanner, ["//", "/*", "\""]);
            if (!started)
                started = true;
            Add(lastPos..scanner.Position);
            lastPos = scanner.Position;
            if (Terminals.Literal("//", ref scanner))
            {
                CommonParsers.Until(ref scanner, '\n', advance: true);
                lastPos = scanner.Position;
                Add([' ']);
            }
            else if (Terminals.Literal("/*", ref scanner))
            {
                CommonParsers.Until(ref scanner, "*/", advance: true);
                lastPos = scanner.Position;
                Add([' ']);
            }
            else if (Terminals.Literal("\"", ref scanner))
            {
                CommonParsers.Until(ref scanner, "\"", advance: true);
                Add(lastPos..scanner.Position);
                lastPos = scanner.Position;
            }
        }
    }

    internal void Add(Range range)
    {
        (_, var length) = range.GetOffsetAndLength(Original.Length);
        Processed = Processed.Add(Original.Span[range]);
        Links = Links.Add([new(range, (Processed.Length - length)..Processed.Length)]);

    }
    internal void Add(Span<char> span)
    {
        Processed = Processed.Add(span);
    }

    /// <summary>
    /// Gets the list of text locations that translate the range chosen to the original file.
    /// </summary>
    /// <value></value>
    public readonly TextLocation GetOriginalLocation(Range range)
    {
        var (start, length) = range.GetOffsetAndLength(Processed.Length);
        var end = start + length;
        var outputStart = -1;
        foreach (var link in Links.Span)
        {
            var (linkStart, linkLength) = link.Processed.GetOffsetAndLength(Processed.Length);
            var linkEnd = linkStart + linkLength;
            (var linkOriginalStart, var linkOriginalLength) = link.Origin.GetOffsetAndLength(Original.Length);
            var linkOriginalEnd = linkOriginalStart + linkOriginalLength;
            var realStart = linkOriginalStart + (start - linkStart);
            if (outputStart == -1 && start >= linkStart && start < linkEnd)
            {
                outputStart = realStart;
            }
            if (end <= linkEnd)
            {
                var outputEnd = linkOriginalEnd - (linkEnd - end);
                return new(Original, outputStart..outputEnd);
            }
        }
        return new(Original, 0..0);
    }
}