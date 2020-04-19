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
using System.IO;
using System.Text;
using Xunit;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Yaml.Tests.Serialization
{
    public class YamlStreamTests : YamlTest
    {
        [Fact]
        public void LoadSimpleDocument()
        {
            var stream = new YamlStream();
            stream.Load(YamlFile("test2.yaml"));

            Assert.Equal(1, stream.Documents.Count);
            Assert.True(stream.Documents[0].RootNode is YamlScalarNode);
            Assert.Equal("a scalar", ((YamlScalarNode) stream.Documents[0].RootNode).Value);
        }

        [Fact]
        public void BackwardAliasReferenceWorks()
        {
            var stream = new YamlStream();
            stream.Load(YamlFile("backwardsAlias.yaml"));

            Assert.Equal(1, stream.Documents.Count);
            Assert.True(stream.Documents[0].RootNode is YamlSequenceNode);

            var sequence = (YamlSequenceNode) stream.Documents[0].RootNode;
            Assert.Equal(3, sequence.Children.Count);

            Assert.Equal("a scalar", ((YamlScalarNode) sequence.Children[0]).Value);
            Assert.Equal("another scalar", ((YamlScalarNode) sequence.Children[1]).Value);
            Assert.Equal("a scalar", ((YamlScalarNode) sequence.Children[2]).Value);
            Assert.Equal(sequence.Children[0], sequence.Children[2]);
        }

        [Fact]
        public void ForwardAliasReferenceWorks()
        {
            var stream = new YamlStream();
            stream.Load(YamlFile("forwardAlias.yaml"));

            Assert.Equal(1, stream.Documents.Count);
            Assert.True(stream.Documents[0].RootNode is YamlSequenceNode);

            var sequence = (YamlSequenceNode) stream.Documents[0].RootNode;
            Assert.Equal(3, sequence.Children.Count);

            Assert.Equal("a scalar", ((YamlScalarNode) sequence.Children[0]).Value);
            Assert.Equal("another scalar", ((YamlScalarNode) sequence.Children[1]).Value);
            Assert.Equal("a scalar", ((YamlScalarNode) sequence.Children[2]).Value);
            Assert.Equal(sequence.Children[0], sequence.Children[2]);
        }

        [Fact]
        public void RoundtripExample1()
        {
            RoundtripTest("test1.yaml");
        }

        [Fact]
        public void RoundtripExample2()
        {
            RoundtripTest("test2.yaml");
        }

        [Fact]
        public void RoundtripExample3()
        {
            RoundtripTest("test3.yaml");
        }

        [Fact]
        public void RoundtripExample4()
        {
            RoundtripTest("test4.yaml");
        }

        [Fact]
        public void RoundtripExample5()
        {
            RoundtripTest("test6.yaml");
        }

        [Fact]
        public void RoundtripExample6()
        {
            RoundtripTest("test6.yaml");
        }

        [Fact]
        public void RoundtripExample7()
        {
            RoundtripTest("test7.yaml");
        }

        [Fact]
        public void RoundtripExample8()
        {
            RoundtripTest("test8.yaml");
        }

        [Fact]
        public void RoundtripExample9()
        {
            RoundtripTest("test9.yaml");
        }

        [Fact]
        public void RoundtripExample10()
        {
            RoundtripTest("test10.yaml");
        }

        [Fact]
        public void RoundtripExample11()
        {
            RoundtripTest("test11.yaml");
        }

        [Fact]
        public void RoundtripExample12()
        {
            RoundtripTest("test12.yaml");
        }

        [Fact]
        public void RoundtripExample13()
        {
            RoundtripTest("test13.yaml");
        }

        [Fact]
        public void RoundtripExample14()
        {
            RoundtripTest("test14.yaml");
        }

        [Fact]
        public void RoundtripBackreference()
        {
            RoundtripTest("backreference.yaml");
        }

        [Fact]
        public void FailBackreference()
        {
            RoundtripTest("fail-backreference.yaml");
        }

        [Fact]
        public void RoundtripTags()
        {
            RoundtripTest("tags.yaml");
        }

        [Fact]
        public void AllAliasesMustBeResolved()
        {
            var original = new YamlStream();
            Assert.Throws<AnchorNotFoundException>(() => original.Load(YamlFile("invalid-reference.yaml")));
        }

        private void RoundtripTest(string yamlFileName)
        {
            var original = new YamlStream();
            original.Load(YamlFile(yamlFileName));

            var buffer = new StringBuilder();
            original.Save(new StringWriter(buffer));

            Dump.WriteLine(buffer);

            var final = new YamlStream();
            final.Load(new StringReader(buffer.ToString()));

            var originalBuilder = new YamlDocumentStructureBuilder();
            original.Accept(originalBuilder);

            var finalBuilder = new YamlDocumentStructureBuilder();
            final.Accept(finalBuilder);

            Dump.WriteLine("The original document produced {0} events.", originalBuilder.Events.Count);
            Dump.WriteLine("The final document produced {0} events.", finalBuilder.Events.Count);
            Assert.Equal(originalBuilder.Events.Count, finalBuilder.Events.Count);

            for (var i = 0; i < originalBuilder.Events.Count; ++i)
            {
                var originalEvent = originalBuilder.Events[i];
                var finalEvent = finalBuilder.Events[i];

                Assert.Equal(originalEvent.Type, finalEvent.Type);
                Assert.Equal(originalEvent.Value, finalEvent.Value);
            }
        }

        private class YamlDocumentStructureBuilder : YamlVisitor
        {
            private readonly List<YamlNodeEvent> events = new List<YamlNodeEvent>();

            public IList<YamlNodeEvent> Events { get { return events; } }

            protected override void Visit(YamlScalarNode scalar)
            {
                events.Add(new YamlNodeEvent(YamlNodeEventType.Scalar, scalar.Anchor, scalar.Tag, scalar.Value));
            }

            protected override void Visit(YamlSequenceNode sequence)
            {
                events.Add(new YamlNodeEvent(YamlNodeEventType.SequenceStart, sequence.Anchor, sequence.Tag, null));
            }

            protected override void Visited(YamlSequenceNode sequence)
            {
                events.Add(new YamlNodeEvent(YamlNodeEventType.SequenceEnd, sequence.Anchor, sequence.Tag, null));
            }

            protected override void Visit(YamlMappingNode mapping)
            {
                events.Add(new YamlNodeEvent(YamlNodeEventType.MappingStart, mapping.Anchor, mapping.Tag, null));
            }

            protected override void Visited(YamlMappingNode mapping)
            {
                events.Add(new YamlNodeEvent(YamlNodeEventType.MappingEnd, mapping.Anchor, mapping.Tag, null));
            }
        }

        private class YamlNodeEvent
        {
            public YamlNodeEventType Type { get; private set; }
            public string Anchor { get; private set; }
            public string Tag { get; private set; }
            public string Value { get; private set; }

            public YamlNodeEvent(YamlNodeEventType type, string anchor, string tag, string value)
            {
                Type = type;
                Anchor = anchor;
                Tag = tag;
                Value = value;
            }
        }

        private enum YamlNodeEventType
        {
            SequenceStart,
            SequenceEnd,
            MappingStart,
            MappingEnd,
            Scalar,
        }

        // Todo: Sample.. belongs elsewhere?
        [Fact]
        public void RoundtripSample()
        {
            var original = new YamlStream();
            original.Load(YamlFile("sample.yaml"));
            original.Accept(new TracingVisitor());
        }
    }
}
