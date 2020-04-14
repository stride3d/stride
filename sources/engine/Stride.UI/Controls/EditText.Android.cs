// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_ANDROID
using System;
using Android.Content;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Android.Text.Method;
using Stride.Core;
using Stride.Games;
using Exception = System.Exception;

namespace Stride.UI.Controls
{
    public partial class EditText
    {
        private static MyAndroidEditText staticEditText;

        private static EditText activeEditText;

        private MyAndroidEditText editText;

        private InputMethodManager inputMethodManager;

        private class MyAndroidEditText : Android.Widget.EditText
        {
            public MyAndroidEditText(Context context)
                : base(context)
            {
            }

            public override bool OnKeyPreIme(Keycode keyCode, KeyEvent e)
            {
                if (e.KeyCode == Keycode.Back)
                {
                    if (activeEditText != null)
                        activeEditText.IsSelectionActive = false;

                    return true;
                }

                return base.OnKeyPreIme(keyCode, e);
            }

            protected override void OnDetachedFromWindow()
            {
                base.OnDetachedFromWindow();

                // Edit text is invalidated and need to be recreated when application is shut down with back key
                staticEditText = null;
            }
        }

        private static void InitializeStaticImpl()
        {
        }

        // Delay creation of static edit text to last moment when we are sure to be in Android UI thread.
        // -> some Android phones crashes when native edit text is created from another thread than OS UI thread.
        private void EnsureStaticEditText()
        {
            if (staticEditText == null)
            {
                // create and add the edit text
                staticEditText = new MyAndroidEditText(PlatformAndroid.Context);

                var editLayoutParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                editLayoutParams.SetMargins(75, 200, 75, 0);
                GetGameContext().EditTextLayout.AddView(staticEditText, editLayoutParams);
            }
        }

        private void InitializeImpl()
        {
            // nothing to do here
        }
        
        private void AndroidEditTextOnAfterTextChanged(object sender, AfterTextChangedEventArgs afterTextChangedEventArgs)
        {
            if (editText == null)
                return;

            var newText = editText.Text;
            var oldStart = selectionStart;
            var newStart = SelectionStart;

            if (newStart <= oldStart) // we erased characters
            {
                SetTextInternal(newText, false);
            }
            else // we add or replaced characters
            {
                // check that new characters are correct.
                builder.Clear();
                var predicate = CharacterFilterPredicate;
                for (int i = oldStart; i < newStart; i++)
                {
                    var character = newText[i];
                    if (predicate == null || predicate(character))
                        builder.Append(character);
                }

                SetTextInternal(newText.Substring(0, oldStart) + builder + newText.Substring(newStart), false);
                newStart = Math.Min(oldStart + builder.Length, text.Length);
            }

            UpdateTextToEditImpl();
            Select(newStart, 0);
        }

        private void AndroidEditTextOnEditorAction(object sender, TextView.EditorActionEventArgs editorActionEventArgs)
        {
            if (editorActionEventArgs.ActionId == ImeAction.Done)
                IsSelectionActive = false;
        }

        private int GetLineCountImpl()
        {
            if (editText == null)
                return 0;

            return editText.LineCount;
        }
        
        private void OnMaxLinesChangedImpl()
        {
            if (editText == null)
                return;

            editText.SetMaxLines(MaxLines);
        }

        private void OnMinLinesChangedImpl()
        {
            if (editText == null)
                return;

            editText.SetMinLines(MinLines);
        }

        private void ActivateEditTextImpl()
        {
            if (activeEditText != null)
                throw new Exception("Internal error: Can not activate edit text, another edit text is already active");

            EnsureStaticEditText();

            activeEditText = this;
            editText = staticEditText;
            
            // set up the initial state of the android EditText
            UpdateInputTypeImpl();

            editText.SetMaxLines(MaxLines);
            editText.SetMinLines(MinLines);

            UpdateTextToEditImpl();
            UpdateSelectionToEditImpl();

            // add callbacks
            editText.EditorAction += AndroidEditTextOnEditorAction;
            editText.AfterTextChanged += AndroidEditTextOnAfterTextChanged;

            // add the edit to the overlay layout and show the layout
            GetGameContext().EditTextLayout.Visibility = ViewStates.Visible;

            // set the focus to the edit box
            editText.RequestFocus();

            // activate the ime (show the keyboard)
            inputMethodManager = (InputMethodManager)PlatformAndroid.Context.GetSystemService(Context.InputMethodService);
            inputMethodManager.ShowSoftInput(staticEditText, ShowFlags.Forced);
        }

        private void DeactivateEditTextImpl()
        {
            if (activeEditText == null)
                throw new Exception("Internal error: Can not deactivate the EditText, it is already nullified");

            // remove callbacks
            editText.EditorAction -= AndroidEditTextOnEditorAction;
            editText.AfterTextChanged -= AndroidEditTextOnAfterTextChanged;

            editText.ClearFocus();

            editText = null;
            activeEditText = null;

            // remove the edit text from the layout and hide the layout
            GetGameContext().EditTextLayout.Visibility = ViewStates.Gone;

            // deactivate the ime (hide the keyboard)
            if (staticEditText != null) // staticEditText can be null if window have already been detached.
                inputMethodManager.HideSoftInputFromWindow(staticEditText.WindowToken, HideSoftInputFlags.None);
            inputMethodManager = null;

            FocusedElement = null;
        }

        private GameContextAndroid GetGameContext()
        {
            if (UIElementServices.Services == null)
                throw new InvalidOperationException("services");

            var game = UIElementServices.Services.GetSafeServiceAs<IGame>();
            return ((GameContextAndroid) game.Context);
        }

        private static void OnTouchMoveImpl(TouchEventArgs args)
        {
        }

        private static void OnTouchDownImpl(TouchEventArgs args)
        {
        }

        private void OnTouchUpImpl(TouchEventArgs args)
        {
        }

        private void UpdateInputTypeImpl()
        {
            if (editText == null)
                return;

            if (ShouldHideText)
            {
                editText.TransformationMethod = new PasswordTransformationMethod();
                editText.InputType = InputTypes.ClassText | InputTypes.TextVariationPassword;
            }
            else
            {
                editText.TransformationMethod = null;
                editText.InputType = InputTypes.ClassText | InputTypes.TextVariationNormal;
            }
        }

        private void UpdateTextToEditImpl()
        {
            if (editText == null)
                return;

            if (editText.Text != Text) // avoid infinite text changed triggering loop.
                editText.Text = text;
        }
        
        private void UpdateSelectionFromEditImpl()
        {
            if (editText == null)
                return;

            selectionStart = editText.SelectionStart;
            selectionStop = editText.SelectionEnd;
        }

        private void UpdateSelectionToEditImpl()
        {
            if (editText == null)
                return;

            editText.SetSelection(selectionStart, selectionStop);
        }
    }
}

#endif
