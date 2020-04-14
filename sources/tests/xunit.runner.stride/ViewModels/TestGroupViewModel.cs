using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Xunit;
using Xunit.Abstractions;

namespace xunit.runner.stride.ViewModels
{
    public class TestGroupViewModel : TestNodeViewModel
    {
        private readonly TestsViewModel tests;
        private readonly string displayName;

        public List<TestNodeViewModel> Children { get; } = new List<TestNodeViewModel>();

        public TestGroupViewModel(TestsViewModel tests, string displayName)
        {
            this.tests = tests;
            this.displayName = displayName;
        }

        public override IEnumerable<TestCaseViewModel> EnumerateTestCases() => Children.SelectMany(x => x.EnumerateTestCases());

        public void RunTest()
        {
            tests.RunTests(this);
        }

        public override TestCaseViewModel LocateTestCase(ITestCase testCase)
        {
            foreach (var child in Children)
            {
                var result = child.LocateTestCase(testCase);
                if (result != null)
                    return result;
            }
            return null;
        }

        public override string DisplayName => displayName;
    }
}
