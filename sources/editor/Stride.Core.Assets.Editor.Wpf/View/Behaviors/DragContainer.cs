// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    [Serializable]
    public sealed class DragContainer : ViewModelBase, ISerializable
    {
        private string message;
        private DropAcceptance acceptance;

        public const string Format = "DraggedData";
        public const int PreviewItemCount = 10;

        public DragContainer([NotNull] IEnumerable<object> items)
            : this()
        {
            Items = items.ToArray();
        }

        private DragContainer()
        {
            DependentProperties.Add(nameof(Acceptance), new[] { nameof(IsAccepted), nameof(IsRejected) });
        }

        [NotNull]
        public object[] Items { get; }

        [NotNull]
        public IEnumerable<object> PreviewItems => Items.Length <= PreviewItemCount ? Items : Items.Take(PreviewItemCount).Concat(PreviewEllipsis);

        [ItemNotNull, NotNull]
        public IEnumerable<object> PreviewEllipsis { get { yield return "..."; } }

        public string Message { get { return message; } set { SetValue(ref message, value); } }

        /// <summary>
        /// Gets or sets how the item currently under the pointer accepts the dragged items.
        /// </summary>
        public DropAcceptance Acceptance { get { return acceptance; } set { SetValue(ref acceptance, value); } }

        /// <summary>
        /// Gets whether the drop is accepted. A no-op is not accepted, because it changes nothing.
        /// </summary>
        public bool IsAccepted => acceptance == DropAcceptance.Accepted;

        /// <summary>
        /// Gets whether the drop is refused. A no-op is not refused, because the user made no error.
        /// </summary>
        public bool IsRejected => acceptance == DropAcceptance.Rejected;

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
