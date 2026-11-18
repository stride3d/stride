// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Numerics;
using System.Reflection;
using Stride.Core;
using Stride.Core.Reflection;

// The names of the math types in asset files and in animation paths
[assembly: DataContractAlias("float2", typeof(Vector2))]
[assembly: DataContractAlias("float3", typeof(Vector3))]
[assembly: DataContractAlias("float4", typeof(Vector4))]
[assembly: DataContractAlias("quaternion", typeof(Quaternion))]
[assembly: DataContractAlias("float4x4", typeof(Matrix4x4))]

namespace Stride.Core.Mathematics;

/// <summary>
/// Module initializer.
/// </summary>
internal class Module
{
    /// <summary>
    /// Module initializer.
    /// </summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        AssemblyRegistry.Register(typeof(Module).GetTypeInfo().Assembly, AssemblyCommonCategories.Assets);
    }
}
