// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Provides extension methods that facilitate the conversion of Vertex Declarations and
///   Vertex Buffer bindings into <see cref="InputElementDescription"/> structures, which are used
///   to define the input layout for Shaders in the graphics pipeline.
/// </summary>
public static class VertexBufferBindingExtensions
{
    /// <summary>
    ///   Creates descriptions for the Input Elements in a Vertex Buffer that will be fed to a Shader
    ///   based on the specified Vertex Declaration.
    /// </summary>
    /// <param name="vertexDeclaration">
    ///   The Vertex Declaration containing the Vertex Elements to be converted into Input Element descriptions.
    /// </param>
    /// <returns>
    ///   An array of <see cref="InputElementDescription"/> structures representing the Input Elements derived from the
    ///   Vertex Declaration.
    /// </returns>
    /// <remarks>
    ///   This method processes the vertex elements in the provided <see cref="VertexDeclaration"/> and
    ///   maps them to corresponding <see cref="InputElementDescription"/> structures.
    /// </remarks>
    public static InputElementDescription[] CreateInputElements(this VertexDeclaration vertexDeclaration)
    {
        var inputElements = new InputElementDescription[vertexDeclaration.VertexElements.Length];
        var inputElementIndex = 0;

        foreach (var element in vertexDeclaration.EnumerateWithOffsets())
        {
            inputElements[inputElementIndex++] = new InputElementDescription
            {
                SemanticName = element.VertexElement.SemanticName,
                SemanticIndex = element.VertexElement.SemanticIndex,
                Format = element.VertexElement.Format,
                InputSlot = 0,
                AlignedByteOffset = element.Offset
            };
        }

        return inputElements;
    }

    /// <summary>
    ///   Creates descriptions for the Input Elements in the Vertex Buffers that will be bound to the
    ///   Graphics Pipeline and fed to a Shader based on the specified Vertex Declaration.
    /// </summary>
    /// <param name="vertexBuffers">
    ///   The descriptions of the Vertex Buffers that will be bound to the pipeline.
    /// </param>
    /// <returns>
    ///   An array of <see cref="InputElementDescription"/> structures representing the Input Elements derived from the
    ///   Vertex Buffer bindings.
    /// </returns>
    /// <remarks>
    ///   This method processes the vertex elements in the provided <see cref="VertexBufferBinding"/>s and
    ///   maps them to corresponding <see cref="InputElementDescription"/> structures.
    /// </remarks>
    public static InputElementDescription[] CreateInputElements(this VertexBufferBinding[] vertexBuffers)
    {
        // Count the total number of Input Elements across all Vertex Buffers
        var inputElementCount = 0;
        foreach (var vertexBuffer in vertexBuffers)
        {
            inputElementCount += vertexBuffer.Declaration.VertexElements.Length;
        }

        var inputElements = new InputElementDescription[inputElementCount];
        var inputElementIndex = 0;
        for (int inputSlot = 0; inputSlot < vertexBuffers.Length; inputSlot++)
        {
            var vertexBuffer = vertexBuffers[inputSlot];
            foreach (var element in vertexBuffer.Declaration.EnumerateWithOffsets())
            {
                inputElements[inputElementIndex++] = new InputElementDescription
                {
                    SemanticName = element.VertexElement.SemanticName,
                    SemanticIndex = element.VertexElement.SemanticIndex,
                    Format = element.VertexElement.Format,
                    InputSlot = inputSlot,
                    AlignedByteOffset = element.Offset
                };
            }
        }

        return inputElements;
    }
}
