// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Extensions;

using Xceed.Wpf.DataGrid;

namespace Xenko.Core.Assets.Editor.View
{
    /// <summary>
    /// This class wraps the <see cref="TextSearch"/> class, making accessible all members that are required to make the feature work.
    /// <see cref="TextSearch"/> is massively internal, making this feature impossible to implement by default on custom controls.
    /// </summary>
    internal class TextSearchWrapper
    {
        private static readonly MethodInfo EnsureInstanceMethod;
        private static readonly MethodInfo GetCultureMethod;
        private static readonly MethodInfo GetPrimaryTextPathMethod;
        private static readonly MethodInfo FindMatchingPrefixMethod;
        private static readonly MethodInfo AddCharToPrefixMethod;
        private static readonly MethodInfo ResetTimeoutMethod;
        private static readonly PropertyInfo IsActiveProperty;
        private static readonly PropertyInfo MatchedItemIndexProperty;
        private static readonly PropertyInfo PrefixProperty;
        private static readonly FieldInfo CharsEnteredField;
        private static readonly bool ReflectionCompleted;
        private readonly TextSearch textSearch;

        static TextSearchWrapper()
        {
            try
            {
                EnsureInstanceMethod = typeof(TextSearch).GetMethod("EnsureInstance", BindingFlags.NonPublic | BindingFlags.Static);
                GetCultureMethod = typeof(TextSearch).GetMethod("GetCulture", BindingFlags.NonPublic | BindingFlags.Static);
                GetPrimaryTextPathMethod = typeof(TextSearch).GetMethod("GetPrimaryTextPath", BindingFlags.NonPublic | BindingFlags.Static);
                var types = new[] { typeof(ItemsControl), typeof(string), typeof(string), typeof(string), typeof(int), typeof(bool), typeof(bool).MakeByRefType() };
                FindMatchingPrefixMethod = typeof(TextSearch).GetMethod("FindMatchingPrefix", BindingFlags.NonPublic | BindingFlags.Static, null, types, null);
                AddCharToPrefixMethod = typeof(TextSearch).GetMethod("AddCharToPrefix", BindingFlags.NonPublic | BindingFlags.Instance);
                ResetTimeoutMethod = typeof(TextSearch).GetMethod("ResetTimeout", BindingFlags.NonPublic | BindingFlags.Instance);
                IsActiveProperty = typeof(TextSearch).GetProperty("IsActive", BindingFlags.NonPublic | BindingFlags.Instance);
                MatchedItemIndexProperty = typeof(TextSearch).GetProperty("MatchedItemIndex", BindingFlags.NonPublic | BindingFlags.Instance);
                PrefixProperty = typeof(TextSearch).GetProperty("Prefix", BindingFlags.NonPublic | BindingFlags.Instance);
                CharsEnteredField = typeof(TextSearch).GetField("_charsEntered", BindingFlags.NonPublic | BindingFlags.Instance);
                ReflectionCompleted = EnsureInstanceMethod != null && GetCultureMethod != null && GetPrimaryTextPathMethod != null
                    && FindMatchingPrefixMethod != null && AddCharToPrefixMethod != null && ResetTimeoutMethod != null && IsActiveProperty != null
                    && MatchedItemIndexProperty != null && PrefixProperty != null && CharsEnteredField != null;
            }
            catch (Exception e)
            {
                e.Ignore();
            }
        }

        public bool IsActive { get { return (bool)IsActiveProperty.GetValue(textSearch); } set { IsActiveProperty.SetValue(textSearch, value); } }

        public int MatchedItemIndex { get { return (int)MatchedItemIndexProperty.GetValue(textSearch); } set { MatchedItemIndexProperty.SetValue(textSearch, value); } }

        public string Prefix { get { return (string)PrefixProperty.GetValue(textSearch); } set { PrefixProperty.SetValue(textSearch, value); } }

        // ReSharper disable InconsistentNaming
        public List<string> _charsEntered { get { return (List<string>)CharsEnteredField.GetValue(textSearch); } set { CharsEnteredField.SetValue(textSearch, value); } }
        // ReSharper restore InconsistentNaming

        private TextSearchWrapper(TextSearch textSearch)
        {
            this.textSearch = textSearch;
        }

        public static TextSearchWrapper EnsureInstance(ItemsControl itemsControl)
        {
            if (ReflectionCompleted)
            {
                var textSearch = (TextSearch)EnsureInstanceMethod.Invoke(null, new object[] { itemsControl });
                return textSearch != null ? new TextSearchWrapper(textSearch) : null;
            }
            return null;
        }

        public static CultureInfo GetCulture(DependencyObject element)
        {
            return (CultureInfo)GetCultureMethod.Invoke(null, new object[] { element });
        }

        public static string GetPrimaryTextPath(ItemsControl itemsControl)
        {
            return (string)GetPrimaryTextPathMethod.Invoke(null, new object[] { itemsControl });

        }

        public static int FindMatchingPrefix(DataGridEx dataGridEx, string primaryTextPath, string prefix, string nextChar, int startItemIndex, bool lookForFallbackMatchToo, ref bool wasNewCharUsed)
        {
            var parameters = new object[] { dataGridEx, primaryTextPath, prefix, nextChar, startItemIndex, lookForFallbackMatchToo, wasNewCharUsed };
            var result = (int)FindMatchingPrefixMethod.Invoke(null, parameters);
            wasNewCharUsed = (bool)parameters[6];
            return result;
        }

        public void AddCharToPrefix(string nextChar)
        {
            AddCharToPrefixMethod.Invoke(textSearch, new object[] { nextChar });
        }

        public void ResetTimeout()
        {
            ResetTimeoutMethod.Invoke(textSearch, new object[] { });
        }
    }

    /// <summary>
    /// An implementation of DataGrid class that inherits from Xceed DataGridControl and add support for <see cref="TextSearch"/>.
    /// </summary>
    public class DataGridEx : DataGridControl
    {
        /// <inheritdoc/>
        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            base.OnTextInput(e);
            if (string.IsNullOrEmpty(e.Text) || !IsTextSearchEnabled || !Equals(e.Source, this) && !Equals(ItemsControlFromItemContainer(e.Source as DependencyObject), this))
                return;

            var textSearch = TextSearchWrapper.EnsureInstance(this);
            if (textSearch == null)
                return;

            DoSearch(textSearch, e.Text);
            e.Handled = true;
        }

        /// <summary>
        /// This method reimplements DoSearch from <see cref="TextSearch"/>.
        /// </summary>
        /// <param name="textSearch"></param>
        /// <param name="nextChar"></param>
        private void DoSearch(TextSearchWrapper textSearch, string nextChar)
        {
            bool lookForFallbackMatchToo = false;
            int startItemIndex = 0;
            ItemCollection items = Items;
            if (textSearch.IsActive)
                startItemIndex = textSearch.MatchedItemIndex;
            if (textSearch._charsEntered.Count > 0 && string.Compare(textSearch._charsEntered[textSearch._charsEntered.Count - 1], nextChar, true, TextSearchWrapper.GetCulture(this)) == 0)
                lookForFallbackMatchToo = true;
            string primaryTextPath = TextSearchWrapper.GetPrimaryTextPath(this);
            bool wasNewCharUsed = false;
            int matchingPrefix = TextSearchWrapper.FindMatchingPrefix(this, primaryTextPath, textSearch.Prefix, nextChar, startItemIndex, lookForFallbackMatchToo, ref wasNewCharUsed);
            if (matchingPrefix != -1)
            {
                if (!textSearch.IsActive || matchingPrefix != startItemIndex)
                {
                    if (SelectedItem != items[matchingPrefix])
                    {
                        SelectedItem = items[matchingPrefix];
                        BringItemIntoView(SelectedItem);
                        UpdateLayout();
                        var container = GetContainerFromItem(SelectedItem) as DataRow;
                        if (container != null)
                        {
                            var cellToFocus = container.FindVisualChildrenOfType<DataCell>().FirstOrDefault(x => x.Focusable);
                            if (cellToFocus != null)
                                Keyboard.Focus(cellToFocus);
                        }
                    }
                    textSearch.MatchedItemIndex = matchingPrefix;
                }
                if (wasNewCharUsed)
                    textSearch.AddCharToPrefix(nextChar);
                if (!textSearch.IsActive)
                    textSearch.IsActive = true;
            }
            if (textSearch.IsActive)
                textSearch.ResetTimeout();
        }
    }
}
