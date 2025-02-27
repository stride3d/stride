using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;


namespace Stride.Shaders.Parsing.LSP;

internal class FoldingRangeHandler : IFoldingRangeHandler
{
    public FoldingRangeRegistrationOptions GetRegistrationOptions() =>
        new FoldingRangeRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("sdsl")
        };

    public Task<Container<FoldingRange>?> Handle(
        FoldingRangeRequestParam request,
        CancellationToken cancellationToken
    )
    {
        var result = SDSLParser.Parse(File.ReadAllText(request.TextDocument.Uri.GetFileSystemPath()));
        if (result.AST is ShaderFile sf && sf.Namespaces.Count > 0)
        {
            var ns = sf.Namespaces.First();
            return Task.FromResult<Container<FoldingRange>?>(
                new Container<FoldingRange>(
                    new FoldingRange
                    {
                        StartLine = ns.Info.Line,
                        EndLine = ns.Info.EndLine,
                        StartCharacter = ns.Info.Column,
                        EndCharacter = ns.Info.EndColumn,
                        Kind = FoldingRangeKind.Region
                    }
                )
            );
        }

        return Task.FromResult<Container<FoldingRange>?>(null);
    }

    public FoldingRangeRegistrationOptions GetRegistrationOptions(FoldingRangeCapability capability, ClientCapabilities clientCapabilities) => new FoldingRangeRegistrationOptions
    {
        DocumentSelector = TextDocumentSelector.ForLanguage("sdsl")
    };
}