// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if DEBUG

using System.Windows;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.Windows.Markup;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Internal;

// Code from http://www.wpfmentor.com/2009/01/how-to-debug-triggers-using-trigger.html
// No license specified - this code is trimmed out from Release build anyway so it should be ok using it this way

// HOWTO: add the following attached property to any trigger and you will see when it is activated/deactivated in the output window
//        TriggerTracing.TriggerName="your debug name"
//        TriggerTracing.TraceEnabled="True"

namespace Stride.Core.Presentation.Diagnostics
{
    /// <summary>
    /// Contains attached properties to activate Trigger Tracing on the specified Triggers.
    /// This file alone should be dropped into your app.
    /// </summary>
    public static class TriggerTracing
    {
        static TriggerTracing()
        {
            // Initialise WPF Animation tracing and add a TriggerTraceListener
            PresentationTraceSources.Refresh();
            PresentationTraceSources.AnimationSource.Listeners.Clear();
            PresentationTraceSources.AnimationSource.Listeners.Add(new TriggerTraceListener());
            PresentationTraceSources.AnimationSource.Switch.Level = SourceLevels.All;
        }
        
        #region TriggerName attached property

        /// <summary>
        /// Gets the trigger name for the specified trigger. This will be used
        /// to identify the trigger in the debug output.
        /// </summary>
        /// <param name="trigger">The trigger.</param>
        /// <returns></returns>
        public static string GetTriggerName([NotNull] TriggerBase trigger)
        {
            return (string)trigger.GetValue(TriggerNameProperty);
        }

        /// <summary>
        /// Sets the trigger name for the specified trigger. This will be used
        /// to identify the trigger in the debug output.
        /// </summary>
        /// <param name="trigger">The trigger.</param>
        /// <returns></returns>
        public static void SetTriggerName([NotNull] TriggerBase trigger, string value)
        {
            trigger.SetValue(TriggerNameProperty, value);
        }

        public static readonly DependencyProperty TriggerNameProperty =
            DependencyProperty.RegisterAttached(
            "TriggerName",
            typeof(string),
            typeof(TriggerTracing),
            new UIPropertyMetadata(string.Empty));

        #endregion

        #region TraceEnabled attached property

        /// <summary>
        /// Gets a value indication whether trace is enabled for the specified trigger.
        /// </summary>
        /// <param name="trigger">The trigger.</param>
        /// <returns></returns>
        public static bool GetTraceEnabled([NotNull] TriggerBase trigger)
        {
            return (bool)trigger.GetValue(TraceEnabledProperty);
        }

        /// <summary>
        /// Sets a value specifying whether trace is enabled for the specified trigger
        /// </summary>
        /// <param name="trigger"></param>
        /// <param name="value"></param>
        public static void SetTraceEnabled([NotNull] TriggerBase trigger, bool value)
        {
            trigger.SetValue(TraceEnabledProperty, value);
        }

        public static readonly DependencyProperty TraceEnabledProperty =
            DependencyProperty.RegisterAttached(
            "TraceEnabled",
            typeof(bool),
            typeof(TriggerTracing),
            new UIPropertyMetadata(BooleanBoxes.FalseBox, OnTraceEnabledChanged));

        private static void OnTraceEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var triggerBase = d as TriggerBase;

            if (triggerBase == null)
                return;

            if (!(e.NewValue is bool))
                return;

            if ((bool)e.NewValue)
            {
                // insert dummy story-boards which can later be traced using WPF animation tracing
                
                var storyboard = new TriggerTraceStoryboard(triggerBase, TriggerTraceStoryboardType.Enter);
                triggerBase.EnterActions.Insert(0, new BeginStoryboard() { Storyboard = storyboard });

                storyboard = new TriggerTraceStoryboard(triggerBase, TriggerTraceStoryboardType.Exit);
                triggerBase.ExitActions.Insert(0, new BeginStoryboard() { Storyboard = storyboard });
            }
            else
            {
                // remove the dummy storyboards
                
                foreach (var actionCollection in new[] { triggerBase.EnterActions, triggerBase.ExitActions })
                {
                    foreach (var triggerAction in actionCollection)
                    {
                        var bsb = triggerAction as BeginStoryboard;

                        if (bsb?.Storyboard is TriggerTraceStoryboard)
                        {
                            actionCollection.Remove(bsb);
                            break;
                        }
                    }
                }
            }
        }

        #endregion

        private enum TriggerTraceStoryboardType
        {
            Enter, Exit
        }

        /// <summary>
        /// A dummy storyboard for tracing purposes
        /// </summary>
        private class TriggerTraceStoryboard : Storyboard
        {
            public TriggerTraceStoryboardType StoryboardType { get; }
            public TriggerBase TriggerBase { get; }

            public TriggerTraceStoryboard(TriggerBase triggerBase, TriggerTraceStoryboardType storyboardType)
            {
                TriggerBase = triggerBase;
                StoryboardType = storyboardType;
            }
        }

        /// <summary>
        /// A custom tracelistener.
        /// </summary>
        private class TriggerTraceListener : TraceListener
        {
            public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
            {
                base.TraceEvent(eventCache, source, eventType, id, format, args);

                if (format.StartsWith("Storyboard has begun;"))
                {
                    var storyboard = args[1] as TriggerTraceStoryboard;
                    if (storyboard != null)
                    {
                        // add a breakpoint here to see when your trigger has been
                        // entered or exited
                        
                        // the element being acted upon
                        var targetElement = args[5];
                        
                        // the namescope of the element being acted upon
                        var namescope = (INameScope)args[7];
                        
                        var triggerBase = storyboard.TriggerBase;
                        var triggerName = GetTriggerName(storyboard.TriggerBase);

                        Debug.WriteLine($"Element: {targetElement}, {triggerBase.GetType().Name}: {triggerName}: {storyboard.StoryboardType}");
                    }
                }
            }

            public override void Write(string message)
            {
            }

            public override void WriteLine(string message)
            {
            }
        }
    }

}

#endif
