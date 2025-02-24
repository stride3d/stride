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
using Stride.Core.Yaml.Tokens;
using AnchorAlias = Stride.Core.Yaml.Events.AnchorAlias;
using DocumentEnd = Stride.Core.Yaml.Events.DocumentEnd;
using DocumentStart = Stride.Core.Yaml.Events.DocumentStart;
using Scalar = Stride.Core.Yaml.Events.Scalar;
using StreamEnd = Stride.Core.Yaml.Events.StreamEnd;
using StreamStart = Stride.Core.Yaml.Events.StreamStart;

namespace Stride.Core.Yaml.Tests;

public class ParserTestHelper : YamlTest
{
    protected const bool Explicit = false;
    protected const bool Implicit = true;
    protected const string TagYaml = "tag:yaml.org,2002:";

    protected static readonly TagDirective[] DefaultTags =
    [
        new TagDirective("!", "!"),
        new TagDirective("!!", TagYaml)
    ];

    protected static StreamStart StreamStart { get { return new StreamStart(); } }

    protected static StreamEnd StreamEnd { get { return new StreamEnd(); } }

    protected static DocumentStart DocumentStart(bool isImplicit)
    {
        return DocumentStart(isImplicit, null, DefaultTags);
    }

    protected static DocumentStart DocumentStart(bool isImplicit, VersionDirective version, params TagDirective[] tags)
    {
        return new DocumentStart(version, new TagDirectiveCollection(tags), isImplicit);
    }

    protected static VersionDirective Version(int major, int minor)
    {
        return new VersionDirective(new Version(major, minor));
    }

    protected static TagDirective TagDirective(string handle, string prefix)
    {
        return new TagDirective(handle, prefix);
    }

    protected static DocumentEnd DocumentEnd(bool isImplicit)
    {
        return new DocumentEnd(isImplicit);
    }

    protected static Scalar PlainScalar(string text)
    {
        return new Scalar(null, null, text, ScalarStyle.Plain, true, false);
    }

    protected static Scalar SingleQuotedScalar(string text)
    {
        return new Scalar(null, null, text, ScalarStyle.SingleQuoted, false, true);
    }

    protected static Scalar DoubleQuotedScalar(string text)
    {
        return DoubleQuotedScalar(null, text);
    }

    protected static Scalar ExplicitDoubleQuotedScalar(string tag, string text)
    {
        return DoubleQuotedScalar(tag, text, false);
    }

    protected static Scalar DoubleQuotedScalar(string tag, string text, bool quotedImplicit = true)
    {
        return new Scalar(null, tag, text, ScalarStyle.DoubleQuoted, false, quotedImplicit);
    }

    protected static Scalar LiteralScalar(string text)
    {
        return new Scalar(null, null, text, ScalarStyle.Literal, false, true);
    }

    protected static Scalar FoldedScalar(string text)
    {
        return new Scalar(null, null, text, ScalarStyle.Folded, false, true);
    }

    protected static SequenceStart BlockSequenceStart => new(null, null, true, DataStyle.Normal);

    protected static SequenceStart FlowSequenceStart => new(null, null, true, DataStyle.Compact);

    protected static SequenceStart AnchoredFlowSequenceStart(string anchor)
    {
        return new SequenceStart(anchor, null, true, DataStyle.Compact);
    }

    protected static SequenceEnd SequenceEnd => new();

    protected static MappingStart BlockMappingStart => new(null, null, true, DataStyle.Normal);

    protected static MappingStart TaggedBlockMappingStart(string tag)
    {
        return new MappingStart(null, tag, false, DataStyle.Normal);
    }

    protected static MappingStart FlowMappingStart => new(null, null, true, DataStyle.Compact);

    protected static MappingEnd MappingEnd => new();

    protected static AnchorAlias AnchorAlias(string alias)
    {
        return new AnchorAlias(alias);
    }
}
