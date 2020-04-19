// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories
{
    public class EntityFactoryCategory : IComparable<EntityFactoryCategory>
    {
        private static readonly Dictionary<string, int> CategoryOrders = new Dictionary<string, int>();

        public class EntityFactoryViewModel
        {
            internal EntityFactoryViewModel(IEntityFactory factory, string name)
            {
                Name = name;
                Factory = factory;
            }

            public string Name { get; private set; }

            public IEntityFactory Factory { get; private set; }
        }
   
        internal EntityFactoryCategory(string name)
        {
            Name = name;
            Factories = new SortedList<int, EntityFactoryViewModel>();
        }

        public string Name { get; }

        public SortedList<int, EntityFactoryViewModel> Factories { get; }

        public void AddFactory(IEntityFactory factory, string name, int order)
        {
            while (Factories.ContainsKey(order))
                ++order;

            var viewModel = new EntityFactoryViewModel(factory, name);
            Factories.Add(order, viewModel);
        }

        public static void RegisterCategory(int order, string name)
        {
            CategoryOrders.Add(name, order);
        }

        int IComparable<EntityFactoryCategory>.CompareTo(EntityFactoryCategory other)
        {
            int orderX, orderY;
            if (!CategoryOrders.TryGetValue(Name, out orderX))
                orderX = int.MaxValue;
            if (!CategoryOrders.TryGetValue(other.Name, out orderY))
                orderY = int.MaxValue;

            if (orderX != orderY)
                return -orderX.CompareTo(orderY);
            return string.Compare(Name, other.Name, CultureInfo.CurrentCulture, CompareOptions.None);
        }
    }
}
