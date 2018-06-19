// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// TouchViewController.cs
//
// Authors:
//	Sebastien Pouliot  <sebastien@xamarin.com>
//
// Copyright 2012 Xamarin Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Drawing;

#if XAMCORE_2_0
using CoreGraphics;
using Foundation;
using UIKit;
#else
using MonoTouch.CoreGraphics;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

using MonoTouch.Dialog;

#if !HAVE_NATIVE_TYPES
using CGSize = global::System.Drawing.SizeF;
#endif

namespace Xenko.UnitTesting.UI {

	[CLSCompliant (false)]
	public partial class TouchViewController : DialogViewController {

		public TouchViewController (RootElement root) : base (root, true)
		{
			Autorotate = true;

			if (UIDevice.CurrentDevice.CheckSystemVersion (5, 0)) {
				NavigationItem.RightBarButtonItems = new UIBarButtonItem [] {
					new UIBarButtonItem (ArrowDown, UIBarButtonItemStyle.Plain, ChangeSort),
					new UIBarButtonItem (Asterisk, UIBarButtonItemStyle.Plain, ChangeFilter)
				};
			}

			Section testcases = root [0];
			OriginalCaption = testcases.Caption;
			Unfiltered = new List<Element> ();
			foreach (Element e in testcases)
				Unfiltered.Add (e);

			CurrentFilter = ResultFilter.All;
			Filter ();
			CurrentSortOrder = SortOrder.None;
			if (TouchOptions.Current.SortNames)
				ChangeSort (this, EventArgs.Empty);
		}

		// Filter

		UIBarButtonItem FilterButton {
			get {
				return NavigationItem.RightBarButtonItems [1];
			}
		}

		enum ResultFilter {
			All,
			Failed,
			Ignored,
			Success,
		}

		string OriginalCaption { get; set; }
		List<Element> Unfiltered { get; set; }

		ResultFilter CurrentFilter { get; set; }

		void ChangeFilter (object sender, EventArgs e)
		{
			switch (CurrentFilter) {
			case ResultFilter.All:
				CurrentFilter = ResultFilter.Failed;
				FilterButton.Image = RemoveSign;
				break;
			case ResultFilter.Failed:
				CurrentFilter = ResultFilter.Ignored;
				FilterButton.Image = QuestionSign;
				break;
			case ResultFilter.Ignored:
				CurrentFilter = ResultFilter.Success;
				FilterButton.Image = OkSign;
				break;
			case ResultFilter.Success:
				CurrentFilter = ResultFilter.All;
				FilterButton.Image = Asterisk;
				break;
			}
			Filter ();
		}

		public void Filter ()
		{
			Section filtered = new Section ();
			foreach (TestElement te in Unfiltered) {
				bool add_element = false;
				switch (CurrentFilter) {
				case ResultFilter.All:
					add_element = true;
					break;
				case ResultFilter.Failed:
					add_element = te.Result.IsFailure ();
					break;
				case ResultFilter.Ignored:
					add_element = te.Result.IsIgnored ();
					break;
				case ResultFilter.Success:
					add_element = te.Result.IsSuccess () || te.Result.IsInconclusive ();
					break;
				}

				if (add_element)
					filtered.Add (te);
			}
			Root.RemoveAt (0);
			if (CurrentFilter == ResultFilter.All) {
				filtered.Caption = String.Format ("{0} ({1})", OriginalCaption, Unfiltered.Count);
			} else {
				filtered.Caption = String.Format ("{0} ({1} : {2}/{3})", OriginalCaption, CurrentFilter, filtered.Count, Unfiltered.Count);
			}
			Root.Insert (0, filtered);
			ReloadData ();
		}

		// Sort

		UIBarButtonItem SortButton {
			get {
				return NavigationItem.RightBarButtonItems [0];
			}
		}

		enum SortOrder {
			None,
			Ascending,
			Descending
		}

		SortOrder CurrentSortOrder { get; set; }

		static ElementComparer Ascending = new ElementComparer (SortOrder.Ascending);
		static ElementComparer Descending = new ElementComparer (SortOrder.Descending);

		void ChangeSort (object sender, EventArgs e)
		{
			List<Element> list = Root [0].Elements;
			switch (CurrentSortOrder) {
			case SortOrder.Ascending:
				SortButton.Image = ArrowUp;
				CurrentSortOrder = SortOrder.Descending;
				list.Sort (Descending);
				break;
			default:
				SortButton.Image = ArrowDown;
				CurrentSortOrder = SortOrder.Ascending;
				list.Sort (Ascending);
				break;
			}
			ReloadData ();
		}

		class ElementComparer : IComparer <Element> {
			int order;

			public ElementComparer (SortOrder sortOrder)
			{
				order = sortOrder == SortOrder.Descending ? -1 : 1;
			}

			public int Compare (Element x, Element y)
			{
				return order * x.Caption.CompareTo (y.Caption);
			}
		}

		// UI

		static UIImage arrow_up;
		static UIImage arrow_down;
		static UIImage ok_sign;
		static UIImage remove_sign;
		static UIImage question_sign;
		static UIImage asterisk;

		static UIImage ArrowUp {
			get {
				if (arrow_up == null)
					arrow_up = GetAwesomeIcon (icon_arrow_up);
				return arrow_up;
			}
		}

		static UIImage ArrowDown {
			get {
				if (arrow_down == null)
					arrow_down = GetAwesomeIcon (icon_arrow_down);
				return arrow_down;
			}
		}

		static UIImage OkSign {
			get {
				if (ok_sign == null)
					ok_sign = GetAwesomeIcon (icon_ok_sign);
				return ok_sign;
			}
		}

		static UIImage RemoveSign {
			get {
				if (remove_sign == null)
					remove_sign = GetAwesomeIcon (icon_remove_sign);
				return remove_sign;
			}
		}

		static UIImage QuestionSign {
			get {
				if (question_sign == null)
					question_sign = GetAwesomeIcon (icon_question_sign);
				return question_sign;
			}
		}


		static UIImage Asterisk {
			get {
				if (asterisk == null)
					asterisk = GetAwesomeIcon (icon_asterisk);
				return asterisk;
			}
		}

		static UIImage GetAwesomeIcon (Action<CGContext> render)
		{
			// 20x20 normal, 40x40 retina
			// https://developer.apple.com/library/ios/#documentation/UserExperience/Conceptual/MobileHIG/IconsImages/IconsImages.html
			// http://tirania.org/blog/archive/2010/Jul-20-2.html
			float size = 20f;
			UIGraphics.BeginImageContextWithOptions (new CGSize (size, size), false, 0);
			using (var c = UIGraphics.GetCurrentContext ()) {
				c.SetFillColor (1.0f, 1.0f, 1.0f, 1.0f);
				c.SetStrokeColor (1.0f, 1.0f, 1.0f, 1.0f);
				c.TranslateCTM (3f, size - 3f);
				c.ScaleCTM (size / 1000, -size / 1000);
				render (c);
			}
			UIImage img = UIGraphics.GetImageFromCurrentImageContext ();
			UIGraphics.EndImageContext ();
			return img;
		}

		#region generated code

		static void icon_arrow_up (CGContext c)
		{
			c.MoveTo (-0.5f, 375f);
			c.AddQuadCurveToPoint (-1f, 394f, 13f, 408f);
			c.AddLineToPoint (342f, 736f);
			c.AddQuadCurveToPoint (356f, 750f, 375f, 750f);
			c.AddQuadCurveToPoint (394f, 750f, 408f, 736f);
			c.AddLineToPoint (736f, 408f);
			c.AddQuadCurveToPoint (750f, 394f, 750f, 375f);
			c.AddQuadCurveToPoint (750f, 356f, 736f, 342f);
			c.AddLineToPoint (687f, 293f);
			c.AddQuadCurveToPoint (673f, 279f, 654.5f, 279f);
			c.AddQuadCurveToPoint (636f, 279f, 622f, 293f);
			c.AddLineToPoint (456f, 458f);
			c.AddLineToPoint (456f, 46f);
			c.AddQuadCurveToPoint (456f, 27f, 442.5f, 13.5f);
			c.AddQuadCurveToPoint (429f, 0f, 410f, 0f);
			c.AddLineToPoint (340f, 0f);
			c.AddQuadCurveToPoint (320f, 0f, 307f, 13.5f);
			c.AddQuadCurveToPoint (294f, 27f, 294f, 46f);
			c.AddLineToPoint (294f, 458f);
			c.AddLineToPoint (129f, 293f);
			c.AddQuadCurveToPoint (115f, 279f, 96f, 279f);
			c.AddQuadCurveToPoint (77f, 279f, 63f, 293f);
			c.AddLineToPoint (14f, 342f);
			c.AddQuadCurveToPoint (0f, 356f, -0.5f, 375f);
			c.ClosePath ();
			c.MoveTo (-0.5f, 375f);
			c.FillPath ();
			c.StrokePath ();
		}
		
		static void icon_arrow_down (CGContext c)
		{
			c.MoveTo (0f, 374f);
			c.AddQuadCurveToPoint (0f, 393f, 14f, 407f);
			c.AddLineToPoint (63f, 456f);
			c.AddQuadCurveToPoint (77f, 470f, 96f, 470f);
			c.AddQuadCurveToPoint (115f, 470f, 129f, 456f);
			c.AddLineToPoint (294f, 291f);
			c.AddLineToPoint (294f, 703f);
			c.AddQuadCurveToPoint (294f, 722f, 307.5f, 735.5f);
			c.AddQuadCurveToPoint (321f, 749f, 340f, 749f);
			c.AddLineToPoint (410f, 749f);
			c.AddQuadCurveToPoint (430f, 749f, 443f, 735.5f);
			c.AddQuadCurveToPoint (456f, 722f, 456f, 703f);
			c.AddLineToPoint (456f, 291f);
			c.AddLineToPoint (622f, 456f);
			c.AddQuadCurveToPoint (636f, 470f, 654.5f, 470f);
			c.AddQuadCurveToPoint (673f, 470f, 687f, 456f);
			c.AddLineToPoint (737f, 407f);
			c.AddQuadCurveToPoint (751f, 393f, 751f, 374f);
			c.AddQuadCurveToPoint (751f, 355f, 737f, 341f);
			c.AddLineToPoint (408f, 13f);
			c.AddQuadCurveToPoint (394f, -1f, 375f, -1f);
			c.AddQuadCurveToPoint (356f, -1f, 342f, 13f);
			c.AddLineToPoint (14f, 341f);
			c.AddQuadCurveToPoint (0f, 355f, 0f, 374f);
			c.ClosePath ();
			c.MoveTo (0f, 374f);
			c.FillPath ();
			c.StrokePath ();
		}

		static void icon_remove_sign (CGContext c)
		{
			c.MoveTo (0f, 376f);
			c.AddQuadCurveToPoint (0f, 448f, 27.5f, 517f);
			c.AddQuadCurveToPoint (55f, 586f, 110f, 641f);
			c.AddQuadCurveToPoint (165f, 696f, 234f, 723f);
			c.AddQuadCurveToPoint (303f, 750f, 375f, 750f);
			c.AddQuadCurveToPoint (447f, 750f, 516f, 723f);
			c.AddQuadCurveToPoint (585f, 696f, 640f, 641f);
			c.AddQuadCurveToPoint (695f, 586f, 722.5f, 517f);
			c.AddQuadCurveToPoint (750f, 448f, 750f, 376f);
			c.AddQuadCurveToPoint (750f, 304f, 722.5f, 235f);
			c.AddQuadCurveToPoint (695f, 166f, 640f, 111f);
			c.AddQuadCurveToPoint (585f, 56f, 516f, 28.5f);
			c.AddQuadCurveToPoint (447f, 1f, 375f, 1f);
			c.AddQuadCurveToPoint (303f, 1f, 234f, 28.5f);
			c.AddQuadCurveToPoint (165f, 56f, 110f, 111f);
			c.AddQuadCurveToPoint (55f, 166f, 27.5f, 235f);
			c.AddQuadCurveToPoint (0f, 304f, 0f, 376f);
			c.ClosePath ();
			c.MoveTo (0f, 376f);
			c.MoveTo (185f, 240f);
			c.AddLineToPoint (240f, 186f);
			c.AddQuadCurveToPoint (245f, 181f, 251f, 181f);
			c.AddQuadCurveToPoint (257f, 181f, 262f, 186f);
			c.AddLineToPoint (376f, 300f);
			c.AddLineToPoint (479f, 196f);
			c.AddQuadCurveToPoint (484f, 191f, 490f, 191f);
			c.AddQuadCurveToPoint (496f, 191f, 501f, 196f);
			c.AddLineToPoint (554f, 249f);
			c.AddQuadCurveToPoint (565f, 260f, 554f, 271f);
			c.AddLineToPoint (450f, 374f);
			c.AddLineToPoint (565f, 489f);
			c.AddQuadCurveToPoint (576f, 500f, 565f, 511f);
			c.AddLineToPoint (510f, 566f);
			c.AddQuadCurveToPoint (499f, 577f, 488f, 566f);
			c.AddLineToPoint (374f, 451f);
			c.AddLineToPoint (270f, 555f);
			c.AddQuadCurveToPoint (259f, 566f, 248f, 555f);
			c.AddLineToPoint (196f, 502f);
			c.AddQuadCurveToPoint (191f, 497f, 191f, 491f);
			c.AddQuadCurveToPoint (191f, 485f, 196f, 480f);
			c.AddLineToPoint (299f, 377f);
			c.AddLineToPoint (185f, 262f);
			c.AddQuadCurveToPoint (175f, 252f, 185f, 240f);
			c.ClosePath ();
			c.MoveTo (185f, 240f);
			c.FillPath ();
			c.StrokePath ();
		}
		
		static void icon_ok_sign (CGContext c)
		{
			c.MoveTo (0f, 375f);
			c.AddQuadCurveToPoint (0f, 453f, 29.5f, 521f);
			c.AddQuadCurveToPoint (59f, 589f, 110f, 640f);
			c.AddQuadCurveToPoint (161f, 691f, 229f, 720.5f);
			c.AddQuadCurveToPoint (297f, 750f, 375f, 750f);
			c.AddQuadCurveToPoint (453f, 750f, 521f, 720.5f);
			c.AddQuadCurveToPoint (589f, 691f, 640f, 640f);
			c.AddQuadCurveToPoint (691f, 589f, 720.5f, 521f);
			c.AddQuadCurveToPoint (750f, 453f, 750f, 375f);
			c.AddQuadCurveToPoint (750f, 297f, 720.5f, 229f);
			c.AddQuadCurveToPoint (691f, 161f, 640f, 110f);
			c.AddQuadCurveToPoint (589f, 59f, 521f, 29.5f);
			c.AddQuadCurveToPoint (453f, 0f, 375f, 0f);
			c.AddQuadCurveToPoint (297f, 0f, 229f, 29.5f);
			c.AddQuadCurveToPoint (161f, 59f, 110f, 110f);
			c.AddQuadCurveToPoint (59f, 161f, 29.5f, 229f);
			c.AddQuadCurveToPoint (0f, 297f, 0f, 375f);
			c.ClosePath ();
			c.MoveTo (0f, 375f);
			c.MoveTo (112f, 351.5f);
			c.AddQuadCurveToPoint (112f, 342f, 119f, 335f);
			c.AddLineToPoint (269f, 185f);
			c.AddQuadCurveToPoint (276f, 179f, 287f, 174f);
			c.AddQuadCurveToPoint (298f, 169f, 308f, 169f);
			c.AddLineToPoint (333f, 169f);
			c.AddQuadCurveToPoint (343f, 169f, 354f, 174f);
			c.AddQuadCurveToPoint (365f, 179f, 372f, 185f);
			c.AddLineToPoint (631f, 444f);
			c.AddQuadCurveToPoint (638f, 451f, 638f, 460.5f);
			c.AddQuadCurveToPoint (638f, 470f, 631f, 476f);
			c.AddLineToPoint (581f, 526f);
			c.AddQuadCurveToPoint (575f, 533f, 565.5f, 533f);
			c.AddQuadCurveToPoint (556f, 533f, 549f, 526f);
			c.AddLineToPoint (337f, 313f);
			c.AddQuadCurveToPoint (330f, 306f, 320.5f, 306f);
			c.AddQuadCurveToPoint (311f, 306f, 305f, 313f);
			c.AddLineToPoint (201f, 417f);
			c.AddQuadCurveToPoint (194f, 424f, 184.5f, 424f);
			c.AddQuadCurveToPoint (175f, 424f, 169f, 417f);
			c.AddLineToPoint (119f, 368f);
			c.AddQuadCurveToPoint (112f, 361f, 112f, 351.5f);
			c.ClosePath ();
			c.MoveTo (112f, 351.5f);
			c.FillPath ();
			c.StrokePath ();
		}
		
		static void icon_question_sign (CGContext c)
		{
			c.MoveTo (0f, 375f);
			c.AddQuadCurveToPoint (0f, 453f, 29.5f, 521f);
			c.AddQuadCurveToPoint (59f, 589f, 110f, 640f);
			c.AddQuadCurveToPoint (161f, 691f, 229f, 720.5f);
			c.AddQuadCurveToPoint (297f, 750f, 375f, 750f);
			c.AddQuadCurveToPoint (453f, 750f, 521f, 720.5f);
			c.AddQuadCurveToPoint (589f, 691f, 640f, 640f);
			c.AddQuadCurveToPoint (691f, 589f, 720.5f, 521f);
			c.AddQuadCurveToPoint (750f, 453f, 750f, 375f);
			c.AddQuadCurveToPoint (750f, 297f, 720.5f, 229f);
			c.AddQuadCurveToPoint (691f, 161f, 640f, 110f);
			c.AddQuadCurveToPoint (589f, 59f, 521f, 29.5f);
			c.AddQuadCurveToPoint (453f, 0f, 375f, 0f);
			c.AddQuadCurveToPoint (297f, 0f, 229f, 29.5f);
			c.AddQuadCurveToPoint (161f, 59f, 110f, 110f);
			c.AddQuadCurveToPoint (59f, 161f, 29.5f, 229f);
			c.AddQuadCurveToPoint (0f, 297f, 0f, 375f);
			c.ClosePath ();
			c.MoveTo (0f, 375f);
			c.MoveTo (250f, 531f);
			c.AddLineToPoint (294f, 476f);
			c.AddQuadCurveToPoint (300f, 472f, 304f, 471f);
			c.AddQuadCurveToPoint (310f, 471f, 314f, 475f);
			c.AddQuadCurveToPoint (322f, 481f, 332f, 486f);
			c.AddQuadCurveToPoint (340f, 490f, 350.5f, 493.5f);
			c.AddQuadCurveToPoint (361f, 497f, 372f, 497f);
			c.AddQuadCurveToPoint (392f, 497f, 405f, 486.5f);
			c.AddQuadCurveToPoint (418f, 476f, 418f, 460f);
			c.AddQuadCurveToPoint (418f, 443f, 406.5f, 429.5f);
			c.AddQuadCurveToPoint (395f, 416f, 378f, 401f);
			c.AddQuadCurveToPoint (367f, 392f, 356f, 381.5f);
			c.AddQuadCurveToPoint (345f, 371f, 336f, 357.5f);
			c.AddQuadCurveToPoint (327f, 344f, 321f, 327.5f);
			c.AddQuadCurveToPoint (315f, 311f, 315f, 290f);
			c.AddLineToPoint (315f, 260f);
			c.AddQuadCurveToPoint (315f, 255f, 319.5f, 250.5f);
			c.AddQuadCurveToPoint (324f, 246f, 329f, 246f);
			c.AddLineToPoint (406f, 246f);
			c.AddQuadCurveToPoint (412f, 246f, 416f, 250.5f);
			c.AddQuadCurveToPoint (420f, 255f, 420f, 260f);
			c.AddLineToPoint (420f, 285f);
			c.AddQuadCurveToPoint (420f, 303f, 432f, 316f);
			c.AddQuadCurveToPoint (444f, 329f, 461f, 344f);
			c.AddQuadCurveToPoint (473f, 354f, 485f, 365.5f);
			c.AddQuadCurveToPoint (497f, 377f, 506.5f, 392f);
			c.AddQuadCurveToPoint (516f, 407f, 522.5f, 425f);
			c.AddQuadCurveToPoint (529f, 443f, 529f, 467f);
			c.AddQuadCurveToPoint (529f, 499f, 516f, 524f);
			c.AddQuadCurveToPoint (503f, 549f, 481.5f, 565.5f);
			c.AddQuadCurveToPoint (460f, 582f, 433f, 590.5f);
			c.AddQuadCurveToPoint (406f, 599f, 379f, 599f);
			c.AddQuadCurveToPoint (349f, 599f, 325.5f, 591.5f);
			c.AddQuadCurveToPoint (302f, 584f, 285.5f, 575f);
			c.AddQuadCurveToPoint (269f, 566f, 260.5f, 558f);
			c.AddQuadCurveToPoint (252f, 550f, 251f, 549f);
			c.AddQuadCurveToPoint (242f, 540f, 250f, 531f);
			c.ClosePath ();
			c.MoveTo (250f, 531f);
			c.MoveTo (315f, 132f);
			c.AddQuadCurveToPoint (315f, 127f, 319.5f, 122.5f);
			c.AddQuadCurveToPoint (324f, 118f, 329f, 118f);
			c.AddLineToPoint (406f, 118f);
			c.AddQuadCurveToPoint (412f, 118f, 416f, 122.5f);
			c.AddQuadCurveToPoint (420f, 127f, 420f, 132f);
			c.AddLineToPoint (420f, 206f);
			c.AddQuadCurveToPoint (420f, 220f, 406f, 220f);
			c.AddLineToPoint (329f, 220f);
			c.AddQuadCurveToPoint (324f, 220f, 319.5f, 216f);
			c.AddQuadCurveToPoint (315f, 212f, 315f, 206f);
			c.AddLineToPoint (315f, 132f);
			c.ClosePath ();
			c.MoveTo (315f, 132f);
			c.FillPath ();
			c.StrokePath ();
		}

		static void icon_asterisk (CGContext c)
		{
			c.MoveTo (1f, 497f);
			c.AddQuadCurveToPoint (-4f, 515f, 6f, 532f);
			c.AddLineToPoint (41f, 593f);
			c.AddQuadCurveToPoint (51f, 610f, 69.5f, 614.5f);
			c.AddQuadCurveToPoint (88f, 619f, 105f, 610f);
			c.AddLineToPoint (267f, 516f);
			c.AddLineToPoint (267f, 703f);
			c.AddQuadCurveToPoint (267f, 723f, 280.5f, 736.5f);
			c.AddQuadCurveToPoint (294f, 750f, 314f, 750f);
			c.AddLineToPoint (383f, 750f);
			c.AddQuadCurveToPoint (403f, 750f, 416.5f, 736.5f);
			c.AddQuadCurveToPoint (430f, 723f, 430f, 704f);
			c.AddLineToPoint (430f, 516f);
			c.AddLineToPoint (592f, 610f);
			c.AddQuadCurveToPoint (609f, 619f, 627.5f, 614.5f);
			c.AddQuadCurveToPoint (646f, 610f, 656f, 593f);
			c.AddLineToPoint (690f, 532f);
			c.AddQuadCurveToPoint (700f, 515f, 695.5f, 497f);
			c.AddQuadCurveToPoint (691f, 479f, 674f, 469f);
			c.AddLineToPoint (511f, 375f);
			c.AddLineToPoint (674f, 281f);
			c.AddQuadCurveToPoint (691f, 271f, 695.5f, 253f);
			c.AddQuadCurveToPoint (700f, 235f, 691f, 218f);
			c.AddLineToPoint (656f, 157f);
			c.AddQuadCurveToPoint (646f, 140f, 627.5f, 135.5f);
			c.AddQuadCurveToPoint (609f, 131f, 592f, 140f);
			c.AddLineToPoint (430f, 234f);
			c.AddLineToPoint (430f, 47f);
			c.AddQuadCurveToPoint (430f, 27f, 416.5f, 13.5f);
			c.AddQuadCurveToPoint (403f, 0f, 383f, 0f);
			c.AddLineToPoint (314f, 0f);
			c.AddQuadCurveToPoint (294f, 0f, 280.5f, 13.5f);
			c.AddQuadCurveToPoint (267f, 27f, 267f, 46f);
			c.AddLineToPoint (267f, 234f);
			c.AddLineToPoint (105f, 140f);
			c.AddQuadCurveToPoint (88f, 130f, 69.5f, 135f);
			c.AddQuadCurveToPoint (51f, 140f, 41f, 157f);
			c.AddLineToPoint (6f, 218f);
			c.AddQuadCurveToPoint (-3f, 235f, 1.5f, 253f);
			c.AddQuadCurveToPoint (6f, 271f, 23f, 281f);
			c.AddLineToPoint (186f, 375f);
			c.AddLineToPoint (23f, 469f);
			c.AddQuadCurveToPoint (6f, 479f, 1f, 497f);
			c.ClosePath ();
			c.MoveTo (1f, 497f);
			c.FillPath ();
			c.StrokePath ();
		}

		#endregion
	}
}
