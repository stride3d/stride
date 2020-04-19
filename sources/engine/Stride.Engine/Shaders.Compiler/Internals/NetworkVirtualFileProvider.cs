// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable SA1402 // File may only contain a single class
using System;
using System.IO;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Engine.Network;

namespace Stride.Shaders.Compiler.Internals
{
    internal class DownloadFileQuery : SocketMessage
    {
        public string Url { get; set; }
    }

    internal class FileExistsQuery : SocketMessage
    {
        public string Url { get; set; }
    }

    internal class FileExistsAnswer : SocketMessage
    {
        public bool FileExists { get; set; }
    }

    internal class DownloadFileAnswer : SocketMessage
    {
        public byte[] Data { get; set; }
    }

    internal class UploadFilePacket
    {
        public string Url { get; set; }
        public byte[] Data { get; set; }
    }

    public class NetworkVirtualFileProvider : VirtualFileProviderBase
    {
        private SocketMessageLayer socketMessageLayer;

        public NetworkVirtualFileProvider(SocketMessageLayer socketMessageLayer, string remoteUrl) : base(null)
        {
            this.socketMessageLayer = socketMessageLayer;
            RemoteUrl = remoteUrl;
            if (!RemoteUrl.EndsWith(VirtualFileSystem.DirectorySeparatorChar.ToString()))
                RemoteUrl += VirtualFileSystem.DirectorySeparatorChar;
        }

        public string RemoteUrl { get; private set; }

        public static void RegisterServer(SocketMessageLayer socketMessageLayer)
        {
            socketMessageLayer.AddPacketHandler<DownloadFileQuery>(
                async (packet) =>
                {
                    var stream = await VirtualFileSystem.OpenStreamAsync(packet.Url, VirtualFileMode.Open, VirtualFileAccess.Read);
                    var data = new byte[stream.Length];
                    await stream.ReadAsync(data, 0, data.Length);
                    stream.Dispose();
                    socketMessageLayer.Send(new DownloadFileAnswer { StreamId = packet.StreamId, Data = data });
                });

            socketMessageLayer.AddPacketHandler<UploadFilePacket>(
                async (packet) =>
                {
                    var stream = await VirtualFileSystem.OpenStreamAsync(packet.Url, VirtualFileMode.Create, VirtualFileAccess.Write);
                    await stream.WriteAsync(packet.Data, 0, packet.Data.Length);
                    stream.Dispose();
                });

            socketMessageLayer.AddPacketHandler<FileExistsQuery>(
                async (packet) =>
                    {
                        var fileExists = await VirtualFileSystem.FileExistsAsync(packet.Url);
                        socketMessageLayer.Send(new FileExistsAnswer { StreamId = packet.StreamId, FileExists = fileExists });
                    });
        }

        public override string GetAbsolutePath(string path)
        {
            return RemoteUrl + path;
        }

        public override Stream OpenStream(string url, VirtualFileMode mode, VirtualFileAccess access, VirtualFileShare share = VirtualFileShare.Read, StreamFlags streamFlags = StreamFlags.None)
        {
            switch (access)
            {
                case VirtualFileAccess.Write:
                    return new NetworkWriteStream(socketMessageLayer, RemoteUrl + url);
                case VirtualFileAccess.Read:
                    var downloadFileAnswer = (DownloadFileAnswer)socketMessageLayer.SendReceiveAsync(new DownloadFileQuery { Url = RemoteUrl + url }).Result;
                    return new MemoryStream(downloadFileAnswer.Data);
                default:
                    throw new NotSupportedException();
            }
        }

        public override bool FileExists(string url)
        {
            var fileExistsAnswer = (FileExistsAnswer)socketMessageLayer.SendReceiveAsync(new FileExistsQuery { Url = RemoteUrl + url }).Result;
            return fileExistsAnswer.FileExists;
        }

        internal class NetworkWriteStream : VirtualFileStream
        {
            private string url;
            private SocketMessageLayer socketMessageLayer;
            private MemoryStream memoryStream;

            public NetworkWriteStream(SocketMessageLayer socketMessageLayer, string url)
                : base(new MemoryStream())
            {
                this.memoryStream = (MemoryStream)InternalStream;
                this.url = url;
                this.socketMessageLayer = socketMessageLayer;
            }

            protected override void Dispose(bool disposing)
            {
                socketMessageLayer.Send(new UploadFilePacket { Url = url, Data = memoryStream.ToArray() });
                base.Dispose(disposing);
            }
        }
    }
}
