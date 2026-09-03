// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Xunit;

using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization;
using Stride.Rendering;

namespace Stride.Engine.Tests
{
    /// <summary>
    /// Tests how a <see cref="ParameterCollection"/> handles object values that are unresolved
    /// attached references (proxies made by <see cref="AttachedReferenceManager.CreateProxyObject{T}(AssetId, string)"/>).
    /// </summary>
    public class ParameterCollectionAttachedReferenceTest
    {
        public static readonly ObjectParameterKey<object> ObjectKey = ParameterKeys.NewObject<object>();

        [Fact]
        public void TestResolveWithoutContentManagerWarnsAndKeepsProxy()
        {
            var parameters = new ParameterCollection();
            var url = $"test/unresolved/{Guid.NewGuid()}";
            var proxy = AttachedReferenceManager.CreateProxyObject<TestReferencedContent>(AssetId.New(), url);
            parameters.Set(ObjectKey, proxy);

            var log = new LoggerResult();
            parameters.ResolveAttachedReferences(null, log);

            Assert.Contains(log.Messages, message => message.Type == LogMessageType.Warning && message.Text.Contains(url));
            Assert.Same(proxy, parameters.Get(ObjectKey));
        }

        [Fact]
        public void TestUpdateLayoutReportsUnresolvedReference()
        {
            var parameters = new ParameterCollection();
            // The report deduplicates per URL for the process lifetime, so make the URL unique
            var url = $"test/unresolved/{Guid.NewGuid()}";
            parameters.Set(ObjectKey, AttachedReferenceManager.CreateProxyObject<TestReferencedContent>(AssetId.New(), url));

            var warnings = CollectGlobalWarningsDuring(() => parameters.UpdateLayout(LayoutWith(ObjectKey)));

            Assert.Contains(warnings, text => text.Contains(url));
        }

        [Fact]
        public void TestUpdateLayoutIgnoresResolvedObjects()
        {
            var parameters = new ParameterCollection();
            parameters.Set(ObjectKey, new TestReferencedContent());

            var warnings = CollectGlobalWarningsDuring(() => parameters.UpdateLayout(LayoutWith(ObjectKey)));

            Assert.DoesNotContain(warnings, text => text.Contains("unloaded content"));
        }

        private static ParameterCollectionLayout LayoutWith(ParameterKey key)
        {
            var layout = new ParameterCollectionLayout { ResourceCount = 1 };
            layout.LayoutParameterKeyInfos.Add(new ParameterKeyInfo(key, 0));
            return layout;
        }

        private static List<string> CollectGlobalWarningsDuring(Action action)
        {
            var warnings = new List<string>();

            void Collect(ILogMessage message)
            {
                if (message.Type >= LogMessageType.Warning)
                {
                    lock (warnings) warnings.Add(message.Text);
                }
            }

            GlobalLogger.GlobalMessageLogged += Collect;
            try
            {
                action();
            }
            finally
            {
                GlobalLogger.GlobalMessageLogged -= Collect;
            }

            return warnings;
        }

        private class TestReferencedContent
        {
        }
    }
}
