using Stride.Core.Assets;

namespace Stride.Assets.Templates
{
    /// <summary>
    /// Represents a <see cref="SolutionPlatform"/> with some additional parameters (such as <see cref="SolutionPlatformTemplate"/>), as selected in <see cref="NewGameTemplateGenerator"/> or <see cref="UpdatePlatformsTemplateGenerator"/>.
    /// </summary>
    public struct SelectedSolutionPlatform
    {
        public SelectedSolutionPlatform(SolutionPlatform platform, SolutionPlatformTemplate template)
        {
            Platform = platform;
            Template = template;
        }

        public SolutionPlatform Platform { get; }

        public SolutionPlatformTemplate Template { get; }
    }
}
