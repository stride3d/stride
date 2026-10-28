// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Common.Tests.Helpers;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;

namespace Stride.Core.Assets.Editor.Common.Tests;

public class TestFlagEnumCommands
{
    [Flags]
    public enum Options
    {
        None = 0,
        A = 1,
        B = 2,
        C = 4,
    }

    public enum Plain
    {
        First,
        Second,
    }

    [DataContract]
    public class Container
    {
        public Options Flags { get; set; }

        public Plain Simple { get; set; }

        public int Number { get; set; }
    }

    [Fact]
    public void AttachesOnlyToFlagEnums()
    {
        var root = new PresenterTestContext().CreatePresenter(new Container());
        var command = new FlagEnumSelectAllCommand();

        Assert.True(command.CanAttach(PresenterTestContext.Child(root, nameof(Container.Flags))));
        Assert.False(command.CanAttach(PresenterTestContext.Child(root, nameof(Container.Simple))));
        Assert.False(command.CanAttach(PresenterTestContext.Child(root, nameof(Container.Number))));
    }

    [Theory]
    [InlineData(Options.None, Options.A | Options.B | Options.C)]
    [InlineData(Options.A | Options.C, Options.A | Options.B | Options.C)]
    public async Task SelectAllSetsEveryFlag(Options initial, Options expected)
    {
        Assert.Equal(expected, await Execute(new FlagEnumSelectAllCommand(), initial));
    }

    [Theory]
    [InlineData(Options.A | Options.B | Options.C)]
    [InlineData(Options.B)]
    public async Task SelectNoneClearsEveryFlag(Options initial)
    {
        Assert.Equal(Options.None, await Execute(new FlagEnumSelectNoneCommand(), initial));
    }

    [Theory]
    [InlineData(Options.A, Options.B | Options.C)]
    [InlineData(Options.A | Options.B | Options.C, Options.None)]
    [InlineData(Options.None, Options.A | Options.B | Options.C)]
    public async Task SelectInvertTogglesEveryFlag(Options initial, Options expected)
    {
        Assert.Equal(expected, await Execute(new FlagEnumSelectInvertCommand(), initial));
    }

    private static async Task<Options> Execute(FlagEnumSelectionCommandBase command, Options initial)
    {
        var container = new Container { Flags = initial };
        var root = new PresenterTestContext().CreatePresenter(container);
        await command.Execute(PresenterTestContext.Child(root, nameof(Container.Flags)), null, null);
        return container.Flags;
    }
}
