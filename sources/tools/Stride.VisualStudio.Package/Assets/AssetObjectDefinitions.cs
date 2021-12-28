// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
//  This file is part of YamlDotNet - A .NET library for YAML.
//  Copyright (c) 2008, 2009, 2010, 2011, 2012, 2013 Antoine Aubry

//  Permission is hereby granted, free of charge, to any person obtaining a copy of
//  this software and associated documentation files (the "Software"), to deal in
//  the Software without restriction, including without limitation the rights to
//  use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
//  of the Software, and to permit persons to whom the Software is furnished to do
//  so, subject to the following conditions:

//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.

//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Stride.VisualStudio.Assets
{
	internal static class AssetObjectDefinitions
	{
        /// <summary>
        /// Content Type
        /// </summary>
        [Export]
        [Name(Constants.ContentType)]
        [BaseDefinition("code")]
        internal static ContentTypeDefinition hidingContentTypeDefinition = null;

        /// <summary>
        /// File extensions
        /// </summary>
		[Export]
		[FileExtension(".sdpkg")]
		[ContentType(Constants.ContentType)]
		internal static FileExtensionToContentTypeDefinition sdpkgFileExtensionDefinition = null;
		
		[Export]
        [FileExtension(".sdfnt")]
        [ContentType(Constants.ContentType)]
        internal static FileExtensionToContentTypeDefinition sdfntFileExtensionDefinition = null;

        [Export]
        [FileExtension(".sdfxlib")]
        [ContentType(Constants.ContentType)]
        internal static FileExtensionToContentTypeDefinition sdfxlibFileExtensionDefinition = null;

        [Export]
        [FileExtension(".sdlightconf")]
        [ContentType(Constants.ContentType)]
        internal static FileExtensionToContentTypeDefinition sdlightconfFileExtensionDefinition = null;

        [Export]
        [FileExtension(".sdtex")]
        [ContentType(Constants.ContentType)]
        internal static FileExtensionToContentTypeDefinition sdtexFileExtensionDefinition = null;

        [Export]
        [FileExtension(".sdscene")]
        [ContentType(Constants.ContentType)]
        internal static FileExtensionToContentTypeDefinition sdsceneFileExtensionDefinition = null;

	    [Export]
	    [FileExtension(".sdprefab")]
	    [ContentType(Constants.ContentType)]
	    internal static FileExtensionToContentTypeDefinition sdprefabFileExtensionDefinition = null;

        [Export]
        [FileExtension(".sdm3d")]
        [ContentType(Constants.ContentType)]
        internal static FileExtensionToContentTypeDefinition sdm3dFileExtensionDefinition = null;

        [Export]
        [FileExtension(".sdanim")]
        [ContentType(Constants.ContentType)]
        internal static FileExtensionToContentTypeDefinition sdanimFileExtensionDefinition = null;

        [Export]
        [FileExtension(".sdsnd")]
        [ContentType(Constants.ContentType)]
        internal static FileExtensionToContentTypeDefinition sdsndFileExtensionDefinition = null;

        [Export]
        [FileExtension(".sdmat")]
        [ContentType(Constants.ContentType)]
        internal static FileExtensionToContentTypeDefinition sdmatFileExtensionDefinition = null;

        [Export]
        [FileExtension(".sdsprite")]
        [ContentType(Constants.ContentType)]
        internal static FileExtensionToContentTypeDefinition sdsprtFileExtensionDefinition = null;

        /// <summary>
        /// Classification type definitions
        /// </summary>
        [Export(typeof(ClassificationTypeDefinition))]
        [Name(AnchorClassificationName)]
		internal static ClassificationTypeDefinition YamlAnchorType = null;
	    public const string AnchorClassificationName = "Stride.Yaml.Anchor";

		[Export(typeof(ClassificationTypeDefinition))]
        [Name(AliasClassificationName)]
		internal static ClassificationTypeDefinition YamlAliasType = null;
        public const string AliasClassificationName = "Stride.Yaml.Alias";

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(NumberClassificationName)]
        internal static ClassificationTypeDefinition YamlNumberType = null;
        public const string NumberClassificationName = "Stride.Yaml.Number";

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(KeyClassificationName)]
        internal static ClassificationTypeDefinition YamlKeyType = null;
        public const string KeyClassificationName = "Stride.Yaml.Key";

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(ErrorClassificationName)]
        internal static ClassificationTypeDefinition YamlErrorType = null;
        public const string ErrorClassificationName = "Stride.Yaml.Error";
    }
}
