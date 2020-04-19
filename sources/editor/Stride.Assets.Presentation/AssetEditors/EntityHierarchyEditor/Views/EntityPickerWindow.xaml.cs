// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Collections;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.SceneEditor.Services;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Views
{
    /// <summary>
    /// Interaction logic for EntityPickerWindow.xaml
    /// </summary>
    public partial class EntityPickerWindow : INotifyPropertyChanged, IEntityPickerDialog
    {
        private EntityHierarchyItemViewModelWrapper selectedEntity;

        public class EntityHierarchyItemViewModelWrapper : IPickedEntity
        {
            private readonly bool matchesCriteria;

            public EntityHierarchyItemViewModelWrapper(EntityHierarchyItemViewModel entityHierarchyItem, Func<EntityHierarchyItemViewModel, bool> filter, Type componentType)
            {
                EntityHierarchyItem = entityHierarchyItem;
                Entity = entityHierarchyItem as EntityViewModel;
                if (Entity != null)
                {
                    if (componentType != null)
                    {
                        Components = new List<Tuple<int, EntityComponent>>();
                        for (var i = 0; i < Entity.AssetSideEntity.Components.Count; i++)
                        {
                            var component = Entity.AssetSideEntity.Components[i];
                            if (componentType.IsInstanceOfType(component))
                            {
                                Components.Add(Tuple.Create(i, component));
                            }
                        }
                        SelectedComponent = Components.Count > 0 ? Components.First() : null;
                    }
                    matchesCriteria = componentType == null || Components.Count > 0;
                }
                foreach (var subEntity in entityHierarchyItem.Children.Where(filter).Select(x => new EntityHierarchyItemViewModelWrapper(x, filter, componentType)).Where(x => x.MatchesCriteria))
                {
                    Content.Add(subEntity);
                }
            }

            public EntityHierarchyItemViewModel EntityHierarchyItem { get; }

            public string Name => EntityHierarchyItem.Name;

            public EntityViewModel Entity { get; }

            public List<EntityHierarchyItemViewModelWrapper> Content { get; } = new List<EntityHierarchyItemViewModelWrapper>();

            public List<Tuple<int, EntityComponent>> Components { get; }

            public Tuple<int, EntityComponent> SelectedComponent { get; set; }

            public bool MatchesCriteria => matchesCriteria || Content.Any(x => x.MatchesCriteria);

            int IPickedEntity.ComponentIndex => SelectedComponent?.Item1 ?? -1;
        }

        public EntityPickerWindow([NotNull] EntityHierarchyEditorViewModel editor, Type targetType)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));

            InitializeComponent();
            if (targetType != null && typeof(EntityComponent).IsAssignableFrom(targetType))
            {
                ComponentType = targetType;
                Width *= 2;
            }
            DataContext = this;
            Loaded += (s, e) => OnLoaded(editor);
        }

        public ObservableList<EntityHierarchyItemViewModelWrapper> SceneContent { get; } = new ObservableList<EntityHierarchyItemViewModelWrapper>();

        public Func<EntityHierarchyItemViewModel, bool> Filter { get; set; } = x => true;

        public IPickedEntity SelectedEntity { get { return selectedEntity; } set { selectedEntity = (EntityHierarchyItemViewModelWrapper)value; OnPropertyChanged("SelectedEntity", "SelectionValid"); } }

        public bool SelectionValid => IsSelectionValid();

        public Type ComponentType { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(params string[] propertyNames)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                foreach (var name in propertyNames)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (Result != Stride.Core.Presentation.Services.DialogResult.Ok)
                SelectedEntity = null;

            base.OnClosed(e);
        }

        private bool IsSelectionValid()
        {
            if (SelectedEntity == null)
                return false;

            if (ComponentType != null)
            {
                return ComponentType.IsInstanceOfType(((EntityHierarchyItemViewModelWrapper)SelectedEntity).SelectedComponent?.Item2);
            }

            return true;
        }

        private void OnLoaded([NotNull] EntityHierarchyEditorViewModel editor)
        {
            var items = editor.HierarchyRoot.Children.Where(Filter)
                .Select(x => new EntityHierarchyItemViewModelWrapper(x, Filter, ComponentType)).Where(x => x.MatchesCriteria);
            SceneContent.Clear();
            SceneContent.AddRange(items);
        }
    }
}
