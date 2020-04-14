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
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Yaml.Tests.Serialization
{
    public class TracingVisitor : YamlVisitor
    {
        private int indent = 0;

        private void WriteIndent()
        {
            for (int i = 0; i < indent; ++i)
            {
                Console.Write("  ");
            }
        }

        protected override void Visit(YamlDocument document)
        {
            WriteIndent();
            Console.WriteLine("Visit(YamlDocument)");
            ++indent;
        }

        protected override void Visit(YamlMappingNode mapping)
        {
            WriteIndent();
            Console.WriteLine("Visit(YamlMapping, {0}, {1})", mapping.Anchor, mapping.Tag);
            ++indent;
        }

        protected override void Visit(YamlScalarNode scalar)
        {
            WriteIndent();
            Console.WriteLine("Visit(YamlScalarNode, {0}, {1}) - {2}", scalar.Anchor, scalar.Tag, scalar.Value);
            ++indent;
        }

        protected override void Visit(YamlSequenceNode sequence)
        {
            WriteIndent();
            Console.WriteLine("Visit(YamlSequenceNode, {0}, {1})", sequence.Anchor, sequence.Tag);
            ++indent;
        }

        protected override void Visit(YamlStream stream)
        {
            WriteIndent();
            Console.WriteLine("Visit(YamlStream)");
            ++indent;
        }

        protected override void Visited(YamlDocument document)
        {
            --indent;
            WriteIndent();
            Console.WriteLine("Visited(YamlDocument)");
        }

        protected override void Visited(YamlMappingNode mapping)
        {
            --indent;
            WriteIndent();
            Console.WriteLine("Visited(YamlMappingNode)");
        }

        protected override void Visited(YamlScalarNode scalar)
        {
            --indent;
            WriteIndent();
            Console.WriteLine("Visited(YamlScalarNode)");
        }

        protected override void Visited(YamlSequenceNode sequence)
        {
            --indent;
            WriteIndent();
            Console.WriteLine("Visited(YamlSequenceNode)");
        }

        protected override void Visited(YamlStream stream)
        {
            --indent;
            WriteIndent();
            Console.WriteLine("Visited(YamlStream)");
        }
    }
}