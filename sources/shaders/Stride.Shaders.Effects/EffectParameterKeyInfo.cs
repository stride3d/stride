// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;

using Stride.Core;
using Stride.Rendering;

namespace Stride.Shaders;

/// <summary>
///   Contains information about a key identifying an Effect / Shader parameter.
/// </summary>
[DataContract]
[DebuggerDisplay("{Key} ({KeyName})")]
public struct EffectParameterKeyInfo
{
    /// <summary>
    ///   The key that identifies the Effect / Shader parameter.
    /// </summary>
    [DataMemberIgnore]
    public ParameterKey Key;

    /// <summary>
    ///   The name of the Effect / Shader parameter.
    /// </summary>
    public string KeyName;
}
