// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Extensions;

namespace Stride.Core.Assets.Editor.View
{
    /// <summary>
    /// Stride generic wpf data grid. Left empty for future development on a generic datagrid.
    /// </summary>
    public class DataGridEx : DataGrid
    {

    }

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

        public void AddCharToPrefix(string nextChar)
        {
            AddCharToPrefixMethod.Invoke(textSearch, new object[] { nextChar });
        }

        public void ResetTimeout()
        {
            ResetTimeoutMethod.Invoke(textSearch, new object[] { });
        }
    }
}
