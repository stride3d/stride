// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Xunit;
using Stride.Core.Assets.Editor.View.Behaviors;

namespace Stride.Core.Assets.Editor.Tests
{
    public class TestDropFeedbackResources
    {
        private const string Key = "TestDropFeedbackKey";

        [Fact]
        public void TestMissingResourceGivesTheDefault()
        {
            Assert.Equal(7.0, DropFeedbackResources.Find(null, "NoSuchResourceKey", 7.0));
        }

        [Fact]
        public void TestResourceOfTheElementWins()
        {
            RunSta(() =>
            {
                var brush = Brushes.Fuchsia;
                var element = new FrameworkElement();
                element.Resources[Key] = brush;

                Assert.Same(brush, DropFeedbackResources.Find<Brush>(element, Key, null));
            });
        }

        [Fact]
        public void TestResourceOfAnotherTypeGivesTheDefault()
        {
            RunSta(() =>
            {
                // An adorner must not throw while it draws, therefore a wrong type counts as a missing resource.
                var element = new FrameworkElement();
                element.Resources[Key] = "not a number";

                Assert.Equal(7.0, DropFeedbackResources.Find(element, Key, 7.0));
            });
        }

        /// <summary>
        /// Runs the given action on a thread in the single-threaded apartment.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <remarks>
        /// A <see cref="FrameworkElement"/> can only be made on such a thread, but the test runner does not use one.
        /// </remarks>
        private static void RunSta(Action action)
        {
            ExceptionDispatchInfo failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    failure = ExceptionDispatchInfo.Capture(exception);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            failure?.Throw();
        }
    }
}
