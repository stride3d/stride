// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Engine.Network;

namespace Stride.ConnectionRouter
{
    public enum RouterMessage : ushort
    {
        ClientRequestServer = ClientRouterMessage.RequestServer, // ClientRequestServer <string:url>
        ClientServerStarted = ClientRouterMessage.ServerStarted, // ClientServerStarted <int:errorcode> <string:message optional(if errorcode != 0)>

        ServiceProvideServer = 0x1000, // ProvideServer <string:url>       
        ServiceRequestServer = 0x1001, // RequestServer <string:url> <guid:token>
        TaskProvideServer = 0x1002, // ProvideServer <string:url>

        ServerStarted = 0x2000, // ServerStarted <guid:token> <varint:errorcode> <string:message optional(if errorcode != 0)>
    }
}
