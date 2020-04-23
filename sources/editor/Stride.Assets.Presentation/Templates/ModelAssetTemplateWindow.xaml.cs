// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.CompilerServices;
using System.Windows;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Quantum;
using Stride.Rendering;

namespace Stride.Assets.Presentation.Templates
{
    public class ImportModelFromFileViewModel : DispatcherViewModel
    {
        [DataContract]
        public class DummyReferenceContainer : IPropertyProviderViewModel
        {
            private readonly IObjectNode rootNode;

            public DummyReferenceContainer()
            {
                rootNode = SessionViewModel.Instance.AssetNodeContainer.GetOrCreateNode(this);
            }

            public Skeleton Skeleton { get; set; }

            bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

            IObjectNode IPropertyProviderViewModel.GetRootNode() => rootNode;

            bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => true;

            bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => true;
        }

        private readonly DummyReferenceContainer referenceContainer = new DummyReferenceContainer();

        private bool importMaterials = true;
        private bool showDeduplicateMaterialsCheckBox = true;
        private bool showFbxDedupeNotSupportedWarning = false;
        private bool deduplicateMaterials = true;
        private bool importTextures = true;
        private bool importSkeleton = true;
        private bool dontImportSkeleton;
        private bool reuseSkeleton;

        public ImportModelFromFileViewModel(IViewModelServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            ReferenceViewModel = GraphViewModel.Create(serviceProvider, new[] { referenceContainer });
        }

        public bool ImportMaterials { get { return importMaterials; } set { SetValue(ref importMaterials, value); } }

        public bool ShowDeduplicateMaterialsCheckBox { get { return showDeduplicateMaterialsCheckBox; } set { SetValue(ref showDeduplicateMaterialsCheckBox, value); } }
        public bool ShowFbxDedupeNotSupportedWarning { get { return showFbxDedupeNotSupportedWarning; } set { SetValue(ref showFbxDedupeNotSupportedWarning, value); } }
        public bool DeduplicateMaterials { get { return deduplicateMaterials; } set { SetValue(ref deduplicateMaterials, value); } }

        public bool ImportTextures { get { return importTextures; } set { SetValue(ref importTextures, value); } }

        public bool ImportSkeleton { get { return importSkeleton; } set { SetValue(ref importSkeleton, value); } }

        public bool DontImportSkeleton { get { return dontImportSkeleton; } set { SetValue(ref dontImportSkeleton, value); } }

        public bool ReuseSkeleton { get { return reuseSkeleton; } set { SetValue(ref reuseSkeleton, value); } }

        public Skeleton SkeletonToReuse { get { return referenceContainer.Skeleton; } set { referenceContainer.Skeleton = value; } }

        public GraphViewModel ReferenceViewModel { get; }
    }
    /// <summary>
    /// Interaction logic for ModelAssetTemplateWindow.xaml
    /// </summary>
    public partial class ModelAssetTemplateWindow
    {
        public ModelAssetTemplateWindow()
        {
            var viewModelService = new GraphViewModelService(SessionViewModel.Instance.AssetNodeContainer);
            var services = new ViewModelServiceProvider(SessionViewModel.Instance.ServiceProvider, viewModelService.Yield());
            Parameters = new ImportModelFromFileViewModel(services);
            InitializeComponent();
        }

        public ImportModelFromFileViewModel Parameters { get { return (ImportModelFromFileViewModel)DataContext; } set { DataContext = value; } }

        private void ButtonOk(object sender, RoutedEventArgs e)
        {
            Result = Stride.Core.Presentation.Services.DialogResult.Ok;
            Close();
        }

        private void ButtonCancel(object sender, RoutedEventArgs e)
        {
            Result = Stride.Core.Presentation.Services.DialogResult.Cancel;
            Close();
        }
    }
}
