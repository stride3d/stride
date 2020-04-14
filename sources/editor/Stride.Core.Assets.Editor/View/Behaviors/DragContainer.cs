// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Stride.Core.Annotations;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    [Serializable]
    public sealed class DragContainer : ViewModelBase, ISerializable
    {
        private string message;
        private bool isAccepted;

        public const string Format = "DraggedData";
        public const int PreviewItemCount = 10;

        public DragContainer([NotNull] IEnumerable<object> items)
            : this()
        {
            Items = items.ToArray();
        }

        private DragContainer()
        {
            DependentProperties.Add(nameof(IsAccepted), new[] { nameof(IsRejected) });
        }

        [NotNull]
        public object[] Items { get; }

        [NotNull]
        public IEnumerable<object> PreviewItems => Items.Length <= PreviewItemCount ? Items : Items.Take(PreviewItemCount).Concat(PreviewEllipsis);

        [ItemNotNull, NotNull]
        public IEnumerable<object> PreviewEllipsis { get { yield return "..."; } }

        public string Message { get { return message; } set { SetValue(ref message, value); } }

        public bool IsAccepted { get { return isAccepted; } set { SetValue(ref isAccepted, value); } }

        public bool IsRejected => !isAccepted;

        /// <summary>
        /// The special constructor is used to deserialize values.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        private DragContainer(SerializationInfo info, StreamingContext context)
            : this()
        {
            // Reset the property value using the GetValue method.
            Items = (object[])info.GetValue(nameof(Items), typeof(object[]));
        }

        /// <inheritdoc/>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Items), Items, typeof(object[]));
        }
    }
}
