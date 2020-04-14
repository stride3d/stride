// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Stride.Engine.Network
{
    public static class SocketExtensions
    {
        public static async Task ReadAllAsync(this Stream socket, byte[] buffer, int offset, int size)
        {
            while (size > 0)
            {
                int read = await socket.ReadAsync(buffer, offset, size).ConfigureAwait(false);
                if (read == 0)
                    throw new IOException("Socket closed");
                size -= read;
                offset += read;
            }
        }

        public static async Task WriteInt32Async(this Stream socket, int value)
        {
            var buffer = BitConverter.GetBytes(value);
            await socket.WriteAsync(buffer, 0, sizeof(int)).ConfigureAwait(false);
        }

        public static async Task<int> ReadInt32Async(this Stream socket)
        {
            var buffer = new byte[sizeof(int)];
            await socket.ReadAllAsync(buffer, 0, sizeof(int)).ConfigureAwait(false);
            return BitConverter.ToInt32(buffer, 0);
        }

        public static async Task WriteInt16Async(this Stream socket, short value)
        {
            var buffer = BitConverter.GetBytes(value);
            await socket.WriteAsync(buffer, 0, sizeof(short)).ConfigureAwait(false);
        }

        public static async Task<short> ReadInt16Async(this Stream socket)
        {
            var buffer = new byte[sizeof(short)];
            await socket.ReadAllAsync(buffer, 0, sizeof(short)).ConfigureAwait(false);
            return BitConverter.ToInt16(buffer, 0);
        }

        public static async Task Write7BitEncodedInt(this Stream socket, int value)
        {
            var buffer = new byte[5];
            int bufferLength = 0;

            uint v = (uint)value;   // support negative numbers
            while (v >= 0x80)
            {
                buffer[bufferLength++] = (byte)(v | 0x80);
                v >>= 7;
            }
            buffer[bufferLength++] = (byte)v;

            await socket.WriteAsync(buffer, 0, bufferLength).ConfigureAwait(false);
        }

        public static async Task<int> Read7BitEncodedInt(this Stream socket)
        {
            var buffer = new byte[1];

            // Read out an Int32 7 bits at a time.  The high bit 
            // of the byte when on means to continue reading more bytes.
            int count = 0;
            int shift = 0;
            byte b;
            do
            {
                // Check for a corrupted stream.  Read a max of 5 bytes. 
                // In a future version, add a DataFormatException. 
                if (shift == 5 * 7) // 5 bytes max per Int32, shift += 7
                    throw new FormatException("Bad string length. 7bit Int32 format");

                // ReadByte handles end of stream cases for us.
                if (await socket.ReadAsync(buffer, 0, 1).ConfigureAwait(false) != 1)
                    throw new IOException("Socket closed");

                b = buffer[0];
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }

        public static async Task WriteStringAsync(this Stream socket, string value)
        {
            var buffer = System.Text.Encoding.UTF8.GetBytes(value);
            await Write7BitEncodedInt(socket, buffer.Length).ConfigureAwait(false);
            await socket.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
        }

        public static async Task<string> ReadStringAsync(this Stream socket)
        {
            var bufferSize = await Read7BitEncodedInt(socket);
            var buffer = new byte[bufferSize];
            await ReadAllAsync(socket, buffer, 0, buffer.Length).ConfigureAwait(false);

            return System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        }

        public static async Task WriteGuidAsync(this Stream socket, Guid guid)
        {
            var guidBuffer = guid.ToByteArray();
            await socket.WriteAsync(guidBuffer, 0, guidBuffer.Length).ConfigureAwait(false);
        }

        public static async Task<Guid> ReadGuidAsync(this Stream socket)
        {
            var guidBuffer = new byte[16];
            await socket.ReadAllAsync(guidBuffer, 0, guidBuffer.Length).ConfigureAwait(false);
            return new Guid(guidBuffer);
        }
    }
}
