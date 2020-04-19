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
using System.Linq;
using Stride.Core.Yaml.Events;

namespace Stride.Core.Yaml.Serialization
{
    internal class AnchorEventEmitter : ChainedEventEmitter
    {
        private readonly List<object> events = new List<object>();
        private readonly HashSet<string> alias = new HashSet<string>();

        public AnchorEventEmitter(IEventEmitter nextEmitter) : base(nextEmitter)
        {
        }

        public override void Emit(AliasEventInfo eventInfo)
        {
            alias.Add(eventInfo.Alias);
            events.Add(eventInfo);
        }

        public override void Emit(ScalarEventInfo eventInfo)
        {
            events.Add(eventInfo);
        }

        public override void Emit(MappingStartEventInfo eventInfo)
        {
            events.Add(eventInfo);
        }

        public override void Emit(MappingEndEventInfo eventInfo)
        {
            events.Add(eventInfo);
        }

        public override void Emit(SequenceStartEventInfo eventInfo)
        {
            events.Add(eventInfo);
        }

        public override void Emit(SequenceEndEventInfo eventInfo)
        {
            events.Add(eventInfo);
        }

        public override void Emit(ParsingEvent parsingEvent)
        {
            events.Add(parsingEvent);
        }

        public override void DocumentEnd()
        {
            // remove all unused anchor
            foreach (var objectEventInfo in events.OfType<ObjectEventInfo>())
            {
                if (objectEventInfo.Anchor != null && !alias.Contains(objectEventInfo.Anchor))
                {
                    objectEventInfo.Anchor = null;
                }
            }

            // Flush all events to emitter.
            foreach (var evt in events)
            {
                if (evt is AliasEventInfo)
                {
                    nextEmitter.Emit((AliasEventInfo) evt);
                }
                else if (evt is ScalarEventInfo)
                {
                    nextEmitter.Emit((ScalarEventInfo) evt);
                }
                else if (evt is MappingStartEventInfo)
                {
                    nextEmitter.Emit((MappingStartEventInfo) evt);
                }
                else if (evt is MappingEndEventInfo)
                {
                    nextEmitter.Emit((MappingEndEventInfo) evt);
                }
                else if (evt is SequenceStartEventInfo)
                {
                    nextEmitter.Emit((SequenceStartEventInfo) evt);
                }
                else if (evt is SequenceEndEventInfo)
                {
                    nextEmitter.Emit((SequenceEndEventInfo) evt);
                }
                else if (evt is ParsingEvent)
                {
                    nextEmitter.Emit((ParsingEvent) evt);
                }
            }

            nextEmitter.DocumentEnd();
        }
    }
}