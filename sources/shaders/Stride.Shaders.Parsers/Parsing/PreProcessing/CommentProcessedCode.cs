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
            Parsers.Until(ref scanner, ["//", "/*", "\""]);
            if (!started)
                started = true;
            Add(lastPos..scanner.Position);
            lastPos = scanner.Position;
            // A comment becomes as many spaces, keeping its line breaks: lines and columns stay those of the original,
            // so are the ones in error messages. The end of a line comment stays with the code after it
            if (Tokens.Literal("//", ref scanner))
            {
                Parsers.Until(ref scanner, '\n');
                AddBlank(lastPos..scanner.Position);
                lastPos = scanner.Position;
            }
            else if (Tokens.Literal("/*", ref scanner))
            {
                Parsers.Until(ref scanner, "*/", advance: true);
                AddBlank(lastPos..scanner.Position);
                lastPos = scanner.Position;
            }
            else if (Tokens.Literal("\"", ref scanner))
            {
                Parsers.Until(ref scanner, "\"", advance: true);
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

    // Spaces in place of the original text, except its line breaks and tabs: the layout stays the original's
    internal void AddBlank(Range range)
    {
        var original = Original.Span[range];
        Span<char> blank = original.Length <= 256 ? stackalloc char[original.Length] : new char[original.Length];
        for (var i = 0; i < original.Length; i++)
            blank[i] = original[i] is '\n' or '\r' or '\t' ? original[i] : ' ';
        Add(blank);
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
