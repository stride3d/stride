// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Stride.Graphics;

namespace Stride.Engine.Mcp.Tools
{
    [McpServerToolType]
    public sealed class CaptureScreenshotTool
    {
        [McpServerTool(Name = "capture_screenshot"), Description("Captures a screenshot of the game's back buffer and returns it as a base64-encoded PNG image.")]
        public static async Task<IEnumerable<ContentBlock>> CaptureScreenshot(
            GameBridge bridge,
            CancellationToken cancellationToken = default)
        {
            var base64 = await bridge.RunOnGameThread(game =>
            {
                var backBuffer = game.GraphicsDevice?.Presenter?.BackBuffer;
                if (backBuffer == null)
                    return null;

                using var image = backBuffer.GetDataAsImage(game.GraphicsContext.CommandList);
                using var memoryStream = new MemoryStream();
                image.Save(memoryStream, ImageFileType.Png);
                return Convert.ToBase64String(memoryStream.ToArray());
            }, cancellationToken);

            if (base64 == null)
            {
                return new ContentBlock[]
                {
                    new TextContentBlock { Text = "Error: Back buffer is not available" },
                };
            }

            return new ContentBlock[]
            {
                new ImageContentBlock
                {
                    Data = base64,
                    MimeType = "image/png",
                },
            };
        }
    }
}
