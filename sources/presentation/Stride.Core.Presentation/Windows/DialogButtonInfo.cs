// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Presentation.Windows
{
    /// <summary>
    /// Represents a button in a <see cref="MessageDialogBase"/>.
    /// </summary>
    public sealed class DialogButtonInfo : ViewModelBase
    {
        private object content;
        private bool isCancel;
        private bool isDefault;
        private int result;
        private string key = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogButtonInfo"/> class.
        /// </summary>
        /// <param name="content">The content of the button.</param>
        /// <param name="result">The result value associated to this button.</param>
        /// <param name="isDefault">Indicates if this button is the default button.</param>
        /// <param name="isCancel">Indicates if this button is the cancel button.</param>
        public DialogButtonInfo(object content, int result, bool isDefault = false, bool isCancel = false)
        {
            this.content = content;
            this.result = result;
            this.isDefault = isDefault;
            this.isCancel = isCancel;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogButtonInfo"/> class.
        /// </summary>
        public DialogButtonInfo()
        {
        }

        /// <summary>
        /// The content of this button.
        /// </summary>
        public object Content
        {
            get { return content; }
            set { SetValue(ref content, value); }
        }

        /// <summary>
        /// Specifies whether or not this button is the cancel button.
        /// </summary>
        public bool IsCancel
        {
            get { return isCancel; }
            set { SetValue(ref isCancel, value); }
        }

        /// <summary>
        /// Specifies whether or not this button is the default button.
        /// </summary>
        public bool IsDefault
        {
            get { return isDefault; }
            set { SetValue(ref isDefault, value); }
        }

        /// <summary>
        /// The gesture associated with this button.
        /// </summary>
        public string Key
        {
            get { return key; }
            set { SetValue(ref key, value); }
        }

        /// <summary>
        /// The result associated with this button.
        /// </summary>
        /// <seealso cref="MessageDialogBase.Result"/>
        public int Result
        {
            get { return result; }
            set { SetValue(ref result, value); }
        }
    }
}
