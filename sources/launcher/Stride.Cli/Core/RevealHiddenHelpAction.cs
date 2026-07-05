// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;

namespace Stride.Cli.Core;

// System.CommandLine prints nothing for a Hidden command's help even when it's explicitly requested. Reveal the
// target command just while the default help action renders it; a no-op for visible (non-Hidden) commands.
internal sealed class RevealHiddenHelpAction : SynchronousCommandLineAction
{
    private readonly HelpAction inner = new();

    public override int Invoke(ParseResult parseResult)
    {
        var command = parseResult.CommandResult.Command;
        var wasHidden = command.Hidden;
        command.Hidden = false;
        try
        {
            return inner.Invoke(parseResult);
        }
        finally
        {
            command.Hidden = wasHidden;
        }
    }
}
