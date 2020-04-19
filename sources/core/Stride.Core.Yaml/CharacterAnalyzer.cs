// Copyright (c) 2015 SharpYaml - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// -------------------------------------------------------------------------------
// SharpYaml is a fork of YamlDotNet https://github.com/aaubry/YamlDotNet
// published with the following license:
// -------------------------------------------------------------------------------
// 
// Copyright (c) 2008, 2009, 2010, 2011, 2012 Antoine Aubry
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Diagnostics;

namespace Stride.Core.Yaml
{
    internal class CharacterAnalyzer<TBuffer> where TBuffer : ILookAheadBuffer
    {
        private readonly TBuffer buffer;

        public CharacterAnalyzer(TBuffer buffer)
        {
            this.buffer = buffer;
        }

        public TBuffer Buffer { get { return buffer; } }

        public bool EndOfInput { get { return buffer.EndOfInput; } }

        public char Peek(int offset)
        {
            return buffer.Peek(offset);
        }

        public void Skip(int length)
        {
            buffer.Skip(length);
        }

        /// <summary>
        /// Check if the character at the specified position is an alphabetical
        /// character, a digit, '_', or '-'.
        /// </summary>
        public bool IsAlpha(int offset)
        {
            char character = buffer.Peek(offset);
            return
                (character >= '0' && character <= '9') ||
                (character >= 'A' && character <= 'Z') ||
                (character >= 'a' && character <= 'z') ||
                character == '_' ||
                character == '-';
        }

        public bool IsAlpha()
        {
            return IsAlpha(0);
        }

        /// <summary>
        /// Check if the character is ASCII.
        /// </summary>
        public bool IsAscii(int offset)
        {
            return buffer.Peek(offset) <= '\x7F';
        }

        public bool IsAscii()
        {
            return IsAscii(0);
        }

        public bool IsPrintable(int offset)
        {
            char character = buffer.Peek(offset);
            return Emitter.IsPrintable(character);
        }

        public bool IsPrintable()
        {
            return IsPrintable(0);
        }

        /// <summary>
        /// Check if the character at the specified position is a digit.
        /// </summary>
        public bool IsDigit(int offset)
        {
            char character = buffer.Peek(offset);
            return character >= '0' && character <= '9';
        }

        public bool IsDigit()
        {
            return IsDigit(0);
        }

        /// <summary>
        /// Get the value of a digit.
        /// </summary>
        public int AsDigit(int offset)
        {
            return buffer.Peek(offset) - '0';
        }

        public int AsDigit()
        {
            return AsDigit(0);
        }

        /// <summary>
        /// Check if the character at the specified position is a hex-digit.
        /// </summary>
        public bool IsHex(int offset)
        {
            char character = buffer.Peek(offset);
            return
                (character >= '0' && character <= '9') ||
                (character >= 'A' && character <= 'F') ||
                (character >= 'a' && character <= 'f');
        }

        /// <summary>
        /// Get the value of a hex-digit.
        /// </summary>
        public int AsHex(int offset)
        {
            char character = buffer.Peek(offset);

            if (character <= '9')
            {
                return character - '0';
            }
            else if (character <= 'F')
            {
                return character - 'A' + 10;
            }
            else
            {
                return character - 'a' + 10;
            }
        }

        public bool IsSpace(int offset)
        {
            return Check(' ', offset);
        }

        public bool IsSpace()
        {
            return IsSpace(0);
        }

        /// <summary>
        /// Check if the character at the specified position is NUL.
        /// </summary>
        public bool IsZero(int offset)
        {
            return Check('\0', offset);
        }

        public bool IsZero()
        {
            return IsZero(0);
        }

        /// <summary>
        /// Check if the character at the specified position is tab.
        /// </summary>
        public bool IsTab(int offset)
        {
            return Check('\t', offset);
        }

        public bool IsTab()
        {
            return IsTab(0);
        }

        /// <summary>
        /// Check if the character at the specified position is blank (space or tab).
        /// </summary>
        public bool IsBlank(int offset)
        {
            return IsSpace(offset) || IsTab(offset);
        }

        public bool IsBlank()
        {
            return IsBlank(0);
        }

        /// <summary>
        /// Check if the character at the specified position is a line break.
        /// </summary>
        public bool IsBreak(int offset)
        {
            return Check("\r\n\x85\x2028\x2029", offset);
        }

        public bool IsBreak()
        {
            return IsBreak(0);
        }

        public bool IsCrLf(int offset)
        {
            return Check('\r', offset) && Check('\n', offset + 1);
        }

        public bool IsCrLf()
        {
            return IsCrLf(0);
        }

        /// <summary>
        /// Check if the character is a line break or NUL.
        /// </summary>
        public bool IsBreakOrZero(int offset)
        {
            return IsBreak(offset) || IsZero(offset);
        }

        public bool IsBreakOrZero()
        {
            return IsBreakOrZero(0);
        }

        /// <summary>
        /// Check if the character is a line break, space, tab, or NUL.
        /// </summary>
        public bool IsBlankOrBreakOrZero(int offset)
        {
            return IsBlank(offset) || IsBreakOrZero(offset);
        }

        public bool IsBlankOrBreakOrZero()
        {
            return IsBlankOrBreakOrZero(0);
        }

        public bool Check(char expected)
        {
            return Check(expected, 0);
        }

        public bool Check(char expected, int offset)
        {
            return buffer.Peek(offset) == expected;
        }

        public bool Check(string expectedCharacters)
        {
            return Check(expectedCharacters, 0);
        }

        public bool Check(string expectedCharacters, int offset)
        {
            Debug.Assert(expectedCharacters.Length > 1, "Use Check(char, int) instead.");

            char character = buffer.Peek(offset);

            foreach (var expected in expectedCharacters)
            {
                if (expected == character)
                {
                    return true;
                }
            }
            return false;
        }
    }
}