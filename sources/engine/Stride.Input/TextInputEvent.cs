// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Input
{
    /// <summary>
    /// Input event used for text input and IME related events
    /// </summary>
    public class TextInputEvent : InputEvent
    {
        /// <summary>
        /// The text that was entered
        /// </summary>
        public string Text;

        /// <summary>
        /// The type of text input event
        /// </summary>
        public TextInputEventType Type;

        /// <summary>
        /// Start of the current composition being edited
        /// </summary>
        public int CompositionStart;

        /// <summary>
        /// Length of the current part of the composition being edited
        /// </summary>
        public int CompositionLength;

        public override string ToString()
        {
            return $"{nameof(Text)}: {Text}, {nameof(Type)}: {Type}, {nameof(CompositionStart)}: {CompositionStart}, {nameof(CompositionLength)}: {CompositionLength}";
        }
    }
}