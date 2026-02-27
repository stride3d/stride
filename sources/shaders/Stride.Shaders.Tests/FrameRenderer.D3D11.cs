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

namespace Stride.Shaders.Parsers.Tests;



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
    ComPtr<ID3D11GeometryShader> geometryShader = default;
    ComPtr<ID3D11HullShader> hullShader = default;
    ComPtr<ID3D11DomainShader> domainShader = default;
    ComPtr<ID3D11PixelShader> pixelShader = default;
    ComPtr<ID3D11ComputeShader> computeShader = default;
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

    public string? ComputeShaderSource;

    public string? GeometryShaderSource;

    public string? HullShaderSource;

    public string? DomainShaderSource;

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

    private CancellationTokenSource cts;

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

    public unsafe ComPtr<ID3D10Blob> CompileShader(string shaderModel, string source)
    {
        ComPtr<ID3D10Blob> code = default;
        ComPtr<ID3D10Blob> errors = default;
        var sourceBytes = Encoding.ASCII.GetBytes(source);

        // Compile shader
        HResult hr = compiler.Compile
        (
            in sourceBytes[0],
            (nuint)sourceBytes.Length,
            nameof(source),
            null,
            ref Unsafe.NullRef<ID3DInclude>(),
            "main",
            shaderModel,
            0,
            0,
            ref code,
            ref errors
        );

        // Check for compilation errors.
        if (hr.IsFailure)
        {
            if (errors.Handle is not null)
            {
                Console.WriteLine(SilkMarshal.PtrToString((nint)errors.GetBufferPointer()));
            }

            hr.Throw();
        }

        errors.Dispose();

        return code;
    }

    public unsafe void SetupTest()
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

        cts = new CancellationTokenSource();
        if (OperatingSystem.IsWindows())
        {
            // Log debug messages for this device (given that we've enabled the debug flag). Don't do this in release code!
            device.SetInfoQueueCallback(msg => Console.WriteLine(SilkMarshal.PtrToString((nint)msg.PDescription)), cts.Token);
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
    }

    public void PresentAndFinish()
    {
        // Present the drawn image.
        swapchain.Present(1, 0);

        cts.Cancel();
        cts.Dispose();

        window.Close();
        window.Dispose();
    }

    public unsafe void Compute()
    {
        ComPtr<ID3D10Blob> computeCode = CompileShader("cs_5_0", ComputeShaderSource);

        // Create vertex shader.
        SilkMarshal.ThrowHResult
        (
            device.CreateComputeShader
            (
                computeCode.GetBufferPointer(),
                computeCode.GetBufferSize(),
                ref Unsafe.NullRef<ID3D11ClassLinkage>(),
                ref computeShader
            )
        );

        deviceContext.CSSetShader(computeShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);

        ApplyParameters();

        deviceContext.Dispatch(32, 32, 1);

        computeCode.Dispose();
    }

    private unsafe void CompileAndSetupPipeline(
        out ComPtr<ID3D10Blob> vertexCode,
        out ComPtr<ID3D10Blob> geometryCode,
        out ComPtr<ID3D10Blob> hullCode,
        out ComPtr<ID3D10Blob> domainCode)
    {
        // Compile shaders.
        vertexCode = CompileShader("vs_5_0", VertexShaderSource);
        geometryCode = GeometryShaderSource != null ? CompileShader("gs_5_0", GeometryShaderSource) : default;
        hullCode = HullShaderSource != null ? CompileShader("hs_5_0", HullShaderSource) : default;
        domainCode = DomainShaderSource != null ? CompileShader("ds_5_0", DomainShaderSource) : default;

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

        // Create hull shader.
        if (hullCode.Handle != null)
        {
            SilkMarshal.ThrowHResult
            (
                device.CreateHullShader
                (
                    hullCode.GetBufferPointer(),
                    hullCode.GetBufferSize(),
                    ref Unsafe.NullRef<ID3D11ClassLinkage>(),
                    ref hullShader
                )
            );
        }

        // Create domain shader.
        if (domainCode.Handle != null)
        {
            SilkMarshal.ThrowHResult
            (
                device.CreateDomainShader
                (
                    domainCode.GetBufferPointer(),
                    domainCode.GetBufferSize(),
                    ref Unsafe.NullRef<ID3D11ClassLinkage>(),
                    ref domainShader
                )
            );
        }
    }

    private unsafe void SetupInputAssemblerState(ComPtr<ID3D10Blob> vertexCode)
    {
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
            BufferDesc bufferDesc;
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

        // Update the input assembler to use our shader input layout, and associated vertex & index buffers.
        var topology = hullShader.Handle != null
            ? D3DPrimitiveTopology.D3DPrimitiveTopology3ControlPointPatchlist
            : D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist;
        deviceContext.IASetPrimitiveTopology(topology);
        deviceContext.IASetInputLayout(inputLayout);
        deviceContext.IASetVertexBuffers(0, 1, vertexBuffer, 3 * sizeof(float) + 2 * sizeof(float), 0);
        deviceContext.IASetIndexBuffer(indexBuffer, Format.FormatR32Uint, 0);

        // Bind base shaders.
        deviceContext.VSSetShader(vertexShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);
        if (hullShader.Handle != null)
            deviceContext.HSSetShader(hullShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);
        if (domainShader.Handle != null)
            deviceContext.DSSetShader(domainShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);
    }

    public unsafe void RenderFrame(Span<byte> result)
    {
        CompileAndSetupPipeline(out var vertexCode, out var geometryCode, out var hullCode, out var domainCode);

        // Create geometry shader.
        if (geometryCode.Handle != null)
        {
            SilkMarshal.ThrowHResult
            (
                device.CreateGeometryShader
                (
                    geometryCode.GetBufferPointer(),
                    geometryCode.GetBufferSize(),
                    ref Unsafe.NullRef<ID3D11ClassLinkage>(),
                    ref geometryShader
                )
            );
        }

        // Create pixel shader.
        ComPtr<ID3D10Blob> pixelCode = CompileShader("ps_5_0", PixelShaderSource);
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

        SetupInputAssemblerState(vertexCode);

        // Bind GS and PS.
        if (geometryShader.Handle != null)
            deviceContext.GSSetShader(geometryShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);
        deviceContext.PSSetShader(pixelShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);

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

        ApplyParameters();

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

        renderTextureStaging.Dispose();
        renderTexture.Dispose();

        renderTargetView.Dispose();

        framebuffer.Dispose();

        vertexCode.Dispose();
        if (geometryCode.Handle != null) geometryCode.Dispose();
        if (hullCode.Handle != null) hullCode.Dispose();
        if (domainCode.Handle != null) domainCode.Dispose();
        pixelCode.Dispose();
    }

    public unsafe void RenderFrameWithStreamOutput(out byte[] soData, out int soVertexCount)
    {
        CompileAndSetupPipeline(out var vertexCode, out var geometryCode, out var hullCode, out var domainCode);

        // Determine which bytecode to use for SO declarations: GS if present, else DS, else VS
        ComPtr<ID3D10Blob> soStageCode = geometryCode.Handle != null ? geometryCode
            : domainCode.Handle != null ? domainCode
            : vertexCode;

        // Reflect on the SO stage to get output parameter descriptions
        ComPtr<ID3D11ShaderReflection> soReflection = default;
        SilkMarshal.ThrowHResult(
            compiler.Reflect(
                soStageCode.GetBufferPointer(),
                soStageCode.GetBufferSize(),
                out soReflection
            )
        );

        ShaderDesc soShaderDesc = default;
        soReflection.GetDesc(ref soShaderDesc);

        var soEntries = new List<SODeclarationEntry>();
        var semanticNameMemories = new List<GlobalMemory>();
        uint soStride = 0;

        for (uint i = 0; i < soShaderDesc.OutputParameters; i++)
        {
            SignatureParameterDesc paramDesc = default;
            soReflection.GetOutputParameterDesc(i, ref paramDesc);

            var semanticName = SilkMarshal.PtrToString((nint)paramDesc.SemanticName);
            // Skip system-value semantics like SV_Position
            if (semanticName.StartsWith("SV_", StringComparison.OrdinalIgnoreCase))
                continue;

            // Count the number of components used from the mask
            byte componentCount = 0;
            var mask = paramDesc.Mask;
            while (mask != 0) { componentCount += (byte)(mask & 1); mask >>= 1; }

            var nameMemory = SilkMarshal.StringToMemory(semanticName);
            semanticNameMemories.Add(nameMemory);

            soEntries.Add(new SODeclarationEntry
            {
                Stream = (uint)paramDesc.Stream,
                SemanticName = (byte*)nameMemory,
                SemanticIndex = (uint)paramDesc.SemanticIndex,
                StartComponent = 0,
                ComponentCount = componentCount,
                OutputSlot = 0
            });

            soStride += (uint)(componentCount * sizeof(float));
        }

        soReflection.Dispose();

        // Create GS with stream output (no rasterization)
        ComPtr<ID3D11GeometryShader> soGeometryShader = default;
        fixed (SODeclarationEntry* soEntriesPtr = soEntries.ToArray())
        {
            SilkMarshal.ThrowHResult(
                device.CreateGeometryShaderWithStreamOutput(
                    soStageCode.GetBufferPointer(),
                    soStageCode.GetBufferSize(),
                    in soEntriesPtr[0],
                    (uint)soEntries.Count,
                    in soStride,
                    1,
                    unchecked((uint)(-1)), // D3D11_SO_NO_RASTERIZED_STREAM
                    ref Unsafe.NullRef<ID3D11ClassLinkage>(),
                    ref soGeometryShader
                )
            );
        }

        // Create SO output buffer (max 64KB)
        const uint soBufferSize = 64 * 1024;
        ComPtr<ID3D11Buffer> soBuffer = default;
        var bufferDesc = new BufferDesc
        {
            ByteWidth = soBufferSize,
            Usage = Usage.Default,
            BindFlags = (uint)BindFlag.StreamOutput,
        };
        SilkMarshal.ThrowHResult(device.CreateBuffer(in bufferDesc, null, ref soBuffer));

        // Create staging buffer for readback
        ComPtr<ID3D11Buffer> soStagingBuffer = default;
        bufferDesc = new BufferDesc
        {
            ByteWidth = soBufferSize,
            Usage = Usage.Staging,
            CPUAccessFlags = (uint)CpuAccessFlag.Read,
        };
        SilkMarshal.ThrowHResult(device.CreateBuffer(in bufferDesc, null, ref soStagingBuffer));

        // Create SO statistics query
        ComPtr<ID3D11Query> soStatsQuery = default;
        var queryDesc = new QueryDesc { Query = Query.SOStatistics };
        SilkMarshal.ThrowHResult(device.CreateQuery(in queryDesc, ref soStatsQuery));

        SetupInputAssemblerState(vertexCode);

        // Bind SO GS (no pixel shader for SO-only rendering)
        deviceContext.GSSetShader(soGeometryShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);

        ApplyParameters();

        // Bind SO target
        uint soOffset = 0;
        deviceContext.SOSetTargets(1, soBuffer, in soOffset);

        // Begin query, draw, end query
        deviceContext.Begin(soStatsQuery);
        deviceContext.DrawIndexed(3, 0, 0);
        deviceContext.End(soStatsQuery);

        // Wait for query results
        QueryDataSOStatistics soStats = default;
        while (deviceContext.GetData(soStatsQuery, ref soStats, (uint)sizeof(QueryDataSOStatistics), 0) != 0)
        {
            // Spin until data is ready
        }

        // Read back SO buffer
        deviceContext.CopyResource(soStagingBuffer, soBuffer);
        MappedSubresource mappedResource = default;
        deviceContext.Map(soStagingBuffer, 0, Map.MapRead, 0, ref mappedResource);

        // NumPrimitivesWritten is based on the output topology:
        // - PointStream: each Append = 1 point = 1 primitive
        // - TriangleStream/tessellation: each triangle = 1 primitive
        soVertexCount = (int)soStats.NumPrimitivesWritten;

        soData = new byte[soBufferSize];
        new Span<byte>(mappedResource.PData, (int)soBufferSize).CopyTo(soData);
        deviceContext.Unmap(soStagingBuffer, 0);

        // Copy to backbuffer for debug tools
        var framebuffer = swapchain.GetBuffer<ID3D11Texture2D>(0);
        framebuffer.Dispose();

        // Cleanup
        soStatsQuery.Dispose();
        soStagingBuffer.Dispose();
        soBuffer.Dispose();
        soGeometryShader.Dispose();
        vertexCode.Dispose();
        if (geometryCode.Handle != null) geometryCode.Dispose();
        if (hullCode.Handle != null) hullCode.Dispose();
        if (domainCode.Handle != null) domainCode.Dispose();
    }

    private unsafe void ApplyParameters()
    {
        BufferDesc bufferDesc;
        foreach (var param in Parameters)
        {
            var dotIndex = param.Key.IndexOf(".");
            if (dotIndex == -1)
                continue;

            var resourceType = param.Key.Substring(0, dotIndex);
            if (resourceType != "cbuffer" && resourceType != "texture" && resourceType != "buffer")
                continue;

            var resourceName = param.Key.Substring(dotIndex + 1);
            var resourceReflection = EffectReflection.ResourceBindings.Single(x => x.KeyInfo.KeyName.EndsWith(resourceName));

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
                deviceContext.CSSetConstantBuffers((uint)resourceReflection.SlotStart, 1U, &cbuffer.Handle);
                deviceContext.VSSetConstantBuffers((uint)resourceReflection.SlotStart, 1U, &cbuffer.Handle);
                deviceContext.HSSetConstantBuffers((uint)resourceReflection.SlotStart, 1U, &cbuffer.Handle);
                deviceContext.DSSetConstantBuffers((uint)resourceReflection.SlotStart, 1U, &cbuffer.Handle);
                deviceContext.GSSetConstantBuffers((uint)resourceReflection.SlotStart, 1U, &cbuffer.Handle);
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

                deviceContext.CSSetShaderResources((uint)resourceReflection.SlotStart, 1U, &bufferSRV.Handle);
                deviceContext.VSSetShaderResources((uint)resourceReflection.SlotStart, 1U, &bufferSRV.Handle);
                deviceContext.HSSetShaderResources((uint)resourceReflection.SlotStart, 1U, &bufferSRV.Handle);
                deviceContext.DSSetShaderResources((uint)resourceReflection.SlotStart, 1U, &bufferSRV.Handle);
                deviceContext.GSSetShaderResources((uint)resourceReflection.SlotStart, 1U, &bufferSRV.Handle);
                deviceContext.PSSetShaderResources((uint)resourceReflection.SlotStart, 1U, &bufferSRV.Handle);
            }
            else if (resourceType == "texture")
            {
                var color = ParseColor(param.Value);

                var textureDesc = new Texture2DDesc
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

                deviceContext.CSSetShaderResources((uint)resourceReflection.SlotStart, 1U, &textureSRV.Handle);
                deviceContext.VSSetShaderResources((uint)resourceReflection.SlotStart, 1U, &textureSRV.Handle);
                deviceContext.HSSetShaderResources((uint)resourceReflection.SlotStart, 1U, &textureSRV.Handle);
                deviceContext.DSSetShaderResources((uint)resourceReflection.SlotStart, 1U, &textureSRV.Handle);
                deviceContext.GSSetShaderResources((uint)resourceReflection.SlotStart, 1U, &textureSRV.Handle);
                deviceContext.PSSetShaderResources((uint)resourceReflection.SlotStart, 1U, &textureSRV.Handle);
            }
        }
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
            case { Class: EffectParameterClass.Vector }:
                int compIndex = 0;
                foreach (var comp in TestHeaderParser.SplitArgs(value))
                {
                    if (type.Type == EffectParameterType.Float)
                        *((float*)&cbufferDataPtr[offset + compIndex * sizeof(float)]) = float.Parse(comp, CultureInfo.InvariantCulture);
                    else if (type.Type == EffectParameterType.Int)
                        *((int*)&cbufferDataPtr[offset + compIndex * sizeof(int)]) = int.Parse(comp, CultureInfo.InvariantCulture);
                    compIndex++;
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
