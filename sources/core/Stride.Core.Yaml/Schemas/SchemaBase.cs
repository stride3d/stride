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
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Stride.Core.Yaml.Events;

namespace Stride.Core.Yaml.Schemas
{
    /// <summary>
    /// Base implementation for a based schema.
    /// </summary>
    public abstract class SchemaBase : IYamlSchema
    {
        private readonly Dictionary<string, string> shortTagToLongTag = new Dictionary<string, string>();
        private readonly Dictionary<string, string> longTagToShortTag = new Dictionary<string, string>();
        private readonly List<ScalarResolutionRule> scalarTagResolutionRules = new List<ScalarResolutionRule>();
        private readonly Dictionary<string, Regex> algorithms = new Dictionary<string, Regex>();

        private readonly Dictionary<string, List<ScalarResolutionRule>> mapTagToScalarResolutionRuleList =
            new Dictionary<string, List<ScalarResolutionRule>>();

        private readonly Dictionary<Type, List<ScalarResolutionRule>> mapTypeToScalarResolutionRuleList =
            new Dictionary<Type, List<ScalarResolutionRule>>();

        private readonly Dictionary<Type, string> mapTypeToShortTag = new Dictionary<Type, string>();
        private readonly Dictionary<string, Type> mapShortTagToType = new Dictionary<string, Type>();

        private int updateCountter;
        private bool needFirstUpdate = true;

        protected SchemaBase()
        {
            RegisterDefaultTagMappings();
        }

        /// <summary>
        /// The string short tag: !!str
        /// </summary>
        public const string StrShortTag = "!!str";

        /// <summary>
        /// The string long tag: tag:yaml.org,2002:str
        /// </summary>
        public const string StrLongTag = "tag:yaml.org,2002:str";

        public string ExpandTag(string shortTag)
        {
            if (shortTag == null)
                return null;

            string tagExpanded;
            return shortTagToLongTag.TryGetValue(shortTag, out tagExpanded) ? tagExpanded : shortTag;
        }

        public string ShortenTag(string longTag)
        {
            if (longTag == null)
                return null;

            string tagShortened;
            return longTagToShortTag.TryGetValue(longTag, out tagShortened) ? tagShortened : longTag;
        }

        public string GetDefaultTag(NodeEvent nodeEvent)
        {
            EnsureScalarRules();

            if (nodeEvent == null)
                throw new ArgumentNullException("nodeEvent");

            var mapping = nodeEvent as MappingStart;
            if (mapping != null)
            {
                return GetDefaultTag(mapping);
            }

            var sequence = nodeEvent as SequenceStart;
            if (sequence != null)
            {
                return GetDefaultTag(sequence);
            }

            var scalar = nodeEvent as Scalar;
            if (scalar != null)
            {
                object value;
                string tag;
                TryParse(scalar, false, out tag, out value);
                return tag;
            }

            throw new NotSupportedException($"NodeEvent [{nodeEvent.GetType().FullName}] not supported");
        }

        public string GetDefaultTag(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            EnsureScalarRules();

            string defaultTag;
            mapTypeToShortTag.TryGetValue(type, out defaultTag);
            return defaultTag;
        }

        public bool IsTagImplicit(string tag)
        {
            if (tag == null)
            {
                return true;
            }
            return shortTagToLongTag.ContainsKey(tag);
        }

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
        public void RegisterTag(string shortTag, string longTag)
        {
            if (shortTag == null)
                throw new ArgumentNullException("shortTag");
            if (longTag == null)
                throw new ArgumentNullException("longTag");

            shortTagToLongTag[shortTag] = longTag;
            longTagToShortTag[longTag] = shortTag;
        }

        /// <summary>
        /// Gets the default tag for a <see cref="MappingStart"/> event.
        /// </summary>
        /// <param name="nodeEvent">The node event.</param>
        /// <returns>The default tag for a map.</returns>
        protected abstract string GetDefaultTag(MappingStart nodeEvent);

        /// <summary>
        /// Gets the default tag for a <see cref="SequenceStart"/> event.
        /// </summary>
        /// <param name="nodeEvent">The node event.</param>
        /// <returns>The default tag for a seq.</returns>
        protected abstract string GetDefaultTag(SequenceStart nodeEvent);

        public virtual bool TryParse(Scalar scalar, bool parseValue, out string defaultTag, out object value)
        {
            if (scalar == null)
                throw new ArgumentNullException("scalar");

            EnsureScalarRules();

            defaultTag = null;
            value = null;

            // DoubleQuoted and SingleQuoted string are always decoded
            if (scalar.Style == ScalarStyle.DoubleQuoted || scalar.Style == ScalarStyle.SingleQuoted)
            {
                defaultTag = StrShortTag;
                if (parseValue)
                {
                    value = scalar.Value;
                }
                return true;
            }

            // Parse only values if we have some rules
            if (scalarTagResolutionRules.Count > 0)
            {
                foreach (var rule in scalarTagResolutionRules)
                {
                    var match = rule.Pattern.Match(scalar.Value);
                    if (!match.Success)
                        continue;

                    defaultTag = rule.Tag;
                    if (parseValue)
                    {
                        value = rule.Decode(match);
                    }
                    return true;
                }
            }
            else
            {
                // Expand the tag to a default tag.
                defaultTag = ShortenTag(scalar.Tag);
            }

            // Value was not successfully decoded
            return false;
        }

        public bool TryParse(Scalar scalar, Type type, out object value)
        {
            if (scalar == null)
                throw new ArgumentNullException("scalar");
            if (type == null)
                throw new ArgumentNullException("type");

            EnsureScalarRules();

            value = null;

            // DoubleQuoted and SingleQuoted string are always decoded
            if (type == typeof(string) && (scalar.Style == ScalarStyle.DoubleQuoted || scalar.Style == ScalarStyle.SingleQuoted))
            {
                value = scalar.Value;
                return true;
            }

            // Parse only values if we have some rules
            if (mapTypeToScalarResolutionRuleList.Count > 0)
            {
                List<ScalarResolutionRule> rules;
                if (mapTypeToScalarResolutionRuleList.TryGetValue(type, out rules))
                {
                    foreach (var rule in rules)
                    {
                        var match = rule.Pattern.Match(scalar.Value);
                        if (match.Success)
                        {
                            value = rule.Decode(match);
                            return true;
                        }
                    }
                }
            }

            // Value was not successfully decoded
            return false;
        }

        public Type GetTypeForDefaultTag(string shortTag)
        {
            if (shortTag == null)
            {
                return null;
            }

            Type type;
            mapShortTagToType.TryGetValue(shortTag, out type);
            return type;
        }

        /// <summary>
        /// Prepare scalar rules. In the implementation of this method, should call <see cref="AddScalarRule{T}"/>
        /// </summary>
        protected virtual void PrepareScalarRules()
        {
        }

        /// <summary>
        /// Add a tag resolution rule that is invoked when <paramref name="regex" /> matches
        /// the <see cref="Scalar">Value of</see> a <see cref="Scalar" /> node.
        /// The tag is resolved to <paramref name="tag" /> and <paramref name="decode" /> is
        /// invoked when actual value of type <typeparamref name="T" /> is extracted from
        /// the node text.
        /// </summary>
        /// <typeparam name="T">Type of the scalar</typeparam>
        /// <param name="tag">The tag.</param>
        /// <param name="regex">The regex.</param>
        /// <param name="decode">The decode function.</param>
        /// <param name="encode">The encode function.</param>
        /// <example>
        ///   <code>
        /// BeginUpdate(); // to avoid invoking slow internal calculation method many times.
        /// Add( ... );
        /// Add( ... );
        /// Add( ... );
        /// Add( ... );
        /// EndUpdate();   // automaticall invoke internal calculation method
        ///   </code></example>
        protected void AddScalarRule<T>(string tag, string regex, Func<Match, T> decode, Func<T, string> encode)
        {
            // Make sure the tag is expanded to its long form
            var longTag = ShortenTag(tag);
            scalarTagResolutionRules.Add(new ScalarResolutionRule(longTag, regex, m => decode(m), m => encode((T) m), typeof(T)));
        }

        protected void AddScalarRule(Type[] types, string tag, string regex, Func<Match, object> decode, Func<object, string> encode)
        {
            // Make sure the tag is expanded to its long form
            var longTag = ShortenTag(tag);
            scalarTagResolutionRules.Add(new ScalarResolutionRule(longTag, regex, decode, encode, types));
        }

        protected void RegisterDefaultTagMapping<T>(string tag, bool isDefault = false)
        {
            if (tag == null)
                throw new ArgumentNullException("tag");
            RegisterDefaultTagMapping(tag, typeof(T), isDefault);
        }

        protected void RegisterDefaultTagMapping(string tag, Type type, bool isDefault)
        {
            if (tag == null)
                throw new ArgumentNullException("tag");
            if (type == null)
                throw new ArgumentNullException("type");

            if (!mapTypeToShortTag.ContainsKey(type))
                mapTypeToShortTag.Add(type, tag);

            if (isDefault)
            {
                mapShortTagToType[tag] = type;
            }
        }

        /// <summary>
        /// Allows to register tag mapping for all primitive types (e.g. int -> !!int)
        /// </summary>
        protected virtual void RegisterDefaultTagMappings()
        {
        }

        private void EnsureScalarRules()
        {
            lock (this)
            {
                if (needFirstUpdate || updateCountter != scalarTagResolutionRules.Count)
                {
                    PrepareScalarRules();
                    Update();
                    needFirstUpdate = false;
                }
            }
        }

        private void Update()
        {
            // Tag to joined regexp source
            var mapTagToPartialRegexPattern = new Dictionary<string, string>();
            foreach (var rule in scalarTagResolutionRules)
            {
                if (!mapTagToPartialRegexPattern.ContainsKey(rule.Tag))
                {
                    mapTagToPartialRegexPattern.Add(rule.Tag, rule.PatternSource);
                }
                else
                {
                    mapTagToPartialRegexPattern[rule.Tag] += "|" + rule.PatternSource;
                }
            }

            // Tag to joined regexp
            algorithms.Clear();
            foreach (var entry in mapTagToPartialRegexPattern)
            {
                algorithms.Add(
                    entry.Key,
                    new Regex("^(" + entry.Value + ")$")
                    );
            }

            // Tag to decoding methods
            mapTagToScalarResolutionRuleList.Clear();
            foreach (var rule in scalarTagResolutionRules)
            {
                if (!mapTagToScalarResolutionRuleList.ContainsKey(rule.Tag))
                    mapTagToScalarResolutionRuleList[rule.Tag] = new List<ScalarResolutionRule>();
                mapTagToScalarResolutionRuleList[rule.Tag].Add(rule);
            }

            mapTypeToScalarResolutionRuleList.Clear();
            foreach (var rule in scalarTagResolutionRules)
            {
                var types = rule.GetTypeOfValue();
                foreach (var type in types)
                {
                    if (!mapTypeToScalarResolutionRuleList.ContainsKey(type))
                        mapTypeToScalarResolutionRuleList[type] = new List<ScalarResolutionRule>();
                    mapTypeToScalarResolutionRuleList[type].Add(rule);
                }
            }

            // Update the counter
            updateCountter = scalarTagResolutionRules.Count;
        }

        private class ScalarResolutionRule
        {
            public ScalarResolutionRule(string shortTag, string regex, Func<Match, object> decoder, Func<object, string> encoder, params Type[] types)
            {
                Tag = shortTag;
                PatternSource = regex;
                Pattern = new Regex("^(?:" + regex + ")$");
                this.types = types;
                Decoder = decoder;
                Encoder = encoder;
            }

            private readonly Type[] types;
            private readonly Func<Match, object> Decoder;
            private readonly Func<object, string> Encoder;

            public string Tag { get; protected set; }
            public Regex Pattern { get; protected set; }
            public string PatternSource { get; protected set; }

            public object Decode(Match m)
            {
                return Decoder(m);
            }

            public string Encode(object obj)
            {
                return Encoder(obj);
            }

            public Type[] GetTypeOfValue()
            {
                return types;
            }

            public bool HasEncoder()
            {
                return Encoder != null;
            }

            public bool IsMatch(string value)
            {
                return Pattern.IsMatch(value);
            }
        }
    }
}