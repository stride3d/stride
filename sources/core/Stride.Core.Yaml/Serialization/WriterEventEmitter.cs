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

using Stride.Core.Yaml.Events;

namespace Stride.Core.Yaml.Serialization
{
    internal sealed class WriterEventEmitter : IEventEmitter
    {
        private readonly IEmitter emitter;

        public WriterEventEmitter(IEmitter emitter)
        {
            this.emitter = emitter;
        }

        void IEventEmitter.StreamStart()
        {
            emitter.Emit(new StreamStart());
        }

        void IEventEmitter.DocumentStart()
        {
            emitter.Emit(new DocumentStart());
        }

        void IEventEmitter.Emit(AliasEventInfo eventInfo)
        {
            emitter.Emit(new AnchorAlias(eventInfo.Alias));
        }

        void IEventEmitter.Emit(ScalarEventInfo eventInfo)
        {
            emitter.Emit(new Scalar(eventInfo.Anchor, eventInfo.Tag, eventInfo.RenderedValue, eventInfo.Style, eventInfo.IsPlainImplicit, eventInfo.IsQuotedImplicit));
        }

        void IEventEmitter.Emit(MappingStartEventInfo eventInfo)
        {
            emitter.Emit(new MappingStart(eventInfo.Anchor, eventInfo.Tag, eventInfo.IsImplicit, eventInfo.Style));
        }

        void IEventEmitter.Emit(MappingEndEventInfo eventInfo)
        {
            emitter.Emit(new MappingEnd());
        }

        void IEventEmitter.Emit(SequenceStartEventInfo eventInfo)
        {
            emitter.Emit(new SequenceStart(eventInfo.Anchor, eventInfo.Tag, eventInfo.IsImplicit, eventInfo.Style));
        }

        void IEventEmitter.Emit(SequenceEndEventInfo eventInfo)
        {
            emitter.Emit(new SequenceEnd());
        }

        public void Emit(ParsingEvent parsingEvent)
        {
            emitter.Emit(parsingEvent);
        }

        void IEventEmitter.DocumentEnd()
        {
            emitter.Emit(new DocumentEnd(true));
        }

        void IEventEmitter.StreamEnd()
        {
            emitter.Emit(new StreamEnd());
        }
    }
}