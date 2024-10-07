using System.Text;

namespace Stride.Shaders.Spirv.Core;

public class DisWriter
{
    StringBuilder builder;
    int idOffset;

    public DisWriter(int bound) //34
    {
        builder = new();
        idOffset = 3;
        while (bound > 0)
        {
            bound /= 10;
            idOffset += 1;
        }
    }
    public void Append(IdResult? result)
    {
        if (result != null)
        {
            var tmp = result.Value;
            var size = 1;
            while (tmp > 0)
            {
                tmp /= 10;
                size += 1;
            }
            builder.Append('%').Append(result.Value).Append(' ', idOffset - 1 - size).Append('=');
        }
        else
            builder.Append(' ', idOffset);
    }
   
    public void Append<T>(T value) where T : Enum
    {
        var name = Enum.GetName(typeof(T), value);
        builder.Append(' ').Append(name);
    }
    public void Append(IdRef id)
    {
        builder.Append(' ').Append('%').Append(id.Value);
    }
    public void Append(IdResultType id)
    {
        builder.Append(' ').Append('%').Append(id.Value);
    }
    public void AppendInt(int v)
    {
        builder.Append(' ').Append(v);
    }
    public void AppendLiteral(LiteralInteger v)
    {
        builder.Append(' ').Append(v.Words);
    }

    public void AppendLiteral(LiteralFloat v)
    {
        if(v.WordCount == 1)
            builder.Append(' ').Append(Convert.ToSingle(v.Words & 0xFFFF));
        if(v.WordCount == 2)
            builder.Append(' ').Append(Convert.ToDouble(v.Words));
    }
    public void AppendLiteral(LiteralString v, bool quoted = false)
    {
        if(!quoted)
            builder.Append(' ').Append(v.Value);
        else
            builder.Append(' ').Append('"').Append(v.Value).Append('"');
    }
    public void Append(PairLiteralIntegerIdRef v)
    {
        (int,int) value = v;
        AppendInt(value.Item1);
        AppendInt(value.Item2);
    }
    public void Append(PairIdRefLiteralInteger v)
    {
        (int,int) value = v;
        AppendInt(value.Item1);
        AppendInt(value.Item2);
    }
    public void Append(PairIdRefIdRef v)
    {
        (int,int) value = v;
        AppendInt(value.Item1);
        AppendInt(value.Item2);
    }
    public void AppendLine() => builder.AppendLine();
    
    public override string ToString()
    {
        return builder.ToString();
    }
}