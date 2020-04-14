// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;

namespace Stride
{
    namespace AssimpNet
    {
        namespace Material
        {
            /// <summary>
            /// Enumeration of the different types of node in the new Assimp's material stack.
            /// </summary>
            public enum StackType
            {
                Color = 0,
                Texture,
                Operation
            }
            /// <summary>
            /// Enumeration of the new Assimp's flags.
            /// </summary>
            public enum Flags
            {
                Invert = 1,
                ReplaceAlpha = 2
            }
            /// <summary>
            /// Enumeration of the different operations in the new Assimp's material stack.
            /// </summary>
            public enum Operation
            {
                Add = 0,
                Add3ds,
                AddMaya,
                Average,
                Color,
                ColorBurn,
                ColorDodge,
                Darken3ds,
                DarkenMaya,
                Desaturate,
                Difference3ds,
                DifferenceMaya,
                Divide,
                Exclusion,
                HardLight,
                HardMix,
                Hue,
                Illuminate,
                In,
                Lighten3ds,
                LightenMaya,
                LinearBurn,
                LinearDodge,
                Multiply3ds,
                MultiplyMaya,
                None,
                Out,
                Over3ds,
                Overlay3ds,
                OverMaya,
                PinLight,
                Saturate,
                Saturation,
                Screen,
                SoftLight,
                Substract3ds,
                SubstractMaya,
                Value,
                Mask
            }
            /// <summary>
            /// Enumeration of the different mapping modes in the new Assimp's material stack.
            /// </summary>
            public enum MappingMode
            {
                Wrap,
                Clamp,
                Decal,
                Mirror
            }
            /// <summary>
            /// Class representing an element in the new Assimp's material stack.
            /// </summary>
            public abstract class StackElement
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="StackElement"/> class.
                /// </summary>
                /// <param name="Alpha">The alpha of the node.</param>
                /// <param name="Blend">The blending coefficient of the node.</param>
                /// <param name="flags">The flags of the node.</param>
                /// <param name="Type">The type of the node.</param>
                public StackElement(float Alpha, float Blend, int flags, StackType Type)
                {
                    alpha = Alpha;
                    blend = Blend;
                    type = Type;
                }
                /// <summary>
                /// Gets the alpha of the node.
                /// </summary>
                /// <value>
                /// The alpha of the node.
                /// </value>
                public float alpha { get; private set; }
                /// <summary>
                /// Gets the blending coefficient of the node.
                /// </summary>
                /// <value>
                /// The blending coefficient of the node.
                /// </value>
                public float blend { get; private set; }
                /// <summary>
                /// Gets the flags of the node.
                /// </summary>
                /// <value>
                /// The flags of the node.
                /// </value>
                public int flags { get; private set; }
                /// <summary>
                /// Gets the type of the node.
                /// </summary>
                /// <value>
                /// The type of the node.
                /// </value>
                public StackType type { get; private set; }
            }
            /// <summary>
            /// Class representing an operation in the new Assimp's material stack.
            /// </summary>
            public class StackOperation : StackElement
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="StackOperation"/> class.
                /// </summary>
                /// <param name="Operation">The operation of the node.</param>
                /// <param name="Alpha">The alpha of the node.</param>
                /// <param name="Blend">The blending coefficient of the node.</param>
                /// <param name="Flags">The flags.</param>
                public StackOperation(Operation Operation, float Alpha = 1.0f, float Blend = 1.0f, int Flags = 0)
                    : base(Alpha, Blend, Flags, StackType.Operation)
                {
                    operation = Operation;
                }
                /// <summary>
                /// Gets the operation of the node.
                /// </summary>
                /// <value>
                /// The operation of the node.
                /// </value>
                public Operation operation { get; private set; }
            }
            /// <summary>
            /// Class representing a color in the new Assimp's material stack.
            /// </summary>
            public class StackColor : StackElement
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="StackColor"/> class.
                /// </summary>
                /// <param name="Color">The color of the node.</param>
                /// <param name="Alpha">The alpha of the node.</param>
                /// <param name="Blend">The blending coefficient of the node.</param>
                /// <param name="Flags">The flags of the node.</param>
                public StackColor(Color3 Color, float Alpha = 1.0f, float Blend = 1.0f, int Flags = 0)
                    : base(Alpha, Blend, Flags, StackType.Color)
                {
                    color = Color;
                }
                /// <summary>
                /// Gets the color of the node.
                /// </summary>
                /// <value>
                /// The color of the node.
                /// </value>
                public Color3 color { get; private set; }
            }
            /// <summary>
            /// Class representing a texture in the new Assimp's material stack.
            /// </summary>
            public class StackTexture : StackElement
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="StackTexture"/> class.
                /// </summary>
                /// <param name="TexturePath">The texture path.</param>
                /// <param name="Channel">The uv channel used by the texture.</param>
                /// <param name="MappingModeU">The U mapping mode.</param>
                /// <param name="MappingModeV">The V mapping mode.</param>
                /// <param name="Alpha">The alpha of the node.</param>
                /// <param name="Blend">The blending coefficient of the node.</param>
                /// <param name="Flags">The flags of the node.</param>
                public StackTexture(String TexturePath, int Channel, MappingMode MappingModeU, MappingMode MappingModeV, float Alpha = 1.0f, float Blend = 1.0F, int Flags = 0)
                    : base(Alpha, Blend, Flags, StackType.Texture)
                {
                    texturePath = TexturePath;
                    channel = Channel;
                    mappingModeU = MappingModeU;
                    mappingModeV = MappingModeV;
                }
                /// <summary>
                /// Gets the texture path.
                /// </summary>
                /// <value>
                /// The texture path.
                /// </value>
                public String texturePath { get; private set; }
                /// <summary>
                /// Gets the uv channel.
                /// </summary>
                /// <value>
                /// The uv channel.
                /// </value>
                public int channel { get; private set; }
                /// <summary>
                /// Gets the U mapping mode.
                /// </summary>
                /// <value>
                /// The U mapping mode.
                /// </value>
                public MappingMode mappingModeU { get; private set; }
                /// <summary>
                /// Gets the Vmapping mode.
                /// </summary>
                /// <value>
                /// The V mapping mode.
                /// </value>
                public MappingMode mappingModeV { get; private set; }
            }
            /// <summary>
            /// Class representing the new Assimp's material stack in c#.
            /// </summary>
            public class Stack
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="Stack"/> class.
                /// </summary>
                public Stack()
                {
                    stack = new Stack<StackElement>();
                }
                /// <summary>
                /// The internal stack.
                /// </summary>
                private Stack<StackElement> stack;
                /// <summary>
                /// Gets the size of the stack.
                /// </summary>
                /// <value>
                /// The size of the stack.
                /// </value>
                private int Count { get { return stack.Count; } }
                /// <summary>
                /// Gets a value indicating whether the stack is empty.
                /// </summary>
                /// <value>
                ///   <c>true</c> if the stack is empty; otherwise, <c>false</c>.
                /// </value>
                public bool IsEmpty { get { return stack.Count == 0; } }
                /// <summary>
                /// Pushes the specified element.
                /// </summary>
                /// <param name="element">The element.</param>
                public void Push(StackElement element)
                {
                    stack.Push(element);
                }
                /// <summary>
                /// Pops an element.
                /// </summary>
                /// <returns>The element.</returns>
                public StackElement Pop()
                {
                    return stack.Pop();
                }
                /// <summary>
                /// Gets the top element of the stack.
                /// </summary>
                /// <returns>The top element of the stack.</returns>
                public StackElement Peek()
                {
                    return stack.Peek();
                }
                /// <summary>
                /// Clears the stack.
                /// </summary>
                public void Clear()
                {
                    stack.Clear();
                }
            }
        }
    }
}
