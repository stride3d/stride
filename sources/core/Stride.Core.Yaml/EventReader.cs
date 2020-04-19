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

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Stride.Core.Yaml.Events;
using Event = Stride.Core.Yaml.Events.ParsingEvent;

namespace Stride.Core.Yaml
{
    /// <summary>
    /// Reads events from a sequence of <see cref="Event" />.
    /// </summary>
    public class EventReader
    {
        private readonly IParser parser;
        private bool endOfStream;
        private int currentDepth = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventReader"/> class.
        /// </summary>
        /// <param name="parser">The parser that provides the events.</param>
        public EventReader(IParser parser)
        {
            this.parser = parser;
            MoveNext();
        }

        /// <summary>
        /// Gets the underlying parser.
        /// </summary>
        /// <value>The parser.</value>
        public IParser Parser { get { return parser; } }

        public int CurrentDepth { get { return currentDepth; } }

        /// <summary>
        /// Ensures that the current event is of the specified type, returns it and moves to the next event.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="Event"/>.</typeparam>
        /// <returns>Returns the current event.</returns>
        /// <exception cref="YamlException">If the current event is not of the specified type.</exception>
        public T Expect<T>() where T : Event
        {
            var yamlEvent = Allow<T>();
            if (yamlEvent == null)
            {
                // TODO: Throw a better exception
                throw new YamlException(
                    parser.Current.Start,
                    parser.Current.End,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Expected '{0}', got '{1}' (at line {2}, character {3}).",
                        typeof(T).Name,
                        parser.Current.GetType().Name,
                        parser.Current.Start.Line,
                        parser.Current.Start.Column
                        )
                    );
            }
            return yamlEvent;
        }

        /// <summary>
        /// Moves to the next event.
        /// </summary>
        private void MoveNext()
        {
            if (parser.Current != null)
                currentDepth += parser.Current.NestingIncrease;
            endOfStream = !parser.MoveNext();
        }

        /// <summary>
        /// Checks whether the current event is of the specified type.
        /// </summary>
        /// <typeparam name="T">Type of the event.</typeparam>
        /// <returns>Returns true if the current event is of type <typeparamref name="T"/>. Otherwise returns false.</returns>
        public bool Accept<T>() where T : Event
        {
            EnsureNotAtEndOfStream();

            return parser.Current is T;
        }

        /// <summary>
        /// Checks whether the current event is of the specified type.
        /// If the event is of the specified type, returns it and moves to the next event.
        /// Otherwise retruns null.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="Event"/>.</typeparam>
        /// <returns>Returns the current event if it is of type T; otherwise returns null.</returns>
        public T Allow<T>() where T : Event
        {
            if (!Accept<T>())
            {
                return null;
            }
            T yamlEvent = (T) parser.Current;
            MoveNext();
            return yamlEvent;
        }

        /// <summary>
        /// Gets the next event without consuming it.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="Event"/>.</typeparam>
        /// <returns>Returns the current event if it is of type T; otherwise returns null.</returns>
        public T Peek<T>() where T : Event
        {
            if (!Accept<T>())
            {
                return null;
            }
            T yamlEvent = (T) parser.Current;
            return yamlEvent;
        }

        public void ReadCurrent(IList<Event> events)
        {
            int depth = 0;

            do
            {
                if (Accept<SequenceStart>() || Accept<MappingStart>() || Accept<StreamStart>() || Accept<DocumentStart>())
                {
                    ++depth;
                }
                else if (Accept<SequenceEnd>() || Accept<MappingEnd>() || Accept<StreamEnd>() || Accept<DocumentEnd>())
                {
                    --depth;
                }

                events.Add(Allow<Event>());
            } while (depth > 0 && !endOfStream);
        }

        /// <summary>
        /// Skips the current event and any "child" event.
        /// </summary>
        public void Skip()
        {
            int depth = 0;

            do
            {
                if (Accept<SequenceStart>() || Accept<MappingStart>() || Accept<StreamStart>() || Accept<DocumentStart>())
                {
                    ++depth;
                }
                else if (Accept<SequenceEnd>() || Accept<MappingEnd>() || Accept<StreamEnd>() || Accept<DocumentEnd>())
                {
                    --depth;
                }

                MoveNext();
            } while (depth > 0 && !endOfStream);
        }

        /// <summary>
        /// Skips until we reach the appropriate depth again
        /// </summary>
        public void Skip(int untilDepth, bool skipAtLeastOne = true)
        {
            while (CurrentDepth > untilDepth || skipAtLeastOne)
            {
                MoveNext();
                skipAtLeastOne = false;
            }
        }

        /// <summary>
        /// Call this if <see cref="Parser"/> state has changed (i.e. it might not be at end of stream anymore).
        /// </summary>
        public void RefreshParserState()
        {
            endOfStream = parser.IsEndOfStream;
        }

        /// <summary>
        /// Throws an exception if Ensures the not at end of stream.
        /// </summary>
        private void EnsureNotAtEndOfStream()
        {
            if (endOfStream)
            {
                throw new EndOfStreamException();
            }
        }
    }
}