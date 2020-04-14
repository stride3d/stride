using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Xunit;
using Xunit.Abstractions;

namespace xunit.runner.stride.ViewModels
{
    public class TestCaseViewModel : TestNodeViewModel
    {
        private readonly TestsViewModel tests;

        public ITestCase TestCase { get; }

        public TestCaseViewModel(TestsViewModel tests, ITestCase testCase)
        {
            this.tests = tests;
            TestCase = testCase;
        }

        public void RunTest()
        {
            tests.RunTests(this);
        }

        public override IEnumerable<TestCaseViewModel> EnumerateTestCases()
        {
            yield return this;
        }

        public override TestCaseViewModel LocateTestCase(ITestCase testCase)
        {
            return (testCase == this.TestCase) ? this : null;
        }

        public override string DisplayName => TestCase.DisplayName;
    }
}
