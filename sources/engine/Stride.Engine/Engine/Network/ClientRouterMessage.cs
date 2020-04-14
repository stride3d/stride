// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Engine.Network
{
    /// <summary>
    /// Message exchanged between client and router.
    /// Note: shouldn't collide with <see cref="RouterMessage"/>.
    /// </summary>
    public enum ClientRouterMessage : ushort
    {
        RequestServer = 0x0000, // ClientRequestServer <string:url>
        ServerStarted = 0x0001, // ClientServerStarted <int:errorcode> <string:message>
    }
}
