// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Text;
using Stride.Core;
using Stride.Core.Storage;

namespace Stride.Shaders
{
    /// <summary>
    /// The bytecode of an effect.
    /// </summary>
    [DataContract]
    public partial class ShaderBytecode
    {
        /// <summary>
        /// The stage of this Bytecode.
        /// </summary>
        public ShaderStage Stage;

        /// <summary>
        /// Hash of the Data.
        /// </summary>
        public ObjectId Id { get; set; }

        /// <summary>
        /// Gets the shader data that should be used to create the <see cref="Shader"/>.
        /// </summary>
        /// <value>
        /// The shader data.
        /// </value>
        public byte[] Data { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderBytecode"/> class.
        /// </summary>
        public ShaderBytecode()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderBytecode"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        public ShaderBytecode(ObjectId id, byte[] data)
        {
            Id = id;
            Data = data;
        }

        /// <summary>
        /// Shallow clones this instance.
        /// </summary>
        /// <returns>ShaderBytecode.</returns>
        public ShaderBytecode Clone()
        {
            return (ShaderBytecode)MemberwiseClone();
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="ShaderBytecode"/> to <see cref="System.Byte[][]"/>.
        /// </summary>
        /// <param name="shaderBytecode">The shader bytecode.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator byte[](ShaderBytecode shaderBytecode)
        {
            return shaderBytecode.Data;
        }

        /// <summary>
        /// Gets the data as a string.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetDataAsString()
        {
            return Encoding.UTF8.GetString(Data, 0, Data.Length);
        }
    }
}
