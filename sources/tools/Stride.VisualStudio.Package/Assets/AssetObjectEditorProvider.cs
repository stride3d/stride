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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.IO;
using System.Text.RegularExpressions;
using Stride.Core;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Schemas;
using Stride.Core.Yaml.Tokens;

namespace Stride.VisualStudio.Assets
{

	#region Provider definition
	/// <summary>
	/// This class causes a classifier to be added to the set of classifiers. Since 
	/// the content type is set to "text", this classifier applies to all text files
	/// </summary>
	[Export(typeof(IClassifierProvider))]
	[ContentType(Constants.ContentType)]
	internal class AssetObjectEditorProvider : IClassifierProvider
	{
		/// <summary>
		/// Import the classification registry to be used for getting a reference
		/// to the custom classification type later.
		/// </summary>
		[Import]
		internal IClassificationTypeRegistryService ClassificationRegistry = null; // Set via MEF

		public IClassifier GetClassifier(ITextBuffer buffer)
		{
			return buffer.Properties.GetOrCreateSingletonProperty<AssetObjectEditor>(delegate { return new AssetObjectEditor(ClassificationRegistry); });
		}
	}
	#endregion //provider def

	#region Classifier
	/// <summary>
	/// Classifier that classifies all text as an instance of the OrinaryClassifierType
	/// </summary>
	class AssetObjectEditor : IClassifier
	{
		private readonly CoreSchema schema;
		private readonly IClassificationType _comment;
		private readonly IClassificationType _anchor;
		private readonly IClassificationType _alias;
		private readonly IClassificationType _key;
		private readonly IClassificationType _value;
        private readonly IClassificationType _bool;
        private readonly IClassificationType _number;
		private readonly IClassificationType _string;
		private readonly IClassificationType _tag;
		private readonly IClassificationType _symbol;
		private readonly IClassificationType _directive;
		private readonly IClassificationType _tab;

		internal AssetObjectEditor(IClassificationTypeRegistryService registry)
		{
			schema = new CoreSchema();
            _comment = registry.GetClassificationType(PredefinedClassificationTypeNames.Comment);
            _anchor = registry.GetClassificationType(AssetObjectDefinitions.AnchorClassificationName);
            _alias = registry.GetClassificationType(AssetObjectDefinitions.AliasClassificationName);
            _key = registry.GetClassificationType(AssetObjectDefinitions.KeyClassificationName);
            _value = registry.GetClassificationType(PredefinedClassificationTypeNames.Identifier);
            _number = registry.GetClassificationType(AssetObjectDefinitions.NumberClassificationName);
            _string = registry.GetClassificationType(PredefinedClassificationTypeNames.String);
            _bool = registry.GetClassificationType(PredefinedClassificationTypeNames.Keyword);
            _tag = registry.GetClassificationType(PredefinedClassificationTypeNames.SymbolDefinition);
            _symbol = registry.GetClassificationType(PredefinedClassificationTypeNames.Operator);
            _directive = registry.GetClassificationType(PredefinedClassificationTypeNames.PreprocessorKeyword);
            _tab = registry.GetClassificationType(AssetObjectDefinitions.ErrorClassificationName);
        }

		/// <summary>
		/// This method scans the given SnapshotSpan for potential matches for this classification.
		/// In this instance, it classifies everything and returns each span as a new ClassificationSpan.
		/// </summary>
		/// <param name="trackingSpan">The span currently being classified</param>
		/// <returns>A list of ClassificationSpans that represent spans identified to be of this classification</returns>
		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
		{
			var classifications = new List<ClassificationSpan>();

			var text = span.GetText();

			var commentIndex = text.IndexOf('#');
			if (commentIndex >= 0)
			{
				classifications.Add(
					new ClassificationSpan(
						new SnapshotSpan(
							span.Snapshot,
							new Span(span.Start + commentIndex, span.Length - commentIndex)
						),
						_comment
					)
				);

				text = text.Substring(0, commentIndex);
			}

			var match = Regex.Match(text, @"^( *(\t+))+");
			if (match.Success)
			{
				foreach (Capture capture in match.Groups[2].Captures)
				{
					classifications.Add(
						new ClassificationSpan(
							new SnapshotSpan(
								span.Snapshot,
								new Span(span.Start + capture.Index, capture.Length)
							),
							_tab
						)
					);
				}
			}

			try
			{
				var scanner = new Scanner(new StringReader(text));

				Type previousTokenType = null;
				while (scanner.MoveNext())
				{
					IClassificationType classificationType = null;

					var currentTokenType = scanner.Current.GetType();
					var tokenLength = scanner.Current.End.Index - scanner.Current.Start.Index;

					if (currentTokenType == typeof(Anchor))
					{
						classificationType = _anchor;
					}
					else if (currentTokenType == typeof(AnchorAlias))
					{
						classificationType = _alias;
					}
					else if (currentTokenType == typeof(Scalar))
					{
						if (previousTokenType == typeof (Key))
						{
							classificationType = _key;
						}
						else
						{
							// Decode the scalar
							var scalarToken = (Scalar) scanner.Current;
							var scalar = new Core.Yaml.Events.Scalar(scalarToken.Value);
							switch (schema.GetDefaultTag(scalar))
							{
								case JsonSchema.BoolShortTag:
                                    classificationType = _bool;
                                    break;
                                case JsonSchema.FloatShortTag:
								case JsonSchema.IntShortTag:
									classificationType = _number;
									break;
								case SchemaBase.StrShortTag:
                                    classificationType = scalarToken.Style == ScalarStyle.DoubleQuoted || scalarToken.Style == ScalarStyle.SingleQuoted ? _string : _value;
									break;
								default:
									classificationType = _value;
									break;
							}
						}
						
					}
					else if (currentTokenType == typeof(Tag))
					{
						classificationType = _tag;
					}
					else if (currentTokenType == typeof(TagDirective))
					{
						classificationType = _directive;
					}
					else if (currentTokenType == typeof(VersionDirective))
					{
						classificationType = _directive;
					}
					else if (tokenLength > 0)
					{
						classificationType = _symbol;
					}

					previousTokenType = currentTokenType;

					if (classificationType != null && tokenLength > 0)
					{
						classifications.Add(
							new ClassificationSpan(
								new SnapshotSpan(
									span.Snapshot,
									new Span(span.Start + scanner.Current.Start.Index, tokenLength)
								),
								classificationType
							)
						);
					}
				}
			}
			catch (Exception ex)
			{
                Trace.WriteLine("Exception in AssetObjectEditor " + ex);
			}

			return classifications;
		}

#pragma warning disable 67
		// This event gets raised if a non-text change would affect the classification in some way,
		// for example typing /* would cause the classification to change in C# without directly
		// affecting the span.
		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
#pragma warning restore 67
	}
	#endregion //Classifier
}
