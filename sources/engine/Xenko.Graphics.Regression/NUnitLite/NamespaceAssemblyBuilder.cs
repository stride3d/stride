// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_IOS || XENKO_PLATFORM_ANDROID
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework.Api;
using NUnit.Framework.Internal;

namespace Xenko.Graphics.Regression
{
    /// <summary>
    /// Reprocess result of another <see cref="ITestAssemblyBuilder"/> by regrouping test of a given namespace into a TestSuite containing that name.
    /// It matches Resharper behavior for grouping test by namespace.
    /// </summary>
    public class NamespaceAssemblyBuilder : ITestAssemblyBuilder
    {
        private ITestAssemblyBuilder innerAssemblyBuilder;

        public NamespaceAssemblyBuilder(ITestAssemblyBuilder innerAssemblyBuilder)
        {
            this.innerAssemblyBuilder = innerAssemblyBuilder;
        }

        public TestSuite Build(Assembly assembly, IDictionary options)
        {
            return GroupByNamespace(innerAssemblyBuilder.Build(assembly, options));
        }

        public TestSuite Build(string assemblyName, IDictionary options)
        {
            return GroupByNamespace(innerAssemblyBuilder.Build(assemblyName, options));
        }

        public static TestSuite GroupByNamespace(TestSuite testSuite)
        {
            if (testSuite == null)
                return null;

            var result = new TestSuite(testSuite.Name + ".Application");

            foreach (var testGroup in testSuite.Tests.GroupBy(x => GetNamespace(x)))
            {
                var namespaceSuite = new TestNamespace(testGroup.Key);
                foreach (var test in testGroup)
                {
                    namespaceSuite.Tests.Add(test);
                }

                result.Tests.Add(namespaceSuite);
            }

            return result;
        }

        private static string GetNamespace(ITest test)
        {
            var name = test.FullName;

            var lastDot = name.LastIndexOf('.');
            if (lastDot != -1)
                name = name.Substring(0, lastDot);

            return name;
        }
    }
}
#endif
