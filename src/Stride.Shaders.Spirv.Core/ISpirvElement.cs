using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;


public interface ISpirvElement
{
    public int WordCount { get; }
    public SpanOwner<int> AsSpanOwner();
}

public static class ISpirvElementExtensions
{
    
    internal static SpanOwner<int> AsSpanOwner(this string? value)
    {
        if (value is null)
            return SpanOwner<int>.Empty;
        else
        {
            var lit = new LiteralString(value);
            var span = SpanOwner<int>.Allocate(lit.WordCount, AllocationMode.Clear);
            lit.WriteTo(span.Span);
            return span;
        }
    }
    
    internal static SpanOwner<int> AsSpanOwner<T>(this T value)
        where T : struct
    {
        if (value is ISpirvElement element)
            return element.AsSpanOwner();
        else
            return value switch
            {
                byte v => new LiteralInteger(v).AsSpanOwner(),
                sbyte v => new LiteralInteger(v).AsSpanOwner(),
                ushort v => new LiteralInteger(v).AsSpanOwner(),
                short v => new LiteralInteger(v).AsSpanOwner(),
                uint v => new LiteralInteger(v).AsSpanOwner(),
                int v => new LiteralInteger(v).AsSpanOwner(),
                long v => new LiteralInteger(v).AsSpanOwner(),
                ulong v => new LiteralInteger(v).AsSpanOwner(),
                Half v => new LiteralFloat(v).AsSpanOwner(),
                float v => new LiteralFloat(v).AsSpanOwner(),
                double v => new LiteralFloat(v).AsSpanOwner(),
                Enum e => new LiteralInteger(Convert.ToInt32(e)).AsSpanOwner(),
                _ => throw new NotImplementedException()
            };
            
    }
    
    internal static SpanOwner<int> AsSpanOwner<T>(this T? value)
        where T : struct
    {
        if (value is null)
            return SpanOwner<int>.Empty;
        else
            return value.Value.AsSpanOwner();
            
    }
    
    internal static SpanOwner<int> AsSpanOwner<T>(this Span<T> values)
    {

        int length = 0;
        foreach (var value in values)
        {
            length += value switch 
            {
                byte 
                or sbyte 
                or ushort 
                or short 
                or uint 
                or int 
                or Half 
                or float 
                or Enum => 1,
                long 
                or ulong 
                or double => 2,
                ISpirvElement element => element.WordCount,
                _ => throw new NotImplementedException()
            };
        }
        var span = SpanOwner<int>.Allocate(length);
        length = 0;
        for (int i = 0; i < values.Length; i++)
        {
            if(values[i] is ISpirvElement element)
            {
                element.AsSpanOwner().Span.CopyTo(span.Span[length..]);
                length += element.WordCount;
            }
            else if(values[i] is byte vb)
            {
                span.Span[i] = vb;
                length += 1;
            }
            else if(values[i] is sbyte vsb)
            {
                span.Span[i] = vsb;
                length += 1;
            }
            else if(values[i] is short vsh)
            {
                span.Span[i] = vsh;
                length += 1;
            }
            else if(values[i] is ushort vush)
            {
                span.Span[i] = vush;
                length += 1;
            }
            else if(values[i] is Half vh)
            {
                span.Span[i] = (int)(new LiteralFloat(vh).Words & 0xFFFFFFFF);
                length += 1;
            }
            else if(values[i] is float vf)
            {
                span.Span[i] = (int)(new LiteralFloat(vf).Words & 0xFFFFFFFF);
                length += 1;
            }
            else if(values[i] is int vi)
            {
                span.Span[i] = vi;
                length += 1;
            }
            else if(values[i] is Enum e)
            {
                span.Span[i] = Convert.ToInt32(e);
                length += 1;
            }
            else if(values[i] is uint vui)
            {
                span.Span[i] = (int)vui;
                length += 1;
            }
            else if(values[i] is double vd)
            {
                new LiteralFloat(vd).AsSpanOwner().Span.CopyTo(span.Span[i..(i+1)]);
                length += 1;
            }
            else if(values[i] is long vl)
            {
                new LiteralInteger(vl).AsSpanOwner().Span.CopyTo(span.Span[i..(i+1)]);
                length += 1;
            }
            else if(values[i] is ulong vul)
            {
                new LiteralInteger(vul).AsSpanOwner().Span.CopyTo(span.Span[i..(i+1)]);
                length += 1;
            }
            else throw new NotImplementedException();
        }
        return span;
    }

    
    internal static Span<int> AsSpirvSpan<T>(this T? value)
        where T : struct
        => value.AsSpanOwner().Span;
    
    internal static Span<int> AsSpirvSpan<T>(this T value)
        where T : struct
        => value.AsSpanOwner().Span;
    
    internal static Span<int> AsSpirvSpan(this string? value)
        => value.AsSpanOwner().Span;

    
    internal static Span<int> AsSpirvSpan<T>(this Span<T> values)
        => values.AsSpanOwner().Span;
}