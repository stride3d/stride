// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Data;

namespace Stride.Engine.Mcp
{
    [DataContract]
    [Display("MCP Server")]
    public class McpSettings : Configuration
    {
        [DataMember(10)]
        [Display("Enable MCP Server")]
        public bool Enabled { get; set; } = false;

        [DataMember(20)]
        [Display("Port")]
        public int Port { get; set; } = 5272;
    }
}
