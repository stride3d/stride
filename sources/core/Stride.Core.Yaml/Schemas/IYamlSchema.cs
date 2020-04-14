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

namespace Stride.Core.Yaml.Schemas
{
    /// <summary>
    /// Provides schema information for tag resolution.
    /// </summary>
    public interface IYamlSchema
    {
        /// <summary>
        /// Expands the tag. Example, transforms a short tag '!!str' to its long version 'tag:yaml.org,2002:str'
        /// </summary>
        /// <param name="shortTag">The tag.</param>
        /// <returns>Expanded version of the tag.</returns>
        string ExpandTag(string shortTag);

        /// <summary>
        /// Shortens the tag. Example, transforms a long tag 'tag:yaml.org,2002:str' to its short version '!!str'
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>Expanded version of the tag.</returns>
        string ShortenTag(string tag);

        /// <summary>
        /// Gets the default tag for the specified <see cref="NodeEvent"/>. The default tag can be different from a actual tag of this <see cref="NodeEvent"/>.
        /// </summary>
        /// <param name="nodeEvent">The node event.</param>
        /// <returns>A short tag.</returns>
        string GetDefaultTag(NodeEvent nodeEvent);

        /// <summary>
        /// Gets the default tag for the specified <see cref="Type"/>. This is only valid for scalar, return null if no default tag found.
        /// </summary>
        /// <returns>A short tag.</returns>
        string GetDefaultTag(Type type);

        /// <summary>
        /// Determines whether the short tag implicit (e.g. !!int).
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns><c>true</c> if [is tag implicit] [the specified tag]; otherwise, <c>false</c>.</returns>
        bool IsTagImplicit(string tag);

        /// <summary>
        /// Gets the default tag and value for the specified <see cref="Scalar" />. The default tag can be different from a actual tag of this <see cref="NodeEvent" />.
        /// </summary>
        /// <param name="scalar">The scalar event.</param>
        /// <param name="decodeValue">if set to <c>true</c> [decode value].</param>
        /// <param name="defaultTag">The default tag decoded from the scalar.</param>
        /// <param name="value">The value extracted from a scalar.</param>
        /// <returns>System.String.</returns>
        bool TryParse(Scalar scalar, bool decodeValue, out string defaultTag, out object value);

        /// <summary>
        /// Gets the default tag and value for the specified <see cref="Scalar" />. The default tag can be different from a actual tag of this <see cref="NodeEvent" />.
        /// </summary>
        /// <param name="scalar">The scalar event.</param>
        /// <param name="type">The type.</param>
        /// <param name="value">The value extracted from a scalar.</param>
        /// <returns>System.String.</returns>
        bool TryParse(Scalar scalar, Type type, out object value);

        /// <summary>
        /// Gets the type for a default tag.
        /// </summary>
        /// <param name="tag">The tag in short form.</param>
        /// <returns>The type for a default tag or null if no default tag associated</returns>
        Type GetTypeForDefaultTag(string tag);

        /// <summary>
        /// Registers a long/short tag association.
        /// </summary>
        /// <param name="shortTag">The short tag.</param>
        /// <param name="longTag">The long tag.</param>
        /// <exception cref="System.ArgumentNullException">
        /// shortTag
        /// or
        /// shortTag
        /// </exception>
        void RegisterTag(string shortTag, string longTag);
    }
}