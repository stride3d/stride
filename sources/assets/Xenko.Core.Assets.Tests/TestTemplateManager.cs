// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Xenko.Core.Assets.Templates;

namespace Xenko.Core.Assets.Tests
{
    /// <summary>
    /// Tests for the <see cref="TemplateManager"/> class.
    /// </summary>
    [TestFixture]
    public class TestTemplateManager: TemplateGeneratorBase<SessionTemplateGeneratorParameters>
    {
        [Test, Ignore("Need check")]
        public void TestTemplateDescriptions()
        {
            // Preload templates defined in Xenko.xkpkg
            var descriptions = TemplateManager.FindTemplates().ToList();

            // Expect currently 4 templates
            Assert.AreEqual(23, descriptions.Count);
        }

        [Test]
        public void TestTemplateGenerator()
        {
            TemplateManager.Register(this);

            // Preload templates defined in Xenko.xkpkg
            var descriptions = TemplateManager.FindTemplates().ToList();

            Assert.Greater(descriptions.Count, 0);

            var templateGenerator = TemplateManager.FindTemplateGenerator(new SessionTemplateGeneratorParameters());

            Assert.AreEqual(this, templateGenerator);

            TemplateManager.Unregister(this);
        }

        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            return true;
        }

        public override Task<bool> PrepareForRun(SessionTemplateGeneratorParameters parameters)
        {
            // Nothing to do in the tests
            return Task.FromResult(true);
        }

        public override bool Run(SessionTemplateGeneratorParameters parameters)
        {
            return true;
        }

        public static void Main()
        {
            var test = new TestTemplateManager();
            test.TestTemplateDescriptions();
        }
    }
}
