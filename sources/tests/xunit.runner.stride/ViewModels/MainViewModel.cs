// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace xunit.runner.stride.ViewModels;

public class MainViewModel : ViewModelBase
{
    public TestsViewModel Tests { get; } = new TestsViewModel();
}
