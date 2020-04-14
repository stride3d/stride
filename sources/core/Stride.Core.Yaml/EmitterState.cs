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

namespace Stride.Core.Yaml
{
    /// <summary>
    /// Defines the YAML emitter's state.
    /// </summary>
    internal enum EmitterState
    {
        /// <summary>
        /// Expect STREAM-START.
        /// </summary>
        YAML_EMIT_STREAM_START_STATE,

        /// <summary>
        /// Expect the first DOCUMENT-START or STREAM-END.
        /// </summary>
        YAML_EMIT_FIRST_DOCUMENT_START_STATE,

        /// <summary>
        /// Expect DOCUMENT-START or STREAM-END.
        /// </summary>
        YAML_EMIT_DOCUMENT_START_STATE,

        /// <summary>
        /// Expect the content of a document.
        /// </summary>
        YAML_EMIT_DOCUMENT_CONTENT_STATE,

        /// <summary>
        /// Expect DOCUMENT-END.
        /// </summary>
        YAML_EMIT_DOCUMENT_END_STATE,

        /// <summary>
        /// Expect the first item of a flow sequence.
        /// </summary>
        YAML_EMIT_FLOW_SEQUENCE_FIRST_ITEM_STATE,

        /// <summary>
        /// Expect an item of a flow sequence.
        /// </summary>
        YAML_EMIT_FLOW_SEQUENCE_ITEM_STATE,

        /// <summary>
        /// Expect the first key of a flow mapping.
        /// </summary>
        YAML_EMIT_FLOW_MAPPING_FIRST_KEY_STATE,

        /// <summary>
        /// Expect a key of a flow mapping.
        /// </summary>
        YAML_EMIT_FLOW_MAPPING_KEY_STATE,

        /// <summary>
        /// Expect a value for a simple key of a flow mapping.
        /// </summary>
        YAML_EMIT_FLOW_MAPPING_SIMPLE_VALUE_STATE,

        /// <summary>
        /// Expect a value of a flow mapping.
        /// </summary>
        YAML_EMIT_FLOW_MAPPING_VALUE_STATE,

        /// <summary>
        /// Expect the first item of a block sequence.
        /// </summary>
        YAML_EMIT_BLOCK_SEQUENCE_FIRST_ITEM_STATE,

        /// <summary>
        /// Expect an item of a block sequence.
        /// </summary>
        YAML_EMIT_BLOCK_SEQUENCE_ITEM_STATE,

        /// <summary>
        /// Expect the first key of a block mapping.
        /// </summary>
        YAML_EMIT_BLOCK_MAPPING_FIRST_KEY_STATE,

        /// <summary>
        /// Expect the key of a block mapping.
        /// </summary>
        YAML_EMIT_BLOCK_MAPPING_KEY_STATE,

        /// <summary>
        /// Expect a value for a simple key of a block mapping.
        /// </summary>
        YAML_EMIT_BLOCK_MAPPING_SIMPLE_VALUE_STATE,

        /// <summary>
        /// Expect a value of a block mapping.
        /// </summary>
        YAML_EMIT_BLOCK_MAPPING_VALUE_STATE,

        /// <summary>
        /// Expect nothing.
        /// </summary>
        YAML_EMIT_END_STATE
    }
}