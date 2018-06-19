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
using NUnit.Framework;
using Xenko.Core.Yaml.Serialization;

namespace Xenko.Core.Yaml.Tests.Serialization
{
    public class YamlStreamTests : YamlTest
    {
        [Test]
        public void LoadSimpleDocument()
        {
            var stream = new YamlStream();
            stream.Load(YamlFile("test2.yaml"));

            Assert.AreEqual(1, stream.Documents.Count);
            Assert.IsInstanceOf<YamlScalarNode>(stream.Documents[0].RootNode);
            Assert.AreEqual("a scalar", ((YamlScalarNode) stream.Documents[0].RootNode).Value);
        }

        [Test]
        public void BackwardAliasReferenceWorks()
        {
            var stream = new YamlStream();
            stream.Load(YamlFile("backwardsAlias.yaml"));

            Assert.AreEqual(1, stream.Documents.Count);
            Assert.IsInstanceOf<YamlSequenceNode>(stream.Documents[0].RootNode);

            var sequence = (YamlSequenceNode) stream.Documents[0].RootNode;
            Assert.AreEqual(3, sequence.Children.Count);

            Assert.AreEqual("a scalar", ((YamlScalarNode) sequence.Children[0]).Value);
            Assert.AreEqual("another scalar", ((YamlScalarNode) sequence.Children[1]).Value);
            Assert.AreEqual("a scalar", ((YamlScalarNode) sequence.Children[2]).Value);
            Assert.AreSame(sequence.Children[0], sequence.Children[2]);
        }

        [Test]
        public void ForwardAliasReferenceWorks()
        {
            var stream = new YamlStream();
            stream.Load(YamlFile("forwardAlias.yaml"));

            Assert.AreEqual(1, stream.Documents.Count);
            Assert.IsInstanceOf<YamlSequenceNode>(stream.Documents[0].RootNode);

            var sequence = (YamlSequenceNode) stream.Documents[0].RootNode;
            Assert.AreEqual(3, sequence.Children.Count);

            Assert.AreEqual("a scalar", ((YamlScalarNode) sequence.Children[0]).Value);
            Assert.AreEqual("another scalar", ((YamlScalarNode) sequence.Children[1]).Value);
            Assert.AreEqual("a scalar", ((YamlScalarNode) sequence.Children[2]).Value);
            Assert.AreSame(sequence.Children[0], sequence.Children[2]);
        }

        [Test]
        public void RoundtripExample1()
        {
            RoundtripTest("test1.yaml");
        }

        [Test]
        public void RoundtripExample2()
        {
            RoundtripTest("test2.yaml");
        }

        [Test]
        public void RoundtripExample3()
        {
            RoundtripTest("test3.yaml");
        }

        [Test]
        public void RoundtripExample4()
        {
            RoundtripTest("test4.yaml");
        }

        [Test]
        public void RoundtripExample5()
        {
            RoundtripTest("test6.yaml");
        }

        [Test]
        public void RoundtripExample6()
        {
            RoundtripTest("test6.yaml");
        }

        [Test]
        public void RoundtripExample7()
        {
            RoundtripTest("test7.yaml");
        }

        [Test]
        public void RoundtripExample8()
        {
            RoundtripTest("test8.yaml");
        }

        [Test]
        public void RoundtripExample9()
        {
            RoundtripTest("test9.yaml");
        }

        [Test]
        public void RoundtripExample10()
        {
            RoundtripTest("test10.yaml");
        }

        [Test]
        public void RoundtripExample11()
        {
            RoundtripTest("test11.yaml");
        }

        [Test]
        public void RoundtripExample12()
        {
            RoundtripTest("test12.yaml");
        }

        [Test]
        public void RoundtripExample13()
        {
            RoundtripTest("test13.yaml");
        }

        [Test]
        public void RoundtripExample14()
        {
            RoundtripTest("test14.yaml");
        }

        [Test]
        public void RoundtripBackreference()
        {
            RoundtripTest("backreference.yaml");
        }

        [Test]
        public void FailBackreference()
        {
            RoundtripTest("fail-backreference.yaml");
        }

        [Test]
        public void RoundtripTags()
        {
            RoundtripTest("tags.yaml");
        }

        [Test]
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
            Assert.AreEqual(originalBuilder.Events.Count, finalBuilder.Events.Count);

            for (var i = 0; i < originalBuilder.Events.Count; ++i)
            {
                var originalEvent = originalBuilder.Events[i];
                var finalEvent = finalBuilder.Events[i];

                Assert.AreEqual(originalEvent.Type, finalEvent.Type);
                Assert.AreEqual(originalEvent.Value, finalEvent.Value);
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
        [Test]
        public void RoundtripSample()
        {
            var original = new YamlStream();
            original.Load(YamlFile("sample.yaml"));
            original.Accept(new TracingVisitor());
        }
    }
}