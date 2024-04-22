using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Stride.Core.CompilerServices.Common;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.Analysation.Symbols;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.Analysation.Fields;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI;
using Stride.Core.CompilerServices.DataEvaluationApi.ModeInfos;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.Analysation.Analyzers;
using Stride.Core.CompilerServices.DataEvaluationApi.DataApi;
using Stride.Core.CompilerServices.Modules.ObjectDescription;
using Stride.Core.CompilerServices.DataEvaluationApi;
using System.IO.MemoryMappedFiles;
using System.Linq;

namespace Stride.Core.CompilerServices.Generators;
[Generator]
internal class ObjectDescriptorGenerator : IIncrementalGenerator
{
    private List<ClassInfo> cache = new List<ClassInfo>(100);
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Debugger.Launch();
        var assignModeInfo = new AssignModeInfo();


        var classProvider = context.SyntaxProvider
                                   .CreateSyntaxProvider((node, transform) =>
                                   {
                                       return node is ClassDeclarationSyntax or StructDeclarationSyntax &&
    ((TypeDeclarationSyntax)node).Modifiers.Any(modifier =>
        modifier.IsKind(SyntaxKind.PublicKeyword) || modifier.IsKind(SyntaxKind.InternalKeyword)); ;
                                   },
                                   (ctx, transform) =>
                                   {
                                       var classDeclaration = (TypeDeclarationSyntax)ctx.Node;
                                       var compilation = ctx.SemanticModel.Compilation;
                                       var semanticModel = ctx.SemanticModel;
                                       var info = CreateClassInfo(compilation, classDeclaration, semanticModel);
                                       return info;
                                   })
                                   .Where(x => x is not null)
                                   .Collect();

        context.RegisterSourceOutput(classProvider, Generate);
    }
    private ClassInfo CreateClassInfo(Compilation compilation, TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel)
    {
        var dataContractAttribute = WellKnownReferences.DataContractAttribute(compilation);

        if (dataContractAttribute is null)
            return null;

        var type = (ITypeSymbol)semanticModel.GetDeclaredSymbol(classDeclaration);
        if (type.ContainingType != null)
            return null;
        if (!type.HasInheritedDataContract(dataContractAttribute))
            return null;
        bool isPartial = false;
        if (classDeclaration.IsPartial())
        {
            isPartial = true;
        }
        var dataMemberIgnoreAttribute = WellKnownReferences.DataMemberIgnoreAttribute(compilation);
        var memberSelector = new MemberSelector(dataContractAttribute);
        var assignMode = new AssignModeInfo();
        var contentMode = new ContentModeInfo();
        var standardAssignAnalyzer = new PropertyAnalyzer(assignMode)
            .HasVisibleGetter()
            .HasVisibleSetter()
            .IsNotIgnored(IgnoreContext.Yaml, dataMemberIgnoreAttribute);

        var standardGetAssignAnalyzer = new PropertyAnalyzer(contentMode)
             .HasVisibleGetter()
             .WhenNot(x => x.HasVisibleSetter())
             .IsNotIgnored(IgnoreContext.Yaml, dataMemberIgnoreAttribute);
        var standardField = new FieldAnalyzer(assignMode)
             .IsVisibleToSerializer()
             .WhenNot(FieldExtensions.IsReadOnly)
             .IsNotIgnored(IgnoreContext.Yaml, dataMemberIgnoreAttribute);
        var readonlyField = new FieldAnalyzer(contentMode)
            .IsReadOnly()
            .IsVisibleToSerializer()
            .IsNotIgnored(IgnoreContext.Yaml, dataMemberIgnoreAttribute);

        var classInfoMemberProcessor = new ClassInfoMemberProcessor(memberSelector, compilation);
        classInfoMemberProcessor.PropertyAnalyzers.Add(standardAssignAnalyzer);
        classInfoMemberProcessor.PropertyAnalyzers.Add(standardGetAssignAnalyzer);
        classInfoMemberProcessor.FieldAnalyzers.Add(standardField);
        var members = classInfoMemberProcessor.Process(type);
        return ClassInfo.CreateFrom(type, members, isPartial);

    }

    private static void Generate(
      SourceProductionContext ctx,
      ImmutableArray<ClassInfo> myCustomObjects)
    {
        foreach (var obj in myCustomObjects)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            Generates(ctx, obj);
        }
    }
    private static SourceCreator SourceCreator = new SourceCreator();
    private static void Generates(SourceProductionContext ctx, ClassInfo info)
    {
        ctx.AddSource(info.GeneratorName + ".g.cs", SourceCreator.Create(ctx, info));
    }
}
