// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Text;

using Stride.Core;
using Stride.Core.Storage;

namespace Stride.Shaders;

/// <summary>
///   Represents a compiled Shader bytecode.
/// </summary>
[DataContract]
[DebuggerDisplay("Stage = {Stage}, Id = {Id}")]
public partial class ShaderBytecode
{
    /// <summary>
    ///   The stage of this Shader bytecode.
    /// </summary>
    public ShaderStage Stage;

    /// <summary>
    ///   Gets or sets an unique identifier for the Shader bytecode.
    /// </summary>
    public ObjectId Id { get; set; } // TODO: Public set?

    /// <summary>
    ///   Gets or sets the compiled Shader bytecode data that should be used to create the Shader.
    /// </summary>
    public byte[] Data { get; set; }


    /// <summary>
    ///   Initializes a new instance of the <see cref="ShaderBytecode"/> class.
    /// </summary>
    public ShaderBytecode() { }

    /// <summary>
    ///   Initializes a new instance of the <see cref="ShaderBytecode"/> class.
    /// </summary>
    /// <param name="id">An unique identifier for the compiled Shader bytecode data.</param>
    /// <param name="data">The compiled Shader bytecode data.</param>
    public ShaderBytecode(ObjectId id, byte[] data)
    {
        Id = id;
        Data = data;
    }


    /// <summary>
    ///   Creates a shallow copy of the current <see cref="ShaderBytecode"/>.
    /// </summary>
    /// <returns>A shallow copy of the current instance.</returns>
    public ShaderBytecode Clone()
    {
        return (ShaderBytecode) MemberwiseClone();
    }

    /// <summary>
    ///   Performs an implicit conversion from <see cref="ShaderBytecode"/> to <see cref="byte[]"/>, returning
    ///   the compiled Shader data.
    /// </summary>
    /// <param name="shaderBytecode">The compiled Shader bytecode.</param>
    /// <returns>The compiled Shader bytecode data converted to a byte array.</returns>
    public static implicit operator byte[](ShaderBytecode shaderBytecode)
    {
        return shaderBytecode.Data;
    }

    /// <summary>
    ///   Gets the Shader data as a <see langword="string"/>. In some platforms (e.g. OpenGL), the data
    ///   represents the GLSL source code, instead of compiled bytecode.
    /// </summary>
    /// <returns>The Shader data as GLSL source code.</returns>
    public string GetDataAsString()
    {
        // TODO: This is a workaround for OpenGL, where the shader bytecode is actually GLSL source code.
        //       But the class is still called ShaderBytecode!

        return Encoding.UTF8.GetString(Data);
    }
}
