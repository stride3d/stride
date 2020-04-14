// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using System.Windows.Markup;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Internal;

namespace Xenko.Core.Presentation.Behaviors
{
    /// <summary>
    /// Provides a way to define several cursor override on a <see cref="FrameworkElement"/>.
    /// </summary>
    /// <seealso cref="CursorOverrideRule"/>
    [ContentProperty("Rules")]
    public class MultiOverrideCursorBehavior : Behavior<FrameworkElement>, IAddChild
    {
        private readonly CursorOverrideRuleCollection rules;

        public MultiOverrideCursorBehavior()
        {
            rules = new CursorOverrideRuleCollection();
        }

        public CursorOverrideRuleCollection Rules { get { ReadPreamble(); return rules; } }

        void IAddChild.AddChild([NotNull] object value)
        {
            var rule = value as CursorOverrideRule;
            if (rule != null)
                Rules.Add(rule);
            else
                throw new ArgumentException($"Child has wrong type: {value.GetType().FullName} instead of {nameof(CursorOverrideRule)}.");
        }

        void IAddChild.AddText(string text)
        {
            // Nothing to do
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            UpdateCursorOverride();

            var cursorDescriptor = DependencyPropertyDescriptor.FromProperty(CursorOverrideRule.CursorProperty, typeof(CursorOverrideRule));
            var forceCursorDescriptor = DependencyPropertyDescriptor.FromProperty(CursorOverrideRule.ForceCursorProperty, typeof(CursorOverrideRule));
            var whenDescriptor = DependencyPropertyDescriptor.FromProperty(CursorOverrideRule.WhenProperty, typeof(CursorOverrideRule));

            foreach (var rule in Rules)
            {
                cursorDescriptor.AddValueChanged(rule, (s, a) => UpdateCursorOverride());
                forceCursorDescriptor.AddValueChanged(rule, (s, a) => UpdateCursorOverride());
                whenDescriptor.AddValueChanged(rule, (s, a) => UpdateCursorOverride());
            }
        }

        protected override void OnDetaching()
        {
            var cursorDescriptor = DependencyPropertyDescriptor.FromProperty(OverrideCursorBehavior.CursorProperty, typeof(OverrideCursorBehavior));
            var forceCursorDescriptor = DependencyPropertyDescriptor.FromProperty(OverrideCursorBehavior.ForceCursorProperty, typeof(OverrideCursorBehavior));
            var whenDescriptor = DependencyPropertyDescriptor.FromProperty(OverrideCursorBehavior.IsActiveProperty, typeof(OverrideCursorBehavior));

            foreach (var rule in Rules)
            {
                cursorDescriptor.RemoveValueChanged(rule, (s, a) => UpdateCursorOverride());
                forceCursorDescriptor.RemoveValueChanged(rule, (s, a) => UpdateCursorOverride());
                whenDescriptor.RemoveValueChanged(rule, (s, a) => UpdateCursorOverride());
            }

            AssociatedObject.Cursor = null;
            base.OnDetaching();
        }

        private void UpdateCursorOverride()
        {
            if (AssociatedObject == null)
                return;

            if (Rules.Count == 0 || !Rules.Any(r => r.When))
            {
                AssociatedObject.Cursor = null;
                AssociatedObject.ForceCursor = false;
                return;
            }

            var firstRule = Rules.First(r => r.When);
            AssociatedObject.Cursor = firstRule.Cursor;
            AssociatedObject.ForceCursor = firstRule.ForceCursor;
        }
    }

    /// <summary>
    /// Collection of <see cref="CursorOverrideRule"/>.
    /// </summary>
    public class CursorOverrideRuleCollection : FreezableCollection<CursorOverrideRule>
    { }

    public class CursorOverrideRule : Freezable
    {
        public static readonly DependencyProperty CursorProperty = DependencyProperty.Register("Cursor", typeof(Cursor), typeof(CursorOverrideRule), new PropertyMetadata(null));

        public static readonly DependencyProperty ForceCursorProperty = DependencyProperty.Register("ForceCursor", typeof(bool), typeof(CursorOverrideRule), new PropertyMetadata(BooleanBoxes.FalseBox));

        public static readonly DependencyProperty WhenProperty = DependencyProperty.Register("When", typeof(bool), typeof(CursorOverrideRule), new PropertyMetadata(BooleanBoxes.FalseBox));

        public Cursor Cursor { get { return (Cursor)GetValue(CursorProperty); } set { SetValue(CursorProperty, value); } }

        public bool ForceCursor { get { return (bool)GetValue(ForceCursorProperty); } set { SetValue(ForceCursorProperty, value.Box()); } }

        public bool When { get { return (bool)GetValue(WhenProperty); } set { SetValue(WhenProperty, value.Box()); } }

        [NotNull]
        protected override Freezable CreateInstanceCore()
        {
            return new CursorOverrideRule();
        }

        
    }
}
