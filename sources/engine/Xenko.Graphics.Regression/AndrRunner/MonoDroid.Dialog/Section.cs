// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using Android.Content;
using Android.Views;
using Android.Widget;
using Orientation = Android.Widget.Orientation;

namespace MonoDroid.Dialog
{
	/// <summary>
	/// Sections contain individual Element instances that are rendered by MonoDroid.Dialog
	/// </summary>
	/// <remarks>
	/// Sections are used to group elements in the screen and they are the
	/// only valid direct child of the RootElement.    Sections can contain
	/// any of the standard elements, including new RootElements.
	/// 
	/// RootElements embedded in a section are used to navigate to a new
	/// deeper level.
	/// 
	/// You can assign a header and a footer either as strings (Header and Footer)
	/// properties, or as Views to be shown (HeaderView and FooterView).   Internally
	/// this uses the same storage, so you can only show one or the other.
	/// </remarks>
    public class Section : Element, IEnumerable<Element>
	{
		public List<Element> Elements = new List<Element>();

		List<string> ElementTypes = new List<string>();

		public SectionAdapter Adapter;
		public int StartIndex {get;set;}
		// X corresponds to the alignment, Y to the height of the password
		private object footer;
		private object header;

		/// <summary>
		///  Constructs a Section without header or footers.
		/// </summary>
        public Section()
            : this("")
		{
			Adapter = new SectionAdapter(this);
		}

		/// <summary>
		///  Constructs a Section with the specified header
		/// </summary>
		/// <param name="caption">
		/// The header to display
		/// </param>
		public Section(string caption)
			: base(caption)
		{
			Adapter = new SectionAdapter(this);
		}

		/// <summary>
		/// Constructs a Section with a header and a footer
		/// </summary>
		/// <param name="caption">
		/// The caption to display (or null to not display a caption)
		/// </param>
		/// <param name="footer">
		/// The footer to display.
		/// </param>
		public Section(string caption, string footer)
			: this(caption)
		{
			Footer = footer;
		}

		public Section(View header)
			: this()
		{
			HeaderView = header;
		}

		public Section(View header, View footer)
			: this()
		{
			HeaderView = header;
			FooterView = footer;
		}

		/// <summary>
		///    The section header, as a string
		/// </summary>
		public string Header
		{
			get { return Caption; }
			set { Caption = value; }
		}

		/// <summary>
		/// The section footer, as a string.
		/// </summary>
		public string Footer
		{
			get { return footer as string; }
			set { footer = value; }
		}

		/// <summary>
		/// The section's header view.  
		/// </summary>
		public View HeaderView
		{
			get { return header as View; }
			set { header = value; }
		}

		/// <summary>
		/// The section's footer view.
		/// </summary>
		public View FooterView
		{
			get { return footer as View; }
			set { footer = value; }
		}

		public int Count
		{
			get { return Elements.Count; }
		}

		public Element this[int idx]
		{
			get { return Elements[idx]; }
		}

		/// <summary>
		/// Adds a new child Element to the Section
		/// </summary>
		/// <param name="element">
		/// An element to add to the section.
		/// </param>
		public void Add(Element element)
		{
			if (element == null)
				return;

			var elementType = element.GetType().FullName;

			if (!ElementTypes.Contains(elementType))
				ElementTypes.Add(elementType);

			Elements.Add(element);
			element.Parent = this;

			if (Parent != null)
				InsertVisual(Elements.Count - 1, 1);
		}

		/// <summary>
		///    Add version that can be used with LINQ
		/// </summary>
		/// <param name="elements">
		/// An enumerable list that can be produced by something like:
		///    from x in ... select (Element) new MyElement (...)
		/// </param>
		public int Add(IEnumerable<Element> elements)
		{
			int count = 0;
			foreach (Element e in elements)
			{
				Add(e);
				count++;
			}
			return count;
		}

		/// <summary>
		/// Use to add a View to a section, it makes the section opaque, to
		/// get a transparent one, you must manually call ViewElement
		public void Add(View view)
		{
			if (view == null)
				return;
			Add(new ViewElement(null, view, false));
		}

		/// <summary>
		///   Adds the Views to the section.
		/// </summary>
		/// <param name="views">
		/// An enumarable list that can be produced by something like:
		///    from x in ... select (View) new UIFoo ();
		/// </param>
		public void Add(IEnumerable<View> views)
		{
			foreach (View v in views)
				Add(v);
		}

		/// <summary>
		/// Inserts a series of elements into the Section using the specified animation
		/// </summary>
		/// <param name="idx">
		/// The index where the elements are inserted
		/// </param>
		/// <param name="anim">
		/// The animation to use
		/// </param>
		/// <param name="newElements">
		/// A series of elements.
		/// </param>
		public void Insert(int idx, params Element[] newElements)
		{
			if (newElements == null)
				return;

			int pos = idx;
			foreach (Element e in newElements)
			{
				Elements.Insert(pos++, e);
				e.Parent = this;
			}
			var root = Parent as RootElement;
			if (Parent != null)
			{
				InsertVisual(idx, newElements.Length);
			}
		}

		public int Insert(int idx, IEnumerable<Element> newElements)
		{
			if (newElements == null)
				return 0;

			int pos = idx;
			int count = 0;
			foreach (Element e in newElements)
			{
				Elements.Insert(pos++, e);
				e.Parent = this;
				count++;
			}
			var root = Parent as RootElement;
			if (root != null )
			{
				InsertVisual(idx, pos - idx);
			}
			return count;
		}

		private void InsertVisual(int idx, int count)
		{
			//var root = Parent as RootElement;

			//if (root == null || root.TableView == null)
			//    return;

			//int sidx = root.IndexOf(this);
			//var paths = new NSIndexPath[count];
			//for (int i = 0; i < count; i++)
			//    paths[i] = NSIndexPath.FromRowSection(idx + i, sidx);

			//root.TableView.InsertRows(paths);
		}

		public void Remove(Element e)
		{
			if (e == null)
				return;
			for (int i = Elements.Count; i > 0;)
			{
				i--;
				if (Elements[i] == e)
				{
					RemoveRange(i, 1);
					return;
				}
			}
		}

		public void Remove(int idx)
		{
			RemoveRange(idx, 1);
		}

		/// <summary>
		/// Removes a range of elements from the Section
		/// </summary>
		/// <param name="start">
		/// Starting position
		/// </param>
		/// <param name="count">
		/// Number of elements to remove from the section
		/// </param>
		public void RemoveRange(int start, int count)
		{
			if (start < 0 || start >= Elements.Count)
				return;
			if (count == 0)
				return;

			var root = Parent as RootElement;

			if (start + count > Elements.Count)
				count = Elements.Count - start;

			Elements.RemoveRange(start, count);

			if (root == null)
				return;

			int sidx = root.IndexOf(this);
			//var paths = new NSIndexPath[count];
			//for (int i = 0; i < count; i++)
			//    paths[i] = NSIndexPath.FromRowSection(start + i, sidx);
			//root.TableView.DeleteRows(paths, anim);
		}

		public void Clear()
		{
			foreach (Element e in Elements)
				e.Dispose();
			Elements = new List<Element>();

			var root = Parent as RootElement;
			//if (root != null && root.TableView != null)
			//    root.TableView.ReloadData();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Parent = null;
				Clear();
				Elements = null;
			}
		}

		public int GetElementViewType(Element e)
		{
			var elementType = e.GetType().FullName;

			for (int i = 0; i < ElementTypes.Count; i++)
			{
				if (ElementTypes[i].Equals(elementType))
					return i + 1;
			}

			return 0;
		}

		public int ElementViewTypeCount
		{
			get { return ElementTypes.Count; }
		}

		public override View GetView(Context context, View convertView, ViewGroup parent)
		{
			TextView view = (convertView as TextView) 
				?? new TextView(context, null, Android.Resource.Attribute.ListSeparatorTextViewStyle);

			view.Text = this.Caption;

			return view;
		}

        /// Enumerator to get all the elements in the Section.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator{T}"/>
        /// </returns>
        public IEnumerator<Element> GetEnumerator()
        {
            foreach (Element e in Elements)
                yield return e;
        }

        /// Enumerator to get all the elements in the Section.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator{T}"/>
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (Element e in Elements)
                yield return e;
        }
	}
}
