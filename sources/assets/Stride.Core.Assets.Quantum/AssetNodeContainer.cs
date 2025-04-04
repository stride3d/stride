// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Quantum;
using IReference = Stride.Core.Serialization.Contents.IReference;

namespace Stride.Core.Assets.Quantum;

public class AssetNodeContainer : NodeContainer, IPrimitiveTypeFilter
{
    private static readonly Type[] BasePrimitiveTypes =
    [
        typeof(IReference),
        typeof(PropertyKey),
        typeof(TimeSpan),
        typeof(DateTime),
        typeof(Guid),
        typeof(AssetId),
        typeof(Color),
        typeof(Color3),
        typeof(Color4),
        typeof(Vector2),
        typeof(Vector3),
        typeof(Vector4),
        typeof(Int2),
        typeof(Int3),
        typeof(Int4),
        typeof(Quaternion),
        typeof(RectangleF),
        typeof(Rectangle),
        typeof(Matrix),
        typeof(UPath),
        typeof(AngleSingle)
    ];
    
    public AssetNodeContainer()
    {
        NodeBuilder.PrimitiveTypeFilter = this;
    }
    
    public virtual bool IsPrimitiveType(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlyingType)
            type = underlyingType;

        return type.IsPrimitive || type.IsEnum || BasePrimitiveTypes.Any(x => x.IsAssignableFrom(type)) || AssetRegistry.IsExactContentType(type);
    }
}
