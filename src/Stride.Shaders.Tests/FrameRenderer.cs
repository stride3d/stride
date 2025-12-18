using System.Globalization;

namespace Stride.Shaders.Parsing.Tests;

public abstract class FrameRenderer(uint width = 800, uint height = 600, byte[]? vertexSpirv = null, byte[]? fragmentSpirv = null)
{
    uint width = width;
    uint height = height;
    byte[]? vertexSpirv = vertexSpirv;
    byte[]? fragmentSpirv = fragmentSpirv;

    public Dictionary<string, string> Parameters { get; } = new();

    protected static unsafe void FillCBufferData(string value, EffectTypeDescription type, int offset, byte* cbufferDataPtr)
    {
        switch (type)
        {
            case { Elements: > 1 }:
                int index = 0;
                var arrayStride = (type.ElementSize + 15) / 16 * 16;
                foreach (var elementValue in TestHeaderParser.SplitArgs(value))
                {
                    FillCBufferData(elementValue, type with { Elements = 1 }, offset + arrayStride * index, cbufferDataPtr);
                    index++;
                }
                break;
            case { Class: EffectParameterClass.Struct }:
                var structParameters = TestHeaderParser.ParseParameters(value);
                foreach (var member in type.Members)
                {
                    if (structParameters.TryGetValue(member.Name, out var memberValue))
                        FillCBufferData(memberValue, member.Type, offset + member.Offset, cbufferDataPtr);
                }
                break;
            case { Type: EffectParameterType.Int }:
                *((int*)&cbufferDataPtr[offset]) = int.Parse(value);
                break;
            case { Type: EffectParameterType.Float }:
                *((float*)&cbufferDataPtr[offset]) = float.Parse(value);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    protected static unsafe uint ParseColor(string value)
    {
        if (!value.StartsWith("#"))
            throw new NotSupportedException();

        var hexColor = value.Substring(1);
        uint color = uint.Parse(hexColor.Substring(0, 8), NumberStyles.HexNumber);
        color = (((color << 24) & 0xff000000) |
            ((color << 8) & 0xff0000) |
            ((color >> 8) & 0xff00) |
            ((color >> 24) & 0xff));
        return color;
    }

    public abstract void RenderFrame(Span<byte> bytes);
}