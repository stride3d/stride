// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   Defines constants that specify the state of a Graphics Resource regarding how the resource is being used.
/// </summary>
[Flags]
public enum GraphicsResourceState
{
    // From D3D12_RESOURCE_STATES in d3d12.h

    /// <summary>
    ///   The application should transition to this state only for accessing a Graphics Resource <strong>across different graphics engine types</strong>.
    ///   <para>
    ///     Specifically, a Graphics Resource must be in the <strong>Common</strong> state before being used on a <em>Copy Queue</em>
    ///     (when previously used on a <em>Direct / Compute Queue</em>), and before being used on a <em>Direct / Compute Queue</em>
    ///     (when previously used on <em>Copy Queue</em>). This restriction doesn't exist when accessing data between <em>Direct</em>
    ///     and <em>Compute</em> queues.
    ///   </para>
    ///   <para>
    ///     The <strong>Common</strong> state can be used for all usages on a <em>Copy Queue</em> using the implicit state transitions.
    ///     For more information, read about <strong>Multi-engine synchronization</strong>.
    ///   </para>
    ///   <para>
    ///     Additionally, Textures must be in the <strong>Common</strong> state for CPU access to be legal, assuming the Texture
    ///     was created in a CPU-visible heap in the first place.
    ///   </para>
    /// </summary>
    Common = 0,

    /// <summary>
    ///   Synonymous with the <see cref="Common"/> flag.
    /// </summary>
    Present = 0,

    /// <summary>
    ///   A Graphics Sub-Resource must be in this state when it is accessed by the GPU <strong>as a Vertex Buffer or Constant Buffer</strong>.
    ///   <para>
    ///     This is a read-only state.
    ///   </para>
    /// </summary>
    VertexAndConstantBuffer = 1,

    /// <summary>
    ///   A Graphics Sub-Resource must be in this state when it is accessed by the 3D pipeline <strong>as an Index Buffer</strong>.
    ///   <para>
    ///     This is a read-only state.
    ///   </para>
    /// </summary>
    IndexBuffer = 2,

    /// <summary>
    ///   The Graphics Resource is used <strong>as a Render Target</strong>.
    ///   <para>
    ///     A Graphics Sub-Resource must be in this state when it is rendered to,
    ///     or when it is cleared with <see cref="CommandList.Clear(Texture, Core.Mathematics.Color4)"/>.
    ///   </para>
    ///   <para>
    ///     This is a write-only state. To read from a Render Target as a Shader Resource, the Graphics Resource must be in either
    ///     <see cref="NonPixelShaderResource"/> or <see cref="PixelShaderResource"/> state.
    ///   </para>
    /// </summary>
    RenderTarget = 4,

    /// <summary>
    ///   The Graphics Resource is used <strong>for Unordered Access</strong>.
    ///   <para>
    ///     A Graphics Sub-Resource must be in this state when it is accessed by the GPU via an Unordered Access View.
    ///     A Graphics Sub-Resource must also be in this state when it is cleared with <see cref="CommandList.ClearReadWrite"/>.
    ///   </para>
    ///   <para>
    ///     This is a read / write state.
    ///   </para>
    /// </summary>
    UnorderedAccess = 8,

    /// <summary>
    ///   This state should be used for <see cref="CommandList.Clear(Texture, DepthStencilClearOptions, float, byte)"/>
    ///   when the flags (see <see cref="DepthStencilClearOptions"/>) indicate a given Graphics Sub-Resource should be cleared
    ///   (otherwise the Graphics Sub-Resource state doesn't matter), or when using it <strong>in a writable Depth-Stencil View</strong>
    ///   when the Pipeline State has depth write enabled (see <see cref="DepthStencilStateDescription.DepthBufferWriteEnable"/>).
    ///   <para>
    ///     This state is mutually exclusive with other states.
    ///   </para>
    /// </summary>
    DepthWrite = 0x10,

    /// <summary>
    ///   This state should be used when the Graphics Sub-Resource is <strong>in a read-only Depth-Stencil View</strong>,
    ///   or when depth write is disabled (see <see cref="DepthStencilStateDescription.DepthBufferWriteEnable"/>).
    ///   <para>
    ///     It can be combined with other read states (for example, <see cref="PixelShaderResource"/>), such that the Graphics Resource
    ///     can be used for the Depth or Stencil test, and accessed by a Shader within the same draw call.
    ///   </para>
    ///   <para>
    ///     Using it when depth will be written by a draw call or clear command is invalid.
    ///   </para>
    /// </summary>
    DepthRead = 0x20,

    /// <summary>
    ///   The Graphics Resource is used <strong>with a Shader other than the Pixel Shader</strong>.
    ///   <para>
    ///     A Graphics Sub-Resource must be in this state before being read by any stage (except for the Pixel Shader stage)
    ///     via a Shader Resource View.
    ///     You can still use the Graphics Resource in a Pixel Shader with this flag as long as it also has the flag <see cref="PixelShaderResource"/> set.
    ///   </para>
    ///   <para>
    ///     This is a read-only state.
    ///   </para>
    /// </summary>
    NonPixelShaderResource = 0x40,

    /// <summary>
    ///   The Graphics Resource is used <strong>with a Pixel Shader</strong>.
    ///   <para>
    ///     A Graphics Sub-Resource must be in this state before being read by the Pixel Shader via a Shader Resource View.
    ///   </para>
    ///   <para>
    ///     This is a read-only state.
    ///   </para>
    /// </summary>
    PixelShaderResource = 0x80,

    /// <summary>
    ///   The Graphics Resource can be used <strong>in both Pixel and non-Pixel Shaders</strong>.
    /// </summary>
    /// <remarks>
    ///   This value is a combination of the <see cref="NonPixelShaderResource"/> and <see cref="PixelShaderResource"/> flags,
    ///   allowing it to be used in scenarios where both types of Shader Resources are required.
    /// </remarks>
    AllShaderResource = NonPixelShaderResource | PixelShaderResource, // 0x40 | 0x80

    /// <summary>
    ///   The Graphics Resource is used <strong>with Stream Output</strong>.
    ///   <para>
    ///     A Graphics Sub-Resource must be in this state when it is accessed by the 3D pipeline as a Stream-Out target.
    ///   </para>
    ///   <para>
    ///     This is a write-only state.
    ///   </para>
    /// </summary>
    StreamOut = 0x100,

    /// <summary>
    ///   The Graphics Resource is used <strong>as an Indirect Argument</strong>.
    ///   <para>
    ///     Graphics Sub-Resources must be in this state when they are used as the Argument Buffer passed to a
    ///     indirect drawing method like <see cref="CommandList.DrawInstanced(Buffer, int)"/> or <see cref="CommandList.DrawIndexedInstanced(Buffer, int)"/>.
    ///   </para>
    ///   <para>
    ///     This is a read-only state.
    ///   </para>
    /// </summary>
    IndirectArgument = 0x200,

    /// <summary>
    ///   The Graphics Resource is used for <see href="https://learn.microsoft.com/en-us/windows/win32/direct3d12/predication">Predication</see>.
    /// </summary>
    /// <remarks>
    ///   Predication is a feature that enables the GPU rather than the CPU to determine to not draw, copy, or dispatch an object.
    ///   <para>
    ///     The typical use of predication is with occlusion; if a bounding box is drawn and is occluded, there is obviously no point
    ///     in drawing the object itself. In this situation, the drawing of the object can be "predicated", enabling its removal from
    ///     actual rendering by the GPU.
    ///   </para>
    /// </remarks>
    Predication = 0x200,

    /// <summary>
    ///   The Graphics Resource is used as <strong>the destination in a copy operation</strong>.
    ///   <para>
    ///     Graphics Sub-Resources must be in this state when they are used as the destination of a copy operation,
    ///     or a <em>blt</em> operation.
    ///   </para>
    ///   <para>
    ///     This is a write-only state.
    ///   </para>
    /// </summary>
    CopyDestination = 0x400,

    /// <summary>
    ///   The Graphics Resource is used as <strong>the source in a copy operation</strong>.
    ///   <para>
    ///     Graphics Sub-Resources must be in this state when they are used as the source of a copy operation,
    ///     or a <em>blt</em> operation.
    ///   </para>
    ///   <para>
    ///     This is a read-only state.
    ///   </para>
    /// </summary>
    CopySource = 0x800,

    /// <summary>
    ///   This state is the required <strong>starting state for an upload heap</strong>. It is a combination of other read-state bits.
    ///   <para>
    ///     The application should generally avoid transitioning to <strong>GenericRead</strong> when possible, since that
    ///     can result in premature cache flushes, or Graphics Resource layout changes (for example, compress / decompress),
    ///     causing unnecessary pipeline stalls. You should instead transition resources only to the actually-used states.
    ///   </para>
    /// </summary>
    GenericRead = VertexAndConstantBuffer | IndexBuffer | NonPixelShaderResource | PixelShaderResource | IndirectArgument | CopySource,  // 0x1 | 0x2 | 0x40 | 0x80 | 0x200 | 0x800

    /// <summary>
    ///   The Graphics Resource is used as <strong>the destination in a resolve operation</strong>.
    /// </summary>
    ResolveDestination = 0x1000,

    /// <summary>
    ///   The Graphics Resource is used as <strong>the source in a resolve operation</strong>.
    /// </summary>
    ResolveSource = 0x2000
}
