// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Stride.Core.Assets.Templates;

namespace Stride.Core.Assets.Tests
{
    /// <summary>
    /// Tests for the <see cref="TemplateManager"/> class.
    /// </summary>
    public class TestTemplateManager: TemplateGeneratorBase<SessionTemplateGeneratorParameters>
    {
        [Fact(Skip = "Need check")]
        public void TestTemplateDescriptions()
        {
            // Preload templates defined in Stride.sdpkg
            var descriptions = TemplateManager.FindTemplates().ToList();

            // Expect currently 4 templates
            Assert.Equal(23, descriptions.Count);
        }

        [Fact]
        public void TestTemplateGenerator()
        {
            TemplateManager.Register(this);

            var templateGenerator = TemplateManager.FindTemplateGenerator(new SessionTemplateGeneratorParameters());

            Assert.Equal(this, templateGenerator);

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
    }
}
