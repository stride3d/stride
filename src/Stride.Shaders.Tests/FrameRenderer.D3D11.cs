using CommunityToolkit.HighPerformance;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Stride.Shaders;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Stride.Shaders.Parsing.Tests;



public class D3D11FrameRenderer(uint width = 800, uint height = 600, byte[]? fragmentSpirv = null, byte[]? vertexSpirv = null) : FrameRenderer(width, height, vertexSpirv, fragmentSpirv)
{
    static IWindow? window;
    DXGI dxgi = null!;
    D3D11 d3d11 = null!;
    D3DCompiler compiler = null!;

    uint width = width;
    uint height = height;

    ComPtr<IDXGIFactory2> factory = default;
    ComPtr<IDXGISwapChain1> swapchain = default;
    ComPtr<ID3D11Device> device = default;
    ComPtr<ID3D11DeviceContext> deviceContext = default;
    ComPtr<ID3D11Buffer> vertexBuffer = default;
    ComPtr<ID3D11Buffer> indexBuffer = default;
    ComPtr<ID3D11VertexShader> vertexShader = default;
    ComPtr<ID3D11PixelShader> pixelShader = default;
    ComPtr<ID3D11InputLayout> inputLayout = default;

    byte[]? fragmentSpirv = fragmentSpirv;

    //Vertex shaders are run on each vertex.
    public string VertexShaderSource = @"
struct vs_in {
    float3 position_local : POSITION;
    float2 texcoord : TEXCOORD;
};

struct vs_out {
    float2 texcoord : TEXCOORD;
    float4 position_clip : SV_POSITION;
};

vs_out main(vs_in input) {
    vs_out output = (vs_out)0;
    output.position_clip = float4(input.position_local, 1.0);
    output.texcoord = input.texcoord;
    return output;
}
        ";

    //Fragment shaders are run on each fragment/pixel of the geometry.
    public string PixelShaderSource = @"
struct vs_out {
    float4 position_clip : SV_POSITION;
    float2 texcoord : TEXCOORD;
};

float4 main(vs_out input) : SV_TARGET {
    return float4( 1.0, 0.5, 0.2, 1.0 );
}
        ";

    //Vertex data, uploaded to the VBO.
    private static readonly float[] Vertices =
    [
        //X    Y      Z
        1f,  1f, 0f,  1.0f, 1.0f,
        1f, -1f, 0f,  1.0f, 0.0f,
        -1f,-1f, 0f,  0.0f, 0.0f,
        -1f, 1f, 1f,  0.0f, 1.0f,
    ];

    //Index data, uploaded to the EBO.
    private static readonly uint[] Indices =
    [
            0, 1, 3,
            1, 2, 3
    ];

    public EffectReflection EffectReflection { get; set; }

    public override unsafe void RenderFrame(Span<byte> result)
    {
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>((int)width, (int)height);
        options.IsVisible = false;
        options.API = GraphicsAPI.None;
        window = Window.Create(options);
        window.Initialize();

        // Source for most of the code:
        // https://github.com/dotnet/Silk.NET/blob/main/examples/CSharp/Direct3D11%20Tutorials/Tutorial%201.2%20-%20Hello%20quad/Program.cs
        dxgi = DXGI.GetApi(window);
        d3d11 = D3D11.GetApi(window);
        compiler = D3DCompiler.GetApi();

        // Create our D3D11 logical device.
        SilkMarshal.ThrowHResult
        (
            d3d11.CreateDevice
            (
                default(ComPtr<IDXGIAdapter>),
                D3DDriverType.Hardware,
                Software: default,
                (uint)CreateDeviceFlag.Debug,
                null,
                0,
                D3D11.SdkVersion,
                ref device,
                null,
                ref deviceContext
            )
        );

        if (OperatingSystem.IsWindows())
        {
            // Log debug messages for this device (given that we've enabled the debug flag). Don't do this in release code!
            device.SetInfoQueueCallback(msg => Console.WriteLine(SilkMarshal.PtrToString((nint)msg.PDescription)));
        }

        // Create our swapchain.
        var swapChainDesc = new SwapChainDesc1
        {
            BufferCount = 2, // double buffered
            Format = Format.FormatR8G8B8A8Unorm,
            BufferUsage = DXGI.UsageRenderTargetOutput,
            SwapEffect = SwapEffect.FlipDiscard,
            SampleDesc = new SampleDesc(1, 0)
        };

        // Create our DXGI factory to allow us to create a swapchain. 
        factory = dxgi.CreateDXGIFactory<IDXGIFactory2>();

        // Create the swapchain.
        SilkMarshal.ThrowHResult
        (
            factory.CreateSwapChainForHwnd
            (
                device,
                window.Native!.DXHandle!.Value,
                in swapChainDesc,
                null,
                ref Unsafe.NullRef<IDXGIOutput>(),
                ref swapchain
            )
        );

        // Create our vertex buffer.
        var bufferDesc = new BufferDesc
        {
            ByteWidth = (uint)(Vertices.Length * sizeof(float)),
            Usage = Usage.Default,
            BindFlags = (uint)BindFlag.VertexBuffer,
        };

        fixed (float* vertexData = Vertices)
        {
            var subresourceData = new SubresourceData
            {
                PSysMem = vertexData
            };

            SilkMarshal.ThrowHResult(device.CreateBuffer(in bufferDesc, in subresourceData, ref vertexBuffer));
        }

        // Create our index buffer.
        bufferDesc = new BufferDesc
        {
            ByteWidth = (uint)(Indices.Length * sizeof(uint)),
            Usage = Usage.Default,
            BindFlags = (uint)BindFlag.IndexBuffer,
        };

        fixed (uint* indexData = Indices)
        {
            var subresourceData = new SubresourceData
            {
                PSysMem = indexData
            };

            SilkMarshal.ThrowHResult(device.CreateBuffer(in bufferDesc, in subresourceData, ref indexBuffer));
        }

        var vertexShaderBytes = Encoding.ASCII.GetBytes(VertexShaderSource);
        var pixelShaderBytes = Encoding.ASCII.GetBytes(PixelShaderSource);

        // Compile vertex shader.
        ComPtr<ID3D10Blob> vertexCode = default;
        ComPtr<ID3D10Blob> vertexErrors = default;
        HResult hr = compiler.Compile
        (
            in vertexShaderBytes[0],
            (nuint)vertexShaderBytes.Length,
            nameof(VertexShaderSource),
            null,
            ref Unsafe.NullRef<ID3DInclude>(),
            "main",
            "vs_5_0",
            0,
            0,
            ref vertexCode,
            ref vertexErrors
        );

        // Check for compilation errors.
        if (hr.IsFailure)
        {
            if (vertexErrors.Handle is not null)
            {
                Console.WriteLine(SilkMarshal.PtrToString((nint)vertexErrors.GetBufferPointer()));
            }

            hr.Throw();
        }

        // Compile pixel shader.
        ComPtr<ID3D10Blob> pixelCode = default;
        ComPtr<ID3D10Blob> pixelErrors = default;
        hr = compiler.Compile
        (
            in pixelShaderBytes[0],
            (nuint)pixelShaderBytes.Length,
            nameof(PixelShaderSource),
            null,
            ref Unsafe.NullRef<ID3DInclude>(),
            "main",
            "ps_5_0",
            0,
            0,
            ref pixelCode,
            ref pixelErrors
        );

        // Check for compilation errors.
        if (hr.IsFailure)
        {
            if (pixelErrors.Handle is not null)
            {
                Console.WriteLine(SilkMarshal.PtrToString((nint)pixelErrors.GetBufferPointer()));
            }

            hr.Throw();
        }

        // Create vertex shader.
        SilkMarshal.ThrowHResult
        (
            device.CreateVertexShader
            (
                vertexCode.GetBufferPointer(),
                vertexCode.GetBufferSize(),
                ref Unsafe.NullRef<ID3D11ClassLinkage>(),
                ref vertexShader
            )
        );

        // Create pixel shader.
        SilkMarshal.ThrowHResult
        (
            device.CreatePixelShader
            (
                pixelCode.GetBufferPointer(),
                pixelCode.GetBufferSize(),
                ref Unsafe.NullRef<ID3D11ClassLinkage>(),
                ref pixelShader
            )
        );

        // Describe the layout of the input data for the shader.
        fixed (byte* pos = SilkMarshal.StringToMemory("POSITION"))
        fixed (byte* texcoord = SilkMarshal.StringToMemory("TEXCOORD"))
        {
            var inputElements = new List<InputElementDesc>
            {
                new()
                {
                    SemanticName = pos,
                    SemanticIndex = 0,
                    Format = Format.FormatR32G32B32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 0,
                    InputSlotClass = InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                },
                new()
                {
                    SemanticName = texcoord,
                    SemanticIndex = 0, // TEXCOORD0
                    Format = Format.FormatR32G32Float,
                    InputSlot = 0,
                    AlignedByteOffset = uint.MaxValue, // AUTO
                    InputSlotClass = InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                }
            };

            // Keep in memory (even if GC) until call to CreateInputLayout
            var streamSemanticNamesMemory = new List<GlobalMemory>();

            // Start at input slot 1 (0 is standard vertex data)
            uint inputSlot = 1;
            foreach (var parameter in Parameters)
            {
                if (parameter.Key.StartsWith("stream."))
                {
                    var streamSemanticName = parameter.Key.Substring("stream.".Length);

                    var streamSemanticNameMemory = SilkMarshal.StringToMemory(streamSemanticName);
                    streamSemanticNamesMemory.Add(streamSemanticNameMemory);

                    inputElements.Add(new InputElementDesc
                    {
                        SemanticName = (byte*)streamSemanticNameMemory,
                        SemanticIndex = 0,
                        Format = Format.FormatR32G32B32A32Float,
                        InputSlot = inputSlot,
                        AlignedByteOffset = 0,
                        InputSlotClass = InputClassification.PerInstanceData,
                        InstanceDataStepRate = 0,
                    });

                    // Also create the vertex and bind it right away
                    var floatValues = parameter.Value.TrimStart('(').TrimEnd(')').Split(' ', StringSplitOptions.TrimEntries).Select(x => float.Parse(x)).ToArray();
                    bufferDesc = new BufferDesc
                    {
                        ByteWidth = (uint)(sizeof(float) * floatValues.Length), // up to 4 floats
                        Usage = Usage.Default,
                        BindFlags = (uint)BindFlag.VertexBuffer,
                    };

                    ComPtr<ID3D11Buffer> vertexBufferForStream = default;
                    fixed (float* floatValuesPtr = floatValues)
                    {
                        var subresourceData = new SubresourceData
                        {
                            PSysMem = floatValuesPtr
                        };

                        SilkMarshal.ThrowHResult(device.CreateBuffer(in bufferDesc, in subresourceData, ref vertexBufferForStream));
                    }

                    deviceContext.IASetVertexBuffers(inputSlot, 1, vertexBufferForStream, 0, 0);
                    inputSlot++;
                }
            }

            fixed (InputElementDesc* inputElementsPtr = inputElements.AsSpan())
            SilkMarshal.ThrowHResult
            (
                device.CreateInputLayout
                (
                    inputElementsPtr,
                    (uint)inputElements.Count,
                    vertexCode.GetBufferPointer(),
                    vertexCode.GetBufferSize(),
                    ref inputLayout
                )
            );
        }

        // Clean up any resources.
        vertexCode.Dispose();
        vertexErrors.Dispose();
        pixelCode.Dispose();
        pixelErrors.Dispose();

        ComPtr<ID3D11Texture2D> renderTexture = default;
        ComPtr<ID3D11Texture2D> renderTextureStaging = default;

        var textureDesc = new Texture2DDesc
        {
            Width = width,
            Height = height,
            Format = Format.FormatR8G8B8A8Unorm,
            MipLevels = 1,
            BindFlags = (uint)(BindFlag.ShaderResource | BindFlag.RenderTarget),
            Usage = Usage.Default,
            CPUAccessFlags = 0,
            MiscFlags = (uint)ResourceMiscFlag.None,
            SampleDesc = new SampleDesc(1, 0),
            ArraySize = 1
        };

        SilkMarshal.ThrowHResult
        (
            device.CreateTexture2D
            (
                in textureDesc,
                default,
                ref renderTexture
            )
        );

        textureDesc.BindFlags = 0;
        textureDesc.Usage = Usage.Staging;
        textureDesc.CPUAccessFlags = (uint)CpuAccessFlag.Read;

        SilkMarshal.ThrowHResult
        (
            device.CreateTexture2D
            (
                in textureDesc,
                default,
                ref renderTextureStaging
            )
        );

        // Create a view over the render target.
        ComPtr<ID3D11RenderTargetView> renderTargetView = default;
        SilkMarshal.ThrowHResult(device.CreateRenderTargetView(renderTexture, null, ref renderTargetView));

        // Clear the render target to be all black ahead of rendering.
        var backgroundColour = new[] { 0.0f, 0.0f, 0.0f, 1.0f };
        deviceContext.ClearRenderTargetView(renderTargetView, ref backgroundColour[0]);

        // Update the rasterizer state with the current viewport.
        var viewport = new Viewport(0, 0, width, height, 0, 1);
        deviceContext.RSSetViewports(1, in viewport);
        deviceContext.OMSetRenderTargets(1, ref renderTargetView, ref Unsafe.NullRef<ID3D11DepthStencilView>());

        // Update the input assembler to use our shader input layout, and associated vertex & index buffers.
        deviceContext.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
        deviceContext.IASetInputLayout(inputLayout);
        deviceContext.IASetVertexBuffers(0, 1, vertexBuffer, 3 * sizeof(float) + 2 * sizeof(float), 0);
        deviceContext.IASetIndexBuffer(indexBuffer, Format.FormatR32Uint, 0);

        // Bind our shaders.
        deviceContext.VSSetShader(vertexShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);
        deviceContext.PSSetShader(pixelShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);

        foreach (var param in Parameters)
        {
            var dotIndex = param.Key.IndexOf(".");
            if (dotIndex == -1)
                continue;

            var resourceType = param.Key.Substring(0, dotIndex);
            if (resourceType != "cbuffer" && resourceType != "texture" && resourceType != "buffer")
                continue;

            var resourceName = param.Key.Substring(dotIndex + 1);
            var resourceReflection = EffectReflection.ResourceBindings.Single(x => x.RawName == resourceName);

            if (resourceType == "cbuffer")
            {
                var cbReflection = EffectReflection.ConstantBuffers.Single(x => x.Name == resourceName);
                var cbufferData = new byte[cbReflection.Size];
                foreach (var cbufferParameter in TestHeaderParser.ParseParameters(param.Value))
                {
                    var cbMemberReflection = cbReflection.Members.Single(x => x.KeyInfo.KeyName.EndsWith(cbufferParameter.Key));

                    fixed (byte* cbufferDataPtr = cbufferData)
                    {
                        FillData(cbufferParameter.Value, cbMemberReflection.Type, cbMemberReflection.Offset, cbufferDataPtr);
                    }
                }

                // Create cbuffer
                // Create our vertex buffer.
                ComPtr<ID3D11Buffer> cbuffer = default;
                bufferDesc = new BufferDesc
                {
                    ByteWidth = (uint)cbReflection.Size,
                    Usage = Usage.Default,
                    BindFlags = (uint)BindFlag.ConstantBuffer,
                };

                fixed (byte* cbufferDataPtr = cbufferData)
                {
                    var subresourceData = new SubresourceData
                    {
                        PSysMem = cbufferDataPtr
                    };

                    SilkMarshal.ThrowHResult(device.CreateBuffer(in bufferDesc, in subresourceData, ref cbuffer));
                }
                deviceContext.VSSetConstantBuffers((uint)resourceReflection.SlotStart, 1U, &cbuffer.Handle);
                deviceContext.PSSetConstantBuffers((uint)resourceReflection.SlotStart, 1U, &cbuffer.Handle);
            }
            else if (resourceType == "buffer")
            {
                var color = ParseColor(param.Value);

                // Create cbuffer
                // Create our vertex buffer.
                ComPtr<ID3D11Buffer> buffer = default;
                bufferDesc = new BufferDesc
                {
                    ByteWidth = sizeof(uint),
                    Usage = Usage.Default,
                    BindFlags = (uint)BindFlag.ShaderResource,
                    StructureByteStride = sizeof(uint),
                };

                {
                    var subresourceData = new SubresourceData
                    {
                        PSysMem = (byte*)&color,
                    };

                    SilkMarshal.ThrowHResult(device.CreateBuffer(in bufferDesc, in subresourceData, ref buffer));
                }

                // Create a view of the texture for the shader.
                ComPtr<ID3D11ShaderResourceView> bufferSRV = default;
                var srvDesc = new ShaderResourceViewDesc
                {
                    Format = Format.FormatR8G8B8A8Unorm,
                    ViewDimension = D3DSrvDimension.D3DSrvDimensionBuffer,
                    Anonymous = new ShaderResourceViewDescUnion
                    {
                        Buffer = new()
                        {
                            NumElements = 1,
                            FirstElement = 0,
                        }
                    },
                };

                SilkMarshal.ThrowHResult
                (
                    device.CreateShaderResourceView
                    (
                        buffer,
                        in srvDesc,
                        ref bufferSRV
                    )
                );

                deviceContext.VSSetShaderResources((uint)resourceReflection.SlotStart, 1U, &bufferSRV.Handle);
                deviceContext.PSSetShaderResources((uint)resourceReflection.SlotStart, 1U, &bufferSRV.Handle);
            }
            else if (resourceType == "texture")
            {
                var color = ParseColor(param.Value);

                textureDesc = new Texture2DDesc
                {
                    Width = 1,
                    Height = 1,
                    Format = Format.FormatR8G8B8A8Unorm,
                    MipLevels = 1,
                    BindFlags = (uint)BindFlag.ShaderResource,
                    Usage = Usage.Default,
                    CPUAccessFlags = 0,
                    MiscFlags = (uint)ResourceMiscFlag.None,
                    SampleDesc = new SampleDesc(1, 0),
                    ArraySize = 1
                };

                var subresourceData = new SubresourceData
                {
                    PSysMem = &color,
                    SysMemPitch = sizeof(int) * 1,
                    SysMemSlicePitch = sizeof(int) * 1 * 1,
                };

                ComPtr<ID3D11Texture2D> texture = default;
                SilkMarshal.ThrowHResult
                (
                    device.CreateTexture2D
                    (
                        in textureDesc,
                        in subresourceData,
                        ref texture
                    )
                );

                // Create a view of the texture for the shader.
                ComPtr<ID3D11ShaderResourceView> textureSRV = default;
                var srvDesc = new ShaderResourceViewDesc
                {
                    Format = Format.FormatR8G8B8A8Unorm,
                    ViewDimension = D3DSrvDimension.D3DSrvDimensionTexture2D,
                    Anonymous = new ShaderResourceViewDescUnion
                    {
                        Texture2D = new()
                        {
                            MipLevels = 1,
                            MostDetailedMip = 0,
                        }
                    },
                };

                SilkMarshal.ThrowHResult
                (
                    device.CreateShaderResourceView
                    (
                        texture,
                        in srvDesc,
                        ref textureSRV
                    )
                );

                deviceContext.VSSetShaderResources((uint)resourceReflection.SlotStart, 1U, &textureSRV.Handle);
                deviceContext.PSSetShaderResources((uint)resourceReflection.SlotStart, 1U, &textureSRV.Handle);
            }
        }

        // Draw the quad.
        deviceContext.DrawIndexed(6, 0, 0);

        deviceContext.CopyResource(renderTextureStaging, renderTexture);

        MappedSubresource mappedResource = default;
        deviceContext.Map(renderTextureStaging, 0, Map.MapRead, 0, ref mappedResource);
        var span = new Span<byte>(mappedResource.PData, (int)(width * height * 4));
        span.CopyTo(result);
        deviceContext.Unmap(renderTextureStaging, 0);

        // Still do a copy to backbuffer and present, for debugging purpose (i.e. if we run RenderDoc or such debug tools)
        var framebuffer = swapchain.GetBuffer<ID3D11Texture2D>(0);

        deviceContext.CopySubresourceRegion(framebuffer, 0, 0, 0, 0, renderTexture, 0, null);

        // Present the drawn image.
        swapchain.Present(1, 0);

        renderTextureStaging.Dispose();
        renderTexture.Dispose();

        renderTargetView.Dispose();

        framebuffer.Dispose();

        window.Close();
        window.Dispose();

    }

    private static unsafe void FillData(string value, EffectTypeDescription type, int offset, byte* cbufferDataPtr)
    {
        switch (type)
        {
            case { Elements: > 1 }:
                int index = 0;
                var arrayStride = (type.ElementSize + 15) / 16 * 16;
                foreach (var elementValue in TestHeaderParser.SplitArgs(value))
                {
                    FillData(elementValue, type with { Elements = 1 }, offset + arrayStride * index, cbufferDataPtr);
                    index++;
                }
                break;
            case { Class: EffectParameterClass.Struct }:
                var structParameters = TestHeaderParser.ParseParameters(value);
                foreach (var member in type.Members)
                {
                    if (structParameters.TryGetValue(member.Name, out var memberValue))
                        FillData(memberValue, member.Type, offset + member.Offset, cbufferDataPtr);
                }
                break;
            case { Type: EffectParameterType.Int }:
                *((int*)&cbufferDataPtr[offset]) = int.Parse(value);
                break;
            case { Type: EffectParameterType.Float }:
                *((float*)&cbufferDataPtr[offset]) = float.Parse(value);
                break;
            default:
                throw new NotImplementedException();
        }
    }
}