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

namespace Stride.Core.Yaml.Serialization
{
    public abstract class EventInfo
    {
        public object SourceValue { get; private set; }
        public Type SourceType { get; private set; }

        protected EventInfo(object sourceValue, Type sourceType)
        {
            SourceValue = sourceValue;
            SourceType = sourceType;
        }
    }

    public class AliasEventInfo : EventInfo
    {
        public AliasEventInfo(object sourceValue, Type sourceType)
            : base(sourceValue, sourceType)
        {
        }

        public string Alias { get; set; }
    }

    public class ObjectEventInfo : EventInfo
    {
        protected ObjectEventInfo(object sourceValue, Type sourceType)
            : base(sourceValue, sourceType)
        {
        }

        public string Anchor { get; set; }
        public string Tag { get; set; }
    }

    public sealed class ScalarEventInfo : ObjectEventInfo
    {
        public ScalarEventInfo(object sourceValue, Type sourceType)
            : base(sourceValue, sourceType)
        {
        }

        public string RenderedValue { get; set; }
        public ScalarStyle Style { get; set; }
        public bool IsPlainImplicit { get; set; }
        public bool IsQuotedImplicit { get; set; }
    }

    public sealed class MappingStartEventInfo : ObjectEventInfo
    {
        public MappingStartEventInfo(object sourceValue, Type sourceType)
            : base(sourceValue, sourceType)
        {
        }

        public bool IsImplicit { get; set; }
        public DataStyle Style { get; set; }
    }

    public sealed class MappingEndEventInfo : EventInfo
    {
        public MappingEndEventInfo(object sourceValue, Type sourceType)
            : base(sourceValue, sourceType)
        {
        }
    }

    public sealed class SequenceStartEventInfo : ObjectEventInfo
    {
        public SequenceStartEventInfo(object sourceValue, Type sourceType)
            : base(sourceValue, sourceType)
        {
        }

        public bool IsImplicit { get; set; }
        public DataStyle Style { get; set; }
    }

    public sealed class SequenceEndEventInfo : EventInfo
    {
        public SequenceEndEventInfo(object sourceValue, Type sourceType)
            : base(sourceValue, sourceType)
        {
        }
    }
}