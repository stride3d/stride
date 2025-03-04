// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Avalonia.Threading;
using Xunit;

namespace xunit.runner.stride.ViewModels;

public class TestsViewModel : ViewModelBase
{
    private XunitFrontController Controller { get; }

    public TestsViewModel()
    {
        var assemblyFileName = Assembly.GetEntryAssembly()!.Location;

        // TODO: currently we disable app domain otherwise GameTestBase.ForceInteractiveMode is not kept
        //       we should find another way to transfer this parameter
        Controller = new XunitFrontController(AppDomainSupport.Denied, assemblyFileName);
        var sink = new TestDiscoverySink();
        Controller.Find(true, sink, TestFrameworkOptions.ForDiscovery());
        sink.Finished.WaitOne();

        var testAssemblyViewModel = new TestGroupViewModel(this, sink.TestCases.FirstOrDefault()?.TestMethod.TestClass.TestCollection.TestAssembly.Assembly.Name ?? "No tests were found");
        foreach (var testClass in sink.TestCases.GroupBy(x => x.TestMethod.TestClass))
        {
            var testClassViewModel = new TestGroupViewModel(this, testClass.Key.Class.Name);
            testAssemblyViewModel.Children.Add(testClassViewModel);
            foreach (var testCase in testClass)
            {
                testClassViewModel.Children.Add(new TestCaseViewModel(this, testCase));
            }
        }
        TestCases.Add(testAssemblyViewModel);
    }

    public async void RunTests(TestNodeViewModel testNodeViewModel)
    {
        var testCases = testNodeViewModel.EnumerateTestCases();
        var testCaseViewModels = new Dictionary<string, TestCaseViewModel>();
        foreach (var testCase in testCases)
        {
            testCaseViewModels.Add(testCase.TestCase.UniqueID, testCase);
        }

        int testCasesFinished = 0;
        await Task.Run(() =>
        {
            // Reset progress
            Dispatcher.UIThread.Post(() =>
            {
                TestCompletion = 0.0;
                RunningTests = true;
            });

            var sink = new XSink
            {
                HandleTestCaseStarting = args =>
                {
                    if (testCaseViewModels.TryGetValue(args.Message.TestCase.UniqueID, out var testCaseViewModel))
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            // Test status
                            testCaseViewModel.Running = true;
                        });
                    }
                },
                HandleTestCaseFinished = args =>
                {
                    if (testCaseViewModels.TryGetValue(args.Message.TestCase.UniqueID, out var testCaseViewModel))
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            // Test status
                            testCaseViewModel.Failed = args.Message.TestsFailed > 0;
                            testCaseViewModel.Succeeded = args.Message.TestsFailed == 0;
                            testCaseViewModel.Running = false;
                            // Update progress
                            TestCompletion = (double)Interlocked.Increment(ref testCasesFinished) / (double)testCaseViewModels.Count * 100.0;
                        });
                    }
                },
            };
            Controller.RunTests(testCaseViewModels.Select(x => x.Value.TestCase).ToArray(), sink, TestFrameworkOptions.ForExecution());
            sink.Finished.WaitOne();

            Dispatcher.UIThread.Post(() => RunningTests = false);
        });
    }

    double testCompletion;
    public double TestCompletion
    {
        get => testCompletion;
        set => SetProperty(ref testCompletion, value);
    }

    bool runningTests;
    public bool RunningTests
    {
        get => runningTests;
        set => SetProperty(ref runningTests, value);
    }

    bool isInteractiveMode = false;
    public bool IsInteractiveMode
    {
        get => isInteractiveMode;
        set
        {
            SetProperty(ref isInteractiveMode, value);
            SetInteractiveMode?.Invoke(isInteractiveMode);
        }
    }

    public List<TestNodeViewModel> TestCases { get; } = [];
    public Action<bool>? SetInteractiveMode { get; set; }
}
