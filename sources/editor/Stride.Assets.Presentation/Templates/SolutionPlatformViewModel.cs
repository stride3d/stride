// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Assets.Presentation.Templates
{
    /// <summary>
    /// A view model class wrapping the <see cref="SolutionPlatform"/> class, used to create or modify target platforms of a package.
    /// </summary>
    public class SolutionPlatformViewModel : ViewModelBase
    {
        private bool isSelected;
        private SolutionPlatformTemplate selectedTemplate;

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionPlatformViewModel"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for this view model.</param>
        /// <param name="solutionPlatform">The solution platform represented by the view model.</param>
        /// <param name="isAlreadyInstalled">Indicates whether this plaform is already installed in the related package.</param>
        /// <param name="initiallySelected">Indicates whether this plaform should be initially selected.</param>
        public SolutionPlatformViewModel(IViewModelServiceProvider serviceProvider, SolutionPlatform solutionPlatform, bool isAlreadyInstalled, bool initiallySelected)
            : base(serviceProvider)
        {
            SolutionPlatform = solutionPlatform;
            IsAvailableOnMachine = solutionPlatform.IsAvailable;
            IsSelected = initiallySelected;
            IsAlreadyInstalled = isAlreadyInstalled;
            DependentProperties.Add(nameof(IsSelected), new[] { nameof(MarkedToRemove) });
            SelectedTemplate = AvailableTemplates.FirstOrDefault();
        }

        /// <summary>
        /// Gets the name of this platform.
        /// </summary>
        public string Name => SolutionPlatform.DisplayName ?? SolutionPlatform.Name;

        /// <summary>
        /// Gets the type of this platform
        /// </summary>
        public PlatformType Platform => SolutionPlatform.Type;

        /// <summary>
        /// Gets whether this platform can be unselected.
        /// </summary>
        public bool CanBeUnselected => Platform != PlatformType.Windows;

        /// <summary>
        /// Gets whether this platform is currently selected.
        /// </summary>
        public bool IsSelected { get => isSelected; set => SetValue(ref isSelected, value); }

        /// <summary>
        /// The list of available templates (if empty, fallback to default).
        /// </summary>
        public IEnumerable<SolutionPlatformTemplate> AvailableTemplates => SolutionPlatform.Templates;

        /// <summary>
        /// Determines whether if there are any platform templates.
        /// </summary>
        public bool HasTemplates => SolutionPlatform.Templates.Count > 0;

        /// <summary>
        /// Gets whether this platform is currently selected.
        /// </summary>
        public SolutionPlatformTemplate SelectedTemplate { get => selectedTemplate; set => SetValue(ref selectedTemplate, value); }

        /// <summary>
        /// Gets whether this platform has already been installed for the related package.
        /// </summary>
        public bool IsAlreadyInstalled { get; }

        /// <summary>
        /// Gets whether the prerequisites to use this platform are met on the current machine.
        /// </summary>
        public bool IsAvailableOnMachine { get; }

        /// <summary>
        /// Gets whether this platform has been marked to be removed.
        /// </summary>
        public bool MarkedToRemove => !IsSelected && IsAlreadyInstalled;

        /// <summary>
        /// Gets the solution platform 
        /// </summary>
        public SolutionPlatform SolutionPlatform { get; }
    }
}
