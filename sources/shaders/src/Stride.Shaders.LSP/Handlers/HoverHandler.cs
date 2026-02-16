using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.LSP;



public class HoverHandler(ILogger<HoverHandler> logger) : HoverHandlerBase
{

    ILogger<HoverHandler> _logger = logger;
    public override async Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        var content = MonoGamePreProcessor.Run(
            await File.ReadAllTextAsync(DocumentUri.GetFileSystemPath(request.TextDocument.Uri) ?? "", cancellationToken).ConfigureAwait(false),
            Path.GetFileName(DocumentUri.GetFileSystemPath(request.TextDocument.Uri))!
        );
        var result = SDSLParser.Parse(content);
        if (result.AST is ShaderFile sf && sf.Namespaces.Count > 0)
        {
            if (ComputeIntersection(request.Position, sf, out var description))
            {
                return new Hover
                {
                    Contents = description
                };
            }
            else return new Hover
            {
                Contents = new($"Hovering at : {request.Position}")
            };
        }
        return null;
    }

    protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
    {
        return new HoverRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("sdsl"),
            WorkDoneProgress = true
        };
    }

    bool ComputeIntersection(Position position, Node node, out MarkedStringsOrMarkupContent description)
    {
        description = null!;
        if (node is ShaderFile sf)
        {
            foreach (var ns in sf.Namespaces)
                if (ns.Intersects(position))
                    return ComputeIntersection(position, ns, out description);
            foreach (var e in sf.RootDeclarations)
                if (e.Intersects(position))
                    return ComputeIntersection(position, e, out description);
        }
        else if (node is ShaderNamespace sn)
        {
            if (sn.Namespace is not null && sn.Namespace.Intersects(position))
            {
                description = new(sn.Namespace.ToString());
                return true;
            }
            foreach (var decl in sn.Declarations)
            {
                if (decl.Intersects(position))
                    return ComputeIntersection(position, decl, out description);
            }
        }
        else if (node is ShaderClass sc)
        {
            if (sc.Name.Intersects(position))
            {
                description = new($"shader {sc.Name}");
                return true;
            }
            foreach (var parent in sc.Mixins)
                if (parent.Intersects(position))
                {
                    description = new($"mixin {parent}");
                    return true;
                }
            foreach (var e in sc.Elements)
                if (e.Intersects(position))
                    return ComputeIntersection(position, e, out description);
        }
        else if (node is ShaderMember member)
        {
            if (member.TypeName.Intersects(position))
            {
                description = new($"{member.TypeName}");
                return true;
            }
            else
            {
                description = new( new MarkedString("SDSL", $"{member.Info.Text}"));
                return true;
            }
        }
        else if (node is ShaderMethod method)
        {
            if (method.Name.Intersects(position))
            {
                description = new($"method {method.Name}");
                return true;
            }
            foreach (var arg in method.Parameters)
                if (arg.Intersects(position))
                {
                    description = new($"argument {arg.Name}");
                    return true;
                }
            if (method.Body is not null)
                foreach (var s in method.Body.Statements)
                    if (s.Intersects(position))
                        return ComputeIntersection(position, s, out description);
        }
        return false;
    }

    //static bool ComputeIntersection(Position position, ShaderFile file, out string description)
    //{
    //    description = "";
    //    foreach (var ns in file.Namespaces)
    //    {
    //        if (ns.Namespace is not null && ns.Namespace.Intersects(position))
    //        {
    //            description = ns.Namespace.ToString();
    //            return true;
    //        }
    //        else
    //        {
    //            foreach (var decl in ns.Declarations)
    //            {
    //                if (decl is ShaderClass sclass && sclass.Intersects(position))
    //                {
    //                    description = $"shader {sclass.Name}";
    //                    return true;
    //                }
    //            }
    //        }
    //    }
    //    return false;
    //}
}