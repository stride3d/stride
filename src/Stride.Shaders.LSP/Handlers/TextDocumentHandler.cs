using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Progress;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.WorkDone;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using Serilog;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Stride.Shaders.Parsing.LSP;

internal class TextDocumentHandler : TextDocumentSyncHandlerBase
{
    private readonly ILogger<TextDocumentHandler> _logger;
    private readonly ILanguageServerConfiguration _configuration;

    private readonly TextDocumentSelector _textDocumentSelector = new(
        new TextDocumentFilter
        {
            Pattern = "**/*.sdsl"
        }
    );

    public TextDocumentHandler(ILogger<TextDocumentHandler> logger, Foo foo, ILanguageServerConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        foo.SayFoo();
    }

    public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;

    public override Task<Unit> Handle(DidChangeTextDocumentParams notification, CancellationToken token)
    {
        // _logger.LogCritical("Critical");
        // _logger.LogDebug("Debug");
        // _logger.LogTrace("Trace");
        // _logger.LogInformation("Hello world!");
        return Unit.Task;
    }

    public override async Task<Unit> Handle(DidOpenTextDocumentParams notification, CancellationToken token)
    {
        await Task.Yield();
        _logger.LogInformation("Hello world!");
        await _configuration.GetScopedConfiguration(notification.TextDocument.Uri, token).ConfigureAwait(false);
        return Unit.Value;
    }

    public override Task<Unit> Handle(DidCloseTextDocumentParams notification, CancellationToken token)
    {
        if (_configuration.TryGetScopedConfiguration(notification.TextDocument.Uri, out var disposable))
        {
            disposable.Dispose();
        }

        return Unit.Task;
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams notification, CancellationToken token) => Unit.Task;

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities) => new TextDocumentSyncRegistrationOptions()
    {
        DocumentSelector = _textDocumentSelector,
        Change = Change,
        Save = new SaveOptions() { IncludeText = true }
    };

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) => new TextDocumentAttributes(uri, "csharp");
}

internal class MyDocumentSymbolHandler : IDocumentSymbolHandler
{
    public async Task<SymbolInformationOrDocumentSymbolContainer> Handle(
        DocumentSymbolParams request,
        CancellationToken cancellationToken
    )
    {
        // you would normally get this from a common source that is managed by current open editor, current active editor, etc.
        var content = await File.ReadAllTextAsync(DocumentUri.GetFileSystemPath(request), cancellationToken).ConfigureAwait(false);
        var lines = content.Split('\n');
        var symbols = new List<SymbolInformationOrDocumentSymbol>();

        var result = SDSLParser.Parse(content);
        if (result.AST is ShaderNamespace nsp)
        {
            Log.Information($"{nsp.NamespacePath} is being treated");
            symbols.Add(
                new DocumentSymbol()
                {
                    Kind = SymbolKind.Namespace,
                    Name = string.Join(".", nsp.NamespacePath.Select(x => x.Name)),
                    Range = new Range(nsp.Info.Line, nsp.Info.Column, nsp.Info.EndLine, nsp.Info.EndColumn)
                }
            );
        }

        // for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        // {
        //     var line = lines[lineIndex];
        //     var parts = line.Split(' ', '.', '(', ')', '{', '}', '[', ']', ';');
        //     var currentCharacter = 0;
        //     foreach (var part in parts)
        //     {
        //         if (string.IsNullOrWhiteSpace(part))
        //         {
        //             currentCharacter += part.Length + 1;
        //             continue;
        //         }

        //         symbols.Add(
        //             new DocumentSymbol
        //             {
        //                 Detail = part,
        //                 Deprecated = true,
        //                 Kind = SymbolKind.Field,
        //                 Tags = new[] { SymbolTag.Deprecated },
        //                 Range = new Range(
        //                     new Position(lineIndex, currentCharacter),
        //                     new Position(lineIndex, currentCharacter + part.Length)
        //                 ),
        //                 SelectionRange =
        //                     new Range(
        //                         new Position(lineIndex, currentCharacter),
        //                         new Position(lineIndex, currentCharacter + part.Length)
        //                     ),
        //                 Name = part
        //             }
        //         );
        //         currentCharacter += part.Length + 1;
        //     }
        // }

        // await Task.Delay(2000, cancellationToken);
        return symbols;
    }

    public DocumentSymbolRegistrationOptions GetRegistrationOptions(DocumentSymbolCapability capability, ClientCapabilities clientCapabilities) => new DocumentSymbolRegistrationOptions
    {
        DocumentSelector = TextDocumentSelector.ForLanguage("sdsl")
    };
}

internal class MyWorkspaceSymbolsHandler : IWorkspaceSymbolsHandler
{
    private readonly IServerWorkDoneManager _serverWorkDoneManager;
    private readonly IProgressManager _progressManager;
    private readonly ILogger<MyWorkspaceSymbolsHandler> _logger;

    public MyWorkspaceSymbolsHandler(IServerWorkDoneManager serverWorkDoneManager, IProgressManager progressManager, ILogger<MyWorkspaceSymbolsHandler> logger)
    {
        _serverWorkDoneManager = serverWorkDoneManager;
        _progressManager = progressManager;
        _logger = logger;
    }

    public async Task<Container<WorkspaceSymbol>> Handle(
        WorkspaceSymbolParams request,
        CancellationToken cancellationToken
    )
    {
        using var reporter = _serverWorkDoneManager.For(
            request, new WorkDoneProgressBegin
            {
                Cancellable = true,
                Message = "This might take a while...",
                Title = "Some long task....",
                Percentage = 0
            }
        );
        using var partialResults = _progressManager.For(request, cancellationToken);
        if (partialResults != null)
        {
            await Task.Delay(2000, cancellationToken).ConfigureAwait(false);

            reporter.OnNext(
                new WorkDoneProgressReport
                {
                    Cancellable = true,
                    Percentage = 20
                }
            );
            await Task.Delay(500, cancellationToken).ConfigureAwait(false);

            reporter.OnNext(
                new WorkDoneProgressReport
                {
                    Cancellable = true,
                    Percentage = 40
                }
            );
            await Task.Delay(500, cancellationToken).ConfigureAwait(false);

            reporter.OnNext(
                new WorkDoneProgressReport
                {
                    Cancellable = true,
                    Percentage = 50
                }
            );
            await Task.Delay(500, cancellationToken).ConfigureAwait(false);

            partialResults.OnNext(
                [
                        new WorkspaceSymbol {
                            ContainerName = "Partial Container",
                            Kind = SymbolKind.Constant,
                            Location = new Location {
                                Range = new Range(
                                    new Position(2, 1),
                                    new Position(2, 10)
                                )
                            },
                            Name = "Partial name"
                        }
                ]
            );

            reporter.OnNext(
                new WorkDoneProgressReport
                {
                    Cancellable = true,
                    Percentage = 70
                }
            );
            await Task.Delay(500, cancellationToken).ConfigureAwait(false);

            reporter.OnNext(
                new WorkDoneProgressReport
                {
                    Cancellable = true,
                    Percentage = 90
                }
            );

            partialResults.OnCompleted();
            return Array.Empty<WorkspaceSymbol>();
        }

        try
        {
            return new[] {
                    new WorkspaceSymbol {
                        ContainerName = "Container",
                        Kind = SymbolKind.Constant,
                        Location = new Location {
                            Range = new Range(
                                new Position(1, 1),
                                new Position(1, 10)
                            )
                        },
                        Name = "name"
                    }
                };
        }
        finally
        {
            reporter.OnNext(
                new WorkDoneProgressReport
                {
                    Cancellable = true,
                    Percentage = 100
                }
            );
        }
    }

    public WorkspaceSymbolRegistrationOptions GetRegistrationOptions(WorkspaceSymbolCapability capability, ClientCapabilities clientCapabilities) => new();
}