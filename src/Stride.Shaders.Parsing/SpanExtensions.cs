#if NETSTANDARD2_0
public static class SpanExtensions
{
    public static int Count(this ReadOnlySpan<char> span, char c)
    {
        int count = 0;
        foreach(var item in span)
            if(c == item)
                count +=1;
        return count;
    }
    public static int Count(this ReadOnlySpan<int> span, int c)
    {
        int count = 0;
        foreach(var item in span)
            if(c == item)
                count +=1;
        return count;
    }
    public static int Count(this Span<char> span, char c)
    {
        int count = 0;
        foreach(var item in span)
            if(c == item)
                count +=1;
        return count;
    }
    public static int Count(this Span<int> span, int c)
    {
        int count = 0;
        foreach(var item in span)
            if(c == item)
                count +=1;
        return count;
    }
    public static ReadOnlySpan<char> TrimEnd(this ReadOnlySpan<char> span)
    {
        return span;
    }
    public static ReadOnlyMemory<char> TrimEnd(this ReadOnlyMemory<char> span)
    {
        return span;
    }
    public static ReadOnlySpan<char> TrimStart(this ReadOnlySpan<char> span)
    {
        for(int i = 0; i < span.Length; i++)
            if(!char.IsWhiteSpace(span[i]))
                return span[i..];
        return span;
    }
    public static ReadOnlyMemory<char> TrimStart(this ReadOnlyMemory<char> memory)
    {
        for(int i = 0; i < memory.Length; i++)
            if(!char.IsWhiteSpace(memory.Span[i]))
                return memory[i..];
        return memory;
    }
}

#endif