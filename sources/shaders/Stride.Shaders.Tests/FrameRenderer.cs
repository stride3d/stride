using System.Globalization;

namespace Stride.Shaders.Parsers.Tests;

public abstract class FrameRenderer(uint width = 800, uint height = 600)
{
    protected uint width = width;
    protected uint height = height;

    public Dictionary<string, string> Parameters { get; } = new();

    public EffectReflection EffectReflection { get; set; }

    public abstract void SetupTest();

    public abstract void RenderFrame(Span<byte> result);

    public abstract void Compute();

    public abstract void PresentAndFinish();

    /// <summary>
    /// Builds the byte contents of a cbuffer from a "cbuffer.Name=(Member=Value ...)" test parameter,
    /// using the reflection layout.
    /// </summary>
    protected unsafe byte[] BuildCBufferData(string resourceName, string value)
    {
        var cbReflection = FindCBuffer(resourceName);
        var cbufferData = new byte[cbReflection.Size];
        foreach (var cbufferParameter in TestHeaderParser.ParseParameters(value))
        {
            var cbMemberReflection = cbReflection.Members.Single(x => x.KeyInfo.KeyName.EndsWith(cbufferParameter.Key));

            fixed (byte* cbufferDataPtr = cbufferData)
            {
                FillData(cbufferParameter.Value, cbMemberReflection.Type, cbMemberReflection.Offset, cbufferDataPtr);
            }
        }
        return cbufferData;
    }

    protected EffectConstantBufferDescription FindCBuffer(string name)
    {
        foreach (var group in EffectReflection.ResourceGroups)
            if (group.ConstantBuffer?.Name == name)
                return group.ConstantBuffer;
        return EffectReflection.ConstantBuffers.Single(x => x.Name == name);
    }

    /// <summary>
    /// Matches "cbuffer.X", "texture.X" and "buffer.X" test parameters to their reflection resource bindings.
    /// </summary>
    protected IEnumerable<(EffectResourceBindingDescription Binding, string ResourceType, string Value)> MatchResourceParameters()
    {
        foreach (var parameter in Parameters)
        {
            var dotIndex = parameter.Key.IndexOf('.');
            if (dotIndex == -1)
                continue;
            var resourceType = parameter.Key.Substring(0, dotIndex);
            if (resourceType is not ("cbuffer" or "texture" or "buffer"))
                continue;
            var resourceName = parameter.Key.Substring(dotIndex + 1);
            var binding = EffectReflection.ResourceBindings.Single(x => x.KeyInfo.KeyName.EndsWith(resourceName));
            yield return (binding, resourceType, parameter.Value);
        }
    }

    protected static unsafe void FillData(string value, EffectTypeDescription type, int offset, byte* cbufferDataPtr)
    {
        switch (type)
        {
            case { Elements: > 1 }:
                int index = 0;
                var arrayStride = (type.ElementSize + 15) / 16 * 16;
                foreach (var elementValue in TestHeaderParser.SplitArgs(value))
                {
                    FillData(elementValue, type with { Elements = 1 }, offset + arrayStride * index, cbufferDataPtr);
                    index++;
                }
                break;
            case { Class: EffectParameterClass.Struct }:
                var structParameters = TestHeaderParser.ParseParameters(value);
                foreach (var member in type.Members)
                {
                    if (structParameters.TryGetValue(member.Name, out var memberValue))
                        FillData(memberValue, member.Type, offset + member.Offset, cbufferDataPtr);
                }
                break;
            case { Class: EffectParameterClass.Vector }:
                int compIndex = 0;
                foreach (var comp in TestHeaderParser.SplitArgs(value))
                {
                    if (type.Type == EffectParameterType.Float)
                        *((float*)&cbufferDataPtr[offset + compIndex * sizeof(float)]) = float.Parse(comp, NumberStyles.Float, CultureInfo.InvariantCulture);
                    else if (type.Type == EffectParameterType.Int)
                        *((int*)&cbufferDataPtr[offset + compIndex * sizeof(int)]) = int.Parse(comp, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    compIndex++;
                }
                break;
            case { Type: EffectParameterType.Int }:
                *((int*)&cbufferDataPtr[offset]) = int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                break;
            case { Type: EffectParameterType.Float }:
                *((float*)&cbufferDataPtr[offset]) = float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    protected static uint ParseColor(string value)
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
}
