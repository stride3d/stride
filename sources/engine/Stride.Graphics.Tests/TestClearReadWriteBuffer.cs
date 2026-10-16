// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xunit;

using Stride.Core.Mathematics;

namespace Stride.Graphics.Tests
{
    /// <summary>
    /// What <see cref="CommandList.ClearReadWrite(Buffer, UInt4)"/> and its overloads write, per kind of Buffer and format.
    /// </summary>
    public class TestClearReadWriteBuffer : GraphicTestGameBase
    {
        private const int ElementCount = 256;

        public enum ClearCase
        {
            StructuredUInt,
            RawUInt,
            TypedR32UInt,
            TypedR32SInt,
            TypedR32Float,
            TypedR32Float_Bits,
            TypedR8G8B8A8UInt,
            TypedR8G8B8A8UNorm,
            TypedR32G32B32A32UInt,
            TypedR32G32B32A32UInt_SameWords,
            TypedR32G32B32A32Float,
            TypedR16G16Float,
        }

        [SkippableTheory]
        [InlineData(ClearCase.StructuredUInt)]
        [InlineData(ClearCase.RawUInt)]
        [InlineData(ClearCase.TypedR32UInt)]
        [InlineData(ClearCase.TypedR32SInt)]
        [InlineData(ClearCase.TypedR32Float)]
        [InlineData(ClearCase.TypedR32Float_Bits)]
        [InlineData(ClearCase.TypedR8G8B8A8UInt)]
        [InlineData(ClearCase.TypedR8G8B8A8UNorm)]
        [InlineData(ClearCase.TypedR32G32B32A32UInt)]
        [InlineData(ClearCase.TypedR32G32B32A32UInt_SameWords)]
        [InlineData(ClearCase.TypedR32G32B32A32Float)]
        [InlineData(ClearCase.TypedR16G16Float)]
        public void ClearWritesEveryElement(ClearCase clearCase)
        {
            Skip.If(clearCase == ClearCase.RawUInt && GraphicsDevice.Platform == GraphicsPlatform.Vulkan, "Raw buffers can't be created on Vulkan yet (R32_Typeless view).");

            // Vulkan fills a buffer by repeating one 32-bit word: 8 and 16-byte elements with different words throw
            bool fillsWordsOnly = GraphicsDevice.Platform == GraphicsPlatform.Vulkan;

            PerformTest(game =>
            {
                var device = game.GraphicsDevice;
                var commandList = game.GraphicsContext.CommandList;

                switch (clearCase)
                {
                    case ClearCase.StructuredUInt:
                    {
                        // Raw and structured views: X is written to every 32-bit word
                        using var buffer = Buffer.Structured.New<uint>(device, ElementCount, unorderedAccess: true);
                        commandList.ClearReadWrite(buffer, new UInt4(7, 8, 9, 10));
                        AssertWords(buffer.GetData<uint>(commandList), 7u);
                        break;
                    }
                    case ClearCase.RawUInt:
                    {
                        using var buffer = Buffer.Raw.New(device, ElementCount * sizeof(uint), BufferFlags.UnorderedAccess);
                        commandList.ClearReadWrite(buffer, new UInt4(0xDEADBEEF, 1, 2, 3));
                        AssertWords(buffer.GetData<uint>(commandList), 0xDEADBEEF);
                        break;
                    }
                    case ClearCase.TypedR32UInt:
                    {
                        using var buffer = Buffer.Typed.New(device, ElementCount, PixelFormat.R32_UInt, unorderedAccess: true);
                        commandList.ClearReadWrite(buffer, new UInt4(5, 6, 7, 8));
                        AssertWords(buffer.GetData<uint>(commandList), 5u);
                        break;
                    }
                    case ClearCase.TypedR32SInt:
                    {
                        using var buffer = Buffer.Typed.New(device, ElementCount, PixelFormat.R32_SInt, unorderedAccess: true);
                        commandList.ClearReadWrite(buffer, new Int4(-2, 0, 0, 0));
                        AssertWords(buffer.GetData<int>(commandList), -2);
                        break;
                    }
                    case ClearCase.TypedR32Float:
                    {
                        using var buffer = Buffer.Typed.New(device, ElementCount, PixelFormat.R32_Float, unorderedAccess: true);
                        commandList.ClearReadWrite(buffer, new Vector4(1.5f, 0, 0, 0));
                        AssertWords(buffer.GetData<float>(commandList), 1.5f);
                        break;
                    }
                    case ClearCase.TypedR32Float_Bits:
                    {
                        // The UInt overloads copy bits, they don't convert
                        using var buffer = Buffer.Typed.New(device, ElementCount, PixelFormat.R32_Float, unorderedAccess: true);
                        commandList.ClearReadWrite(buffer, new UInt4(0x3FC00000, 0, 0, 0));
                        AssertWords(buffer.GetData<float>(commandList), 1.5f);
                        break;
                    }
                    case ClearCase.TypedR8G8B8A8UInt:
                    {
                        using var buffer = Buffer.Typed.New(device, ElementCount, PixelFormat.R8G8B8A8_UInt, unorderedAccess: true);
                        commandList.ClearReadWrite(buffer, new UInt4(1, 2, 3, 4));
                        AssertWords(buffer.GetData<uint>(commandList), 0x04030201u);
                        break;
                    }
                    case ClearCase.TypedR8G8B8A8UNorm:
                    {
                        // The float overload converts: 0.2 * 255 = 51 = 0x33
                        using var buffer = Buffer.Typed.New(device, ElementCount, PixelFormat.R8G8B8A8_UNorm, unorderedAccess: true);
                        commandList.ClearReadWrite(buffer, new Vector4(1, 0.2f, 0, 1));
                        AssertWords(buffer.GetData<uint>(commandList), 0xFF0033FFu);
                        break;
                    }
                    case ClearCase.TypedR32G32B32A32UInt:
                    {
                        using var buffer = Buffer.Typed.New(device, ElementCount, PixelFormat.R32G32B32A32_UInt, unorderedAccess: true);
                        if (fillsWordsOnly)
                        {
                            Assert.Throws<NotSupportedException>(() => commandList.ClearReadWrite(buffer, new UInt4(1, 2, 3, 4)));
                            break;
                        }
                        commandList.ClearReadWrite(buffer, new UInt4(1, 2, 3, 4));
                        AssertWords(buffer.GetData<UInt4>(commandList), new UInt4(1, 2, 3, 4));
                        break;
                    }
                    case ClearCase.TypedR32G32B32A32UInt_SameWords:
                    {
                        using var buffer = Buffer.Typed.New(device, ElementCount, PixelFormat.R32G32B32A32_UInt, unorderedAccess: true);
                        commandList.ClearReadWrite(buffer, new UInt4(5, 5, 5, 5));
                        AssertWords(buffer.GetData<UInt4>(commandList), new UInt4(5, 5, 5, 5));
                        break;
                    }
                    case ClearCase.TypedR32G32B32A32Float:
                    {
                        using var buffer = Buffer.Typed.New(device, ElementCount, PixelFormat.R32G32B32A32_Float, unorderedAccess: true);
                        if (fillsWordsOnly)
                        {
                            Assert.Throws<NotSupportedException>(() => commandList.ClearReadWrite(buffer, new Vector4(1, 2, 3, 4)));
                            break;
                        }
                        commandList.ClearReadWrite(buffer, new Vector4(1, 2, 3, 4));
                        AssertWords(buffer.GetData<Vector4>(commandList), new Vector4(1, 2, 3, 4));
                        break;
                    }
                    case ClearCase.TypedR16G16Float:
                    {
                        using var buffer = Buffer.Typed.New(device, ElementCount, PixelFormat.R16G16_Float, unorderedAccess: true);
                        commandList.ClearReadWrite(buffer, new Vector4(1, 2, 0, 0));
                        // Half 1.0 = 0x3C00, half 2.0 = 0x4000
                        AssertWords(buffer.GetData<uint>(commandList), 0x40003C00u);
                        break;
                    }
                }
            });
        }

        private static void AssertWords<T>(T[] values, T expected) where T : unmanaged
        {
            Assert.Equal(ElementCount, values.Length);
            for (int i = 0; i < values.Length; i++)
                Assert.Equal(expected, values[i]);
        }
    }
}
