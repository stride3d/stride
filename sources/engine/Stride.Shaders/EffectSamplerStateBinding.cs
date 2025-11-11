// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Rendering;
using Stride.Graphics;

namespace Stride.Shaders;

[DataContract]
public class EffectSamplerStateBinding
{
    /// <summary>
    /// Binding to a sampler.
    /// </summary>
    [DataMemberIgnore]
    public ParameterKey Key;

    public string KeyName;

    public SamplerStateDescription Description;


    public EffectSamplerStateBinding() { }

    public EffectSamplerStateBinding(string keyName, SamplerStateDescription description)
    {
        KeyName = keyName;
        Description = description;
    }


    /// <inheritdoc/>
    public override string ToString()
    {
        return $"SamplerState {Key?.Name ?? KeyName} ({Description.Filter})";
    }
}
