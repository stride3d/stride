using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Parsing.SDSL.PreProcessing.Macros;

public struct CommentPhase() : IPreProcessorPhase
{
    public readonly SDSLPreProcessor Apply(SDSLPreProcessor sdslpp)
    {
        var frame = new CodeFrame();
        var last = sdslpp.CodeFrames[^1];
        var scanner = new Scanner<ScannableMemory>(last.Code.Memory);
        var started = false;
        while(!Parsers.Until(ref scanner, ["//", "/*"]))
        {
            if(!started)
                started = true;
            frame.Add(last, ..scanner.Position);
            if (Tokens.Literal("//", ref scanner))
                Parsers.Until(ref scanner, '\n', advance: true);
            else if (Tokens.Literal("/*", ref scanner))
                Parsers.Until(ref scanner, "*/", advance: true);
        }
        if (!started)
            frame.Add(last, ..);
        return sdslpp;
    }
}
