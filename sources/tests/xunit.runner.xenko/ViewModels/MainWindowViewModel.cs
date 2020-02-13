using System;
using System.Text;

namespace xunit.runner.xenko.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {

        }

        public TestsViewModel Tests { get; } = new TestsViewModel();
    }
}
