// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Android.Content;
using Android.Views;

namespace MonoDroid.Dialog
{
    public abstract class Element : Java.Lang.Object, IDisposable
    {
        /// <summary>
        ///  Initializes the element with the given caption.
        /// </summary>
        /// <param name="caption">
        /// The caption.
        /// </param>
        public Element(string caption)
        {
            Caption = caption;
        }

        public Element(string caption, int layoutId)
        {
            Caption = caption;
            LayoutId = layoutId;
        }

        /// <summary>
        ///  The caption to display for this given element
        /// </summary>
        public string Caption { get; set; }

        public int LayoutId { get; private set; }

        /// For sections this points to a RootElement, for every other object this points to a Section and it is null
        /// for the root RootElement.
        /// </remarks>
        public Element Parent { get; set; }

        /// <summary>
        /// Override for click the click event
        /// </summary>
        public Action Click { get; set; }

        /// <summary>
        /// Override for long click events, some elements use this for action
        /// </summary>
        public Action LongClick { get; set; }

        /// <summary>
        /// An Object that contains data about the element. The default is null.
        /// </summary>
        public Object Tag { get; set; }
	
        public void Dispose()
        {
            Dispose(true);
        }

    	protected virtual void Dispose(bool disposing) { }

        /// <summary>
        /// Returns a summary of the value represented by this object, suitable 
        /// for rendering as the result of a RootElement with child objects.
        /// </summary>
        /// <returns>
        /// The return value must be a short description of the value.
        /// </returns>
        public virtual string Summary()
        {
            return string.Empty;
        }

        /// <summary>
        /// Overriden my most derived classes, creates a view that creates a View with the contents for display
        /// </summary>
        /// <param name="context"></param>
        /// <param name="convertView"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public virtual View GetView(Context context, View convertView, ViewGroup parent)
        {
			var view = LayoutId == 0 ? new View(context) : null;
            return view;
        }

        public virtual void Selected() {}
				
        public virtual bool Matches(string text)
        {
            return Caption != null && Caption.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) != -1;
        }

        public Context GetContext()
        {
            Element element = this;
            while (element.Parent != null)
                element = element.Parent;

            RootElement rootElement = element as RootElement;
            return rootElement == null ? null : rootElement.Context;
        }
    }
}
