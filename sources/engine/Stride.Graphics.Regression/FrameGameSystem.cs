// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using Stride.Core;
using Stride.Games;

namespace Stride.Graphics.Regression;

/// <summary>
///   Game system that schedules and executes methods and tests at specific frames,
///   and supports taking screenshots during the rendering process.
/// </summary>
/// <remarks>
///   The <see cref="FrameGameSystem"/> class is designed to facilitate frame-based testing workflows.
///   <para>
///     For this, it provides functionality for scheduling methods to be executed during the
///     <see cref="Update(GameTime)"/> or <see cref="Draw(GameTime)"/> methods at specific frames.
///   </para>
///   <para>
///     It also supports scheduling screenshots to be taken at specific frames, with optional test names.
///   </para>
/// </remarks>
/// <param name="registry">
///   The service registry where the game system will be registered, and from which it can retrieve
///   other services it depènds on.
/// </param>
public class FrameGameSystem(IServiceRegistry registry) : GameSystemBase(registry)
{
    private readonly List<TestMethodInfo> updateMethods = [];  // Methods to call in the Update method
    private readonly List<TestMethodInfo> drawMethods = [];    // Methods to call in the Draw method

    // A dictionary of (frameIndex, testName) at which it is scheduled to take a screenshot
    private readonly Dictionary<int, string?> screenshotFrames = [];

    /// <summary>
    ///   The last frame at which a test or screenshot is scheduled.
    /// </summary>
    private int lastFrame = -1;


    /// <summary>
    ///   Gets a value indicating if all the scheduled tests have been completed.
    /// </summary>
    public bool AllTestsCompleted => CurrentFrame > lastFrame;

    /// <summary>
    ///   Gets the current frame number.
    /// </summary>
    /// <value>The current frame number, which is auto-incremented when a new frame is rendered.</value>
    public int CurrentFrame { get; private set; }

    /// <summary>
    ///   Gets the number of test frames to render.
    /// </summary>
    public int TestCount => screenshotFrames.Count;

    /// <summary>
    ///   Gets or sets a value indicating whether a unit test is feeding the actions to
    ///   be executed or the screenshots to be captured, instead of a custom Game instance
    ///   or the user.
    /// </summary>
    public bool IsUnitTestFeeding { get; set; }


    /// <summary>
    ///   Adds a method to call in the <see cref="Update(GameTime)"/> method at a specified frame.
    /// </summary>
    /// <param name="frameIndex">The index of the frame at which to execute the <paramref name="method"/>.</param>
    /// <param name="method">The method to call.</param>
    /// <returns>
    ///   This <see cref="FrameGameSystem"/> instance, allowing for method chaining.
    /// </returns>
    public FrameGameSystem Update(int frameIndex, Action method)
    {
        AddTestMethods(method, frameIndex, updateMethods);
        return this;
    }

    /// <summary>
    ///   Adds a method to call in the <see cref="Update(GameTime)"/> method at the next frame.
    /// </summary>
    /// <param name="method">The method to call.</param>
    /// <returns>
    ///   This <see cref="FrameGameSystem"/> instance, allowing for method chaining.
    /// </returns>
    public FrameGameSystem Update(Action method)
    {
        AddTestMethods(method, lastFrame + 1, updateMethods);
        return this;
    }

    /// <summary>
    ///   Adds a method to call in the <see cref="Draw(GameTime)"/> method at a specified frame.
    /// </summary>
    /// <param name="frameIndex">The index of the frame at which to execute the <paramref name="method"/>.</param>
    /// <param name="method">The method to call.</param>
    /// <returns>
    ///   This <see cref="FrameGameSystem"/> instance, allowing for method chaining.
    /// </returns>
    public FrameGameSystem Draw(int frameIndex, Action method)
    {
        AddTestMethods(method, frameIndex, drawMethods);
        return this;
    }

    /// <summary>
    ///   Adds a method to call in the <see cref="Draw(GameTime)"/> method at the next frame.
    /// </summary>
    /// <param name="method">The method to call.</param>
    /// <returns>
    ///   This <see cref="FrameGameSystem"/> instance, allowing for method chaining.
    /// </returns>
    public FrameGameSystem Draw(Action method)
    {
        AddTestMethods(method, lastFrame + 1, drawMethods);
        return this;
    }

    /// <summary>
    ///   Adds a method to be executed later to the specified list.
    /// </summary>
    /// <param name="method">The method to add.</param>
    /// <param name="frameIndex">The index of the frame at which to execute the <paramref name="method"/>.</param>
    /// <param name="targetList">The list where to add the <paramref name="method"/>.</param>
    private static void AddTestMethods(Action method, int frameIndex, List<TestMethodInfo> targetList)
    {
        if (method is null)
            return;

        var methodInfo = new TestMethodInfo(method, frameIndex);
        targetList.Add(methodInfo);
    }


    /// <summary>
    ///   Sets up a screenshot to be taken at a specific frame.
    /// </summary>
    /// <param name="frameIndex">
    ///   The index of the frame at which to take a screenshot.
    ///   Specifying <see langword="null"/> will schedule the screenshot for the next frame.
    /// </param>
    /// <param name="testName">
    ///   An optional name for the test that needs to take a screenshot.
    ///   Can be <see langword="null"/>
    /// </param>
    /// <returns>
    ///   This <see cref="FrameGameSystem"/> instance, allowing for method chaining.
    /// </returns>
    public FrameGameSystem TakeScreenshot(int? frameIndex = null, string? testName = null)
    {
        var realFrameIndex = frameIndex ?? lastFrame + 1;

        screenshotFrames.Add(realFrameIndex, testName);
        if (realFrameIndex > lastFrame)
            lastFrame = realFrameIndex;

        return this;
    }

    /// <summary>
    ///   Determines if a screenshot is scheduled to be taken this frame.
    /// </summary>
    /// <param name="testName">
    ///   When the method returns, contains the name of the test that needs a screenshot,
    ///   or <see langword="null"/> if no name was specified.
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if a screenshot is scheduled for the current frame; otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsScreenshotNeeded(out string? testName)
    {
        return screenshotFrames.TryGetValue(CurrentFrame, out testName);
    }


    /// <inheritdoc/>
    public override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
        ExecuteFrameMethod(drawMethods);
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        // Update is called twice before the first Draw
        CurrentFrame = gameTime.FrameCount - 1;
        base.Update(gameTime);
        ExecuteFrameMethod(updateMethods);
    }


    /// <summary>
    ///   Executes the test methods scheduled for the current frame.
    /// </summary>
    /// <param name="targetList">The list of methods to run.</param>
    private void ExecuteFrameMethod(List<TestMethodInfo> targetList)
    {
        var methodsToRemove = new Stack<int>();

        for (var i = 0; i < targetList.Count; ++i)
        {
            var method = targetList[i];
            if (method.FrameIndex == CurrentFrame)
            {
                Debug.Assert(method.Method is not null);

                GameTestBase.TestGameLogger.Debug(@"Executing method in Draw/Update for frame " + CurrentFrame + @": " + method.Method.GetMethodInfo().Name);
                method.Method.Invoke();

                // Method executed, we can remove it from the list
                methodsToRemove.Push(i);
            }
        }

        // Remove the called methods so that they are only executed for just one frame
        while (methodsToRemove.Count > 0)
            targetList.RemoveAt(methodsToRemove.Pop());
    }

    #region Helper structures

    /// <summary>
    ///   Stores the information of a method to be called before any test.
    /// </summary>
    /// <param name="Method">The method that should be called.</param>
    /// <param name="FrameIndex">The index of the frame at which to execute the <paramref name="Method"/>.</param>
    private record struct TestMethodInfo(Action Method, int FrameIndex);

    #endregion
}
