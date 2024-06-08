// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
        private static readonly object syncRoot = new();
        private static MyAndroidEditText staticEditText;

        private static EditText activeEditText;

        private MyAndroidEditText editText;

        private Action editTextSetMinLinesAction;
        private Action editTextSetMaxLinesAction;
        private Action editTextSetSelectionAction;

        private Action editTextSetActivatedStateAction;

        private class MyAndroidEditText : Android.Widget.EditText
        {
            private Action showSoftInputAction;
            private Action deselectStrideEditTextAction;

            public bool AutoFocus { get; set; }

            public MyAndroidEditText(Context context)
                : base(context)
            {
                showSoftInputAction = () =>
                {
                    System.Diagnostics.Debug.Assert(Context is not null);
                    var inputMethodManager = Context.GetSystemService(Context.InputMethodService) as InputMethodManager;
                    System.Diagnostics.Debug.Assert(inputMethodManager is not null);
                    inputMethodManager.ShowSoftInput(this, default(ShowFlags));
                };
                deselectStrideEditTextAction = () =>
                {
                    lock (syncRoot)
                    {
                        if (activeEditText is not null)
                        {
                            activeEditText.IsSelectionActive = false;
                        }
                    }
                };
            }

            public override bool OnKeyPreIme(Keycode keyCode, KeyEvent e)
            {
                if (e.KeyCode == Keycode.Back)
                {
                    if (activeEditText is not null)
                    {
                        // We use Post instead of deselecting directly because we do not want to close
                        // the PopupWindow of this control in the same execution as processing an input
                        // otherwise the control gets disposed while being active
                        Post(deselectStrideEditTextAction);
                    }
                    return true;
                }

                return base.OnKeyPreIme(keyCode, e);
            }

            public override void OnWindowFocusChanged(bool hasWindowFocus)
            {
                // Adapted from https://developer.android.com/develop/ui/views/touch-and-input/keyboard-input/visibility#ShowReliably
                // Because the Android EditText will appear in a popup, the control can start in focus
                // but it isn't immediately ready to show the soft input, so it must be done this way
                if (AutoFocus && hasWindowFocus)
                {
                    RequestFocus();
                    Post(showSoftInputAction);
                }
            }
        }

        private static void InitializeStaticImpl()
        {
        }

        // Delay creation of static edit text to last moment when we are sure to be in Android UI thread.
        // -> some Android phones crashes when native edit text is created from another thread than OS UI thread.
        private void EnsureStaticEditText()
        {
            var gameContext = GetGameContext();
            if (staticEditText is null || gameContext.RecreateEditTextPopupWindow)
            {
                // create and add the edit text
                staticEditText = new MyAndroidEditText(PlatformAndroid.Context)
                {
                    AutoFocus = true    // Will focus & display the soft input when editing begins
                };

                var popupWindow = gameContext.CreateEditTextPopup(staticEditText);
                popupWindow.DismissEvent += (sender, e) =>
                {
                    if (IsSelectionActive)
                    {
                        IsSelectionActive = false;
                    }
                };
            }
        }

        private void InitializeImpl()
        {
            // Cache all the Actions that will be used in EditText.Post to reduce object allocation.
            // EditText.Post is used because we are not on the same thread as the Android UI thread,
            // and also need to lock on syncRoot to prevent race any condition where Stride thread
            // unsets editText while we on the Android UI thread
            editTextSetMinLinesAction = () =>
            {
                lock (syncRoot)
                {
                    editText?.SetMinLines(MinLines);
                }
            };
            editTextSetMaxLinesAction = () =>
            {
                lock (syncRoot)
                {
                    editText?.SetMaxLines(MaxLines);
                }
            };
            editTextSetSelectionAction = () =>
            {
                lock (syncRoot)
                {
                    editText?.SetSelection(selectionStart, selectionStop);
                }
            };

            editTextSetActivatedStateAction = () =>
            {
                lock (syncRoot)
                {
                    var pendingEditText = staticEditText;
                    // set up the initial state of the android EditText
                    UpdateInputTypeFromSelf(pendingEditText);
                    pendingEditText.SetMinLines(MinLines);
                    pendingEditText.SetMaxLines(MaxLines);
                    pendingEditText.Text = text;
                    pendingEditText.SetSelection(selectionStart, selectionStop);

                    // add callbacks
                    pendingEditText.EditorAction += AndroidEditTextOnEditorAction;
                    pendingEditText.AfterTextChanged += AndroidEditTextOnAfterTextChanged;

                    editText = pendingEditText;
                }
            };
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

            editText.Post(editTextSetMaxLinesAction);
        }

        private void OnMinLinesChangedImpl()
        {
            if (editText == null)
                return;

            editText.Post(editTextSetMinLinesAction);
        }

        private void ActivateEditTextImpl()
        {
            lock (syncRoot)
            {
                if (activeEditText is not null)
                    throw new Exception("Internal error: Can not activate edit text, another edit text is already active");

                EnsureStaticEditText();

                activeEditText = this;

                // Select all text on initial focus.
                // TODO: Maybe make it configurable on how caret/selection should default on activation?
                SelectAll();
                // Note that we do not assign 'staticEditText' to 'editText' until the Post action
                // is actually invoked to ensure the control is fully ready
                staticEditText.Post(editTextSetActivatedStateAction);

                GetGameContext().ShowEditTextPopup();
            }
        }

        private void DeactivateEditTextImpl()
        {
            lock (syncRoot)
            {
                if (activeEditText == null)
                    throw new Exception("Internal error: Can not deactivate the EditText, it is already nullified");

                // remove callbacks
                editText.EditorAction -= AndroidEditTextOnEditorAction;
                editText.AfterTextChanged -= AndroidEditTextOnAfterTextChanged;

                editText = null;
                activeEditText = null;

                GetGameContext().HideEditTextPopup();

                FocusedElement = null;
            }
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

            UpdateInputTypeFromSelf(editText);
        }

        private void UpdateInputTypeFromSelf(MyAndroidEditText editText)
        {
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

            // Avoid infinite text changed triggering loop.
            if (editText.Text != Text)
            {
                editText.Post(() =>
                {
                    lock (syncRoot)
                    {
                        if (editText is not null)
                        {
                            editText.Text = text;
                        }
                    }
                });
            }
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

            editText.Post(editTextSetSelectionAction);
        }
    }
}

#endif
