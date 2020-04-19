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

namespace Stride.Core.Yaml.Events
{
    /// <summary>
    /// Defines the event types. This allows for simpler type comparisons.
    /// </summary>
    internal enum EventType
    {
        /// <summary>
        /// An empty event.
        /// </summary>
        YAML_NO_EVENT,

        /// <summary>
        /// A STREAM-START event.
        /// </summary>
        YAML_STREAM_START_EVENT,

        /// <summary>
        /// A STREAM-END event.
        /// </summary>
        YAML_STREAM_END_EVENT,

        /// <summary>
        /// A DOCUMENT-START event.
        /// </summary>
        YAML_DOCUMENT_START_EVENT,

        /// <summary>
        /// A DOCUMENT-END event.
        /// </summary>
        YAML_DOCUMENT_END_EVENT,

        /// <summary>
        /// An ALIAS event.
        /// </summary>
        YAML_ALIAS_EVENT,

        /// <summary>
        /// A SCALAR event.
        /// </summary>
        YAML_SCALAR_EVENT,

        /// <summary>
        /// A SEQUENCE-START event.
        /// </summary>
        YAML_SEQUENCE_START_EVENT,

        /// <summary>
        /// A SEQUENCE-END event.
        /// </summary>
        YAML_SEQUENCE_END_EVENT,

        /// <summary>
        /// A MAPPING-START event.
        /// </summary>
        YAML_MAPPING_START_EVENT,

        /// <summary>
        /// A MAPPING-END event.
        /// </summary>
        YAML_MAPPING_END_EVENT,
    }
}