// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Rendering
{
    /// <summary>
    /// Extensions for <see cref="ParameterCollection"/>.
    /// </summary>
    public static class ParameterCollectionExtensions
    {
        public static string ToStringPermutationsDetailed(this ParameterCollection parameterCollection)
        {
            var builder = new StringBuilder();

            var first = true;
            foreach (var usedParam in parameterCollection.ParameterKeyInfos)
            {
                // Ignore any non-permutation key
                if (usedParam.Key.Type != ParameterKeyType.Permutation)
                    continue;

                var value = parameterCollection.ObjectValues[usedParam.BindingSlot];

                builder.Append("@P ");
                if (first)
                {
                    builder.Append("  - ");
                    first = false;
                }

                if (usedParam.Key == null)
                    builder.Append("null");
                else
                    builder.Append(usedParam.Key);
                builder.Append(": ");
                if (value == null)
                {
                    builder.AppendLine("null");
                }
                else
                {
                    if (value is Array || value is IList)
                    {
                        builder.AppendLine(string.Join(", ", (IEnumerable<object>)value));
                    }
                    else
                    {
                        builder.AppendLine(value.ToString());
                    }
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Loads the content that object parameters of this collection only hold references to.
        /// </summary>
        /// <param name="parameters">The parameter collection to resolve.</param>
        /// <param name="content">The content manager to load through, or <c>null</c> to only report.</param>
        /// <param name="log">The logger that reports references left unresolved, or <c>null</c> for the default one.</param>
        /// <remarks>
        /// Code that builds objects outside of a content load can attach a reference with
        /// <see cref="AttachedReferenceManager.CreateProxyObject{T}(AssetId, string)"/>, which makes an
        /// empty object and marks it as a proxy. A proxy becomes the real content in one place only,
        /// <c>ReferenceSerializer</c>, and only during a content load, so such collections keep empty
        /// objects until they are loaded here.
        /// </remarks>
        public static void ResolveAttachedReferences(this ParameterCollection parameters, ContentManager content, Logger log = null)
        {
            var objectValues = parameters.ObjectValues;

            if (objectValues is null)
                return;

            foreach (var keyInfo in parameters.ParameterKeyInfos)
            {
                // Permutation keys share ObjectValues with resource parameters; replacing one here
                // would not bump the permutation counter, so only object (resource) keys are handled.
                if (keyInfo.Key.Type != ParameterKeyType.Object || !keyInfo.IsResourceParameter || keyInfo.BindingSlot >= objectValues.Length)
                    continue;

                var reference = AttachedReferenceManager.GetAttachedReference(objectValues[keyInfo.BindingSlot]);
                if (reference is not { IsProxy: true })
                    continue;

                log ??= GlobalLogger.GetLogger(nameof(ParameterCollection));

                if (content is null)
                {
                    log.Warning($"Parameter '{keyInfo.Key}' keeps an empty object, because no content " +
                                $"manager was available to load '{reference.Url}'.");
                    continue;
                }

                try
                {
                    objectValues[keyInfo.BindingSlot] = content.Load(keyInfo.Key.PropertyType, reference.Url);
                }
                catch (ContentManagerException exception)
                {
                    // An asset reaches a build only when something in the content references it.
                    // Nothing references what gets attached here, because that happens at run time,
                    // so the referenced asset can be missing from the build. Keep the empty object
                    // rather than stop the game, and say what is wrong.
                    log.Warning($"Parameter '{keyInfo.Key}' keeps an empty object, because the asset " +
                                $"it references is not in the build: '{reference.Url}'. {exception.Message}");
                }
            }
        }
    }
}
