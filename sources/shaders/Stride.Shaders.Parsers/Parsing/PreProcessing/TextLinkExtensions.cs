namespace Stride.Shaders.Parsing.SDSL.PreProcessing;



public static class TextLinkExtensions
{

    public static bool Intersect(this Range range, Range other, int length)
    {
        var (start, l) = range.GetOffsetAndLength(length);
        var end = start + l;
        var (otherStart, ol) = other.GetOffsetAndLength(length);
        var otherEnd = otherStart + ol;

        return start <= otherEnd && end >= otherStart;
    }

    public static bool OriginIntersect(this TextLink link, Range range, int length, int originLength, out Range? result)
    {
        if (link.Processed.Intersect(range, length))
        {
            var (start, l) = link.Origin.GetOffsetAndLength(originLength);
            var end = start + l;
            result = new Range(start, end);
            return true;
        }

        result = null;
        return false;
    }
}