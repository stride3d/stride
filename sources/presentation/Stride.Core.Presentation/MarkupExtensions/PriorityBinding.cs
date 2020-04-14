// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows.Data;
using System.Windows.Markup;

namespace Stride.Core.Presentation.MarkupExtensions
{
    /// <summary>
    /// This class extends the <see cref="System.Windows.Data.PriorityBinding"/> by providing constructors that allows construction using markup extension.
    /// </summary>
    public class PriorityBinding : System.Windows.Data.PriorityBinding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityBinding"/> class.
        /// </summary>
        /// <param name="binding1">The first binding.</param>
        /// <param name="binding2">The second binding.</param>
        public PriorityBinding(Binding binding1, Binding binding2)
            : this(binding1, binding2, null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityBinding"/> class.
        /// </summary>
        /// <param name="binding1">The first binding.</param>
        /// <param name="binding2">The second binding.</param>
        /// <param name="binding3">The third binding.</param>
        public PriorityBinding(Binding binding1, Binding binding2, Binding binding3)
            : this(binding1, binding2, binding3, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityBinding"/> class.
        /// </summary>
        /// <param name="binding1">The first binding.</param>
        /// <param name="binding2">The second binding.</param>
        /// <param name="binding3">The third binding.</param>
        /// <param name="binding4">The fourth binding.</param>
        public PriorityBinding(Binding binding1, Binding binding2, Binding binding3, Binding binding4)
            : this(binding1, binding2, binding3, binding4, null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityBinding"/> class.
        /// </summary>
        /// <param name="binding1">The first binding.</param>
        /// <param name="binding2">The second binding.</param>
        /// <param name="binding3">The third binding.</param>
        /// <param name="binding4">The fourth binding.</param>
        /// <param name="binding5">The fifth binding.</param>
        public PriorityBinding(Binding binding1, Binding binding2, Binding binding3, Binding binding4, Binding binding5)
            : this(binding1, binding2, binding3, binding4, binding5, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityBinding"/> class.
        /// </summary>
        /// <param name="binding1">The first binding.</param>
        /// <param name="binding2">The second binding.</param>
        /// <param name="binding3">The third binding.</param>
        /// <param name="binding4">The fourth binding.</param>
        /// <param name="binding5">The fifth binding.</param>
        /// <param name="binding6">The sixth binding.</param>
        public PriorityBinding(Binding binding1, Binding binding2, Binding binding3, Binding binding4, Binding binding5, Binding binding6)
            : this(binding1, binding2, binding3, binding4, binding5, binding6, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityBinding"/> class.
        /// </summary>
        /// <param name="binding1">The first binding.</param>
        /// <param name="binding2">The second binding.</param>
        /// <param name="binding3">The third binding.</param>
        /// <param name="binding4">The fourth binding.</param>
        /// <param name="binding5">The fifth binding.</param>
        /// <param name="binding6">The sixth binding.</param>
        /// <param name="binding7">The seventh binding.</param>
        public PriorityBinding(Binding binding1, Binding binding2, Binding binding3, Binding binding4, Binding binding5, Binding binding6, Binding binding7)
            : this(binding1, binding2, binding3, binding4, binding5, binding6, binding7, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityBinding"/> class.
        /// </summary>
        /// <param name="binding1">The first binding.</param>
        /// <param name="binding2">The second binding.</param>
        /// <param name="binding3">The third binding.</param>
        /// <param name="binding4">The fourth binding.</param>
        /// <param name="binding5">The fifth binding.</param>
        /// <param name="binding6">The sixth binding.</param>
        /// <param name="binding7">The seventh binding.</param>
        /// <param name="binding8">The eighth binding.</param>
        public PriorityBinding(Binding binding1, Binding binding2, Binding binding3, Binding binding4, Binding binding5, Binding binding6, Binding binding7, Binding binding8)
        {
            var addChild = (IAddChild)this;
            if (binding1 != null) addChild.AddChild(binding1);
            if (binding2 != null) addChild.AddChild(binding2);
            if (binding3 != null) addChild.AddChild(binding3);
            if (binding4 != null) addChild.AddChild(binding4);
            if (binding5 != null) addChild.AddChild(binding5);
            if (binding6 != null) addChild.AddChild(binding6);
            if (binding7 != null) addChild.AddChild(binding7);
            if (binding8 != null) addChild.AddChild(binding8);
        }
    }
}
