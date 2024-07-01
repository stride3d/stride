// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.BepuPhysics.Definitions.Contacts;

[DataContract]
public struct FilterByDistance
{
    /// <summary>
    /// 0 == Feature disabled
    /// </summary>
    public ushort Id;
    /// <summary>
    /// Collision occurs if delta > 1
    /// </summary>
    public ushort XAxis;
    /// <summary>
    /// Collision occurs if delta > 1
    /// </summary>
    public ushort YAxis;
    /// <summary>
    /// Collision occurs if delta > 1
    /// </summary>
    public ushort ZAxis;
}