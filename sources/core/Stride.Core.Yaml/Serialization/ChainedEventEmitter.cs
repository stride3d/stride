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

using System;
using Stride.Core.Yaml.Events;

namespace Stride.Core.Yaml.Serialization
{
    /// <summary>
    /// Provided the base implementation for an IEventEmitter that is a
    /// decorator for another IEventEmitter.
    /// </summary>
    internal abstract class ChainedEventEmitter : IEventEmitter
    {
        protected readonly IEventEmitter nextEmitter;

        protected ChainedEventEmitter(IEventEmitter nextEmitter)
        {
            if (nextEmitter == null)
            {
                throw new ArgumentNullException("nextEmitter");
            }

            this.nextEmitter = nextEmitter;
        }

        public virtual void StreamStart()
        {
            nextEmitter.StreamStart();
        }

        public virtual void DocumentStart()
        {
            nextEmitter.DocumentStart();
        }

        public virtual void Emit(AliasEventInfo eventInfo)
        {
            nextEmitter.Emit(eventInfo);
        }

        public virtual void Emit(ScalarEventInfo eventInfo)
        {
            nextEmitter.Emit(eventInfo);
        }

        public virtual void Emit(MappingStartEventInfo eventInfo)
        {
            nextEmitter.Emit(eventInfo);
        }

        public virtual void Emit(MappingEndEventInfo eventInfo)
        {
            nextEmitter.Emit(eventInfo);
        }

        public virtual void Emit(SequenceStartEventInfo eventInfo)
        {
            nextEmitter.Emit(eventInfo);
        }

        public virtual void Emit(SequenceEndEventInfo eventInfo)
        {
            nextEmitter.Emit(eventInfo);
        }

        public virtual void Emit(ParsingEvent parsingEvent)
        {
            nextEmitter.Emit(parsingEvent);
        }

        public virtual void DocumentEnd()
        {
            nextEmitter.DocumentEnd();
        }

        public virtual void StreamEnd()
        {
            nextEmitter.StreamEnd();
        }
    }
}