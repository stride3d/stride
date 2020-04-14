// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Controls
{
    [TemplatePart(Name = MessageContainerPartName, Type = typeof(FlowDocumentScrollViewer))]
    public class MarkdownTextBlock : Control
    {
        /// <summary>
        /// The name of the part for the <see cref="FlowDocumentScrollViewer"/> container.
        /// </summary>
        private const string MessageContainerPartName = "PART_MessageContainer";

        /// <summary>
        /// Identifies the <see cref="BaseUrl"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BaseUrlProperty =
            DependencyProperty.Register(nameof(BaseUrl), typeof(string), typeof(MarkdownTextBlock), new PropertyMetadata(BaseUrlChanged));
        /// <summary>
        /// Identifies the <see cref="HyperlinkCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HyperlinkCommandProperty =
            DependencyProperty.Register(nameof(HyperlinkCommand), typeof(ICommand), typeof(MarkdownTextBlock), new PropertyMetadata(HyperlinkCommandChanged));
        /// <summary>
        /// Identifies the <see cref="Markdown"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MarkdownProperty =
            DependencyProperty.Register(nameof(Markdown), typeof(XamlMarkdown), typeof(MarkdownTextBlock), new PropertyMetadata(MarkdownChanged));
        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(MarkdownTextBlock), new PropertyMetadata(TextChanged));

        public string BaseUrl
        {
            get { return (string)GetValue(BaseUrlProperty); }
            set { SetValue(BaseUrlProperty, value); }
        }

        public ICommand HyperlinkCommand
        {
            get { return (ICommand)GetValue(HyperlinkCommandProperty); }
            set { SetValue(HyperlinkCommandProperty, value); }
        }

        public XamlMarkdown Markdown
        {
            get { return (XamlMarkdown)GetValue(MarkdownProperty); }
            set { SetValue(MarkdownProperty, value); }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// The container in which the message is displayed.
        /// </summary>
        private FlowDocumentScrollViewer messageContainer;

        /// <summary>
        /// Default markdown used if none is supplied.
        /// </summary>
        private readonly Lazy<XamlMarkdown> defaultMarkdown;

        static MarkdownTextBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MarkdownTextBlock), new FrameworkPropertyMetadata(typeof(MarkdownTextBlock)));
        }

        public MarkdownTextBlock()
        {
            defaultMarkdown = new Lazy<XamlMarkdown>(() => new XamlMarkdown(this));
        }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            messageContainer = GetTemplateChild(MessageContainerPartName) as FlowDocumentScrollViewer;
            if (messageContainer == null)
                throw new InvalidOperationException($"A part named '{MessageContainerPartName}' must be present in the ControlTemplate, and must be of type '{typeof(FlowDocumentScrollViewer)}'.");

            ResetMessage();
        }

        private static void BaseUrlChanged([NotNull] DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as MarkdownTextBlock;
            if (control == null) throw new ArgumentNullException(nameof(control));

            if (e.NewValue != null)
            {
                control.GetMarkdown().BaseUrl = (string)e.NewValue;
            }
            control.ResetMessage();
        }

        private static void HyperlinkCommandChanged([NotNull] DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as MarkdownTextBlock;
            if (control == null) throw new ArgumentNullException(nameof(control));

            if (e.NewValue != null)
            {
                control.GetMarkdown().HyperlinkCommand = (ICommand)e.NewValue;
            }
            control.ResetMessage();
        }

        private static void MarkdownChanged([NotNull] DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as MarkdownTextBlock;
            if (control == null) throw new ArgumentNullException(nameof(control));

            if (e.NewValue != null)
            {
                ((XamlMarkdown)e.NewValue).BaseUrl = control.BaseUrl;
                ((XamlMarkdown)e.NewValue).HyperlinkCommand = control.HyperlinkCommand;
            }
            control.ResetMessage();
        }

        private static void TextChanged([NotNull] DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as MarkdownTextBlock;
            if (control == null) throw new ArgumentNullException(nameof(control));

            control.ResetMessage();
        }

        [NotNull]
        private XamlMarkdown GetMarkdown()
        {
            return Markdown ?? defaultMarkdown.Value;
        }

        private void ResetMessage()
        {
            if (messageContainer != null)
            {
                messageContainer.Document = ProcessText();
            }
        }

        [CanBeNull]
        private FlowDocument ProcessText()
        {
            try
            {
                return GetMarkdown().Transform(Text ?? "*Nothing to display*");
            }
            catch (ArgumentException) { }
            catch (FormatException) { }
            catch (InvalidOperationException) { }

            return null;
        }
    }
}
