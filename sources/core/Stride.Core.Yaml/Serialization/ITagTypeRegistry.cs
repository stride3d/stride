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
using System.Reflection;
using Stride.Core.Reflection;

namespace Stride.Core.Yaml.Serialization
{
    /// <summary>
    /// Prodives tag discovery from a type and type discovery from a tag.
    /// </summary>
    public interface ITagTypeRegistry : ITagTypeResolver
    {
        /// <summary>
        /// Registers an assembly when trying to resolve types. All types
        /// having <see cref="DataMemberAttribute" /> will be registered
        /// automatically.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="attributeRegistry">The attribute registry to use when quering for <see cref="DataMemberAttribute"/>.</param>
        void RegisterAssembly(Assembly assembly, IAttributeRegistry attributeRegistry);

        /// <summary>
        /// Register a mapping between a tag and a type.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="type">The type.</param>
        /// <param name="alias">if set to <c>true</c> the specified tag is an alias to an existing type that has already a tag associated with it (remap).</param>
        void RegisterTagMapping(string tag, Type type, bool alias);
    }
}
