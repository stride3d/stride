// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Reflection;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters
{
    internal sealed class MathematicsNodeUpdater : AssetNodePresenterUpdaterBase
    {
        private readonly Type[] mathematicsTypes = new[]
        {
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Int2),
            typeof(Int3),
            typeof(Int4),
            typeof(Matrix),
            typeof(Rectangle),
            typeof(RectangleF),
        };

        protected override void UpdateNode(IAssetNodePresenter node)
        {
            if (mathematicsTypes.Contains(node.Type))
            {
                foreach (var field in node.Type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    var dataMemberAttribute = field.GetCustomAttribute<DataMemberAttribute>();
                    var component = node.Factory.CreateVirtualNodePresenter(node, field.Name, field.FieldType, dataMemberAttribute?.Order, () => field.GetValue(node.Value), x => SetComponent(node, field, x), () => node.HasBase, () => node.IsInherited, () => node.IsOverridden);
                    component.IsVisible = false;
                }
            }
        }

        private static void SetComponent(IAssetNodePresenter node, FieldInfo field, object component)
        {
            var value = node.Value;
            field.SetValue(value, component);
            node.UpdateValue(value);
        }
    }
}
