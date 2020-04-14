// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stride.Core.Diagnostics;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Stride.Assets.Scripts
{
    public class BasicBlock
    {
        internal readonly int Index;

        public BasicBlock(int index)
        {
            Index = index;
        }

        public BasicBlock NextBlock { get; set; }

        internal LabeledStatementSyntax Label { get; set; }

        internal List<StatementSyntax> Statements = new List<StatementSyntax>();
    }

    public class VisualScriptCompilerContext
    {
        private readonly VisualScriptAsset asset;
        private readonly Method method;

        // Store execution connectivity information
        private readonly Dictionary<ExecutionBlock, List<ExecutionBlock>> executionOutputs = new Dictionary<ExecutionBlock, List<ExecutionBlock>>();
        private readonly Dictionary<ExecutionBlock, List<ExecutionBlock>> executionInputs = new Dictionary<ExecutionBlock, List<ExecutionBlock>>();

        /// <summary>
        /// Specifies if a specific ExecutionBlock is executed or not before <see cref="CurrentBlock"/>, and if yes, is it executed in all path or not.
        /// </summary>
        private readonly Dictionary<ExecutionBlock, ExecutionBlockLinkState> connectivityToCurrentBlock = new Dictionary<ExecutionBlock, ExecutionBlockLinkState>();

        // If a specific output Slot was stored in a local variable, this will store its name
        private readonly Dictionary<Slot, string> outputSlotLocals = new Dictionary<Slot, string>();

        private int labelCount;
        private int localCount;
        private ExecutionBlock functionStartBlock;

        internal Queue<Tuple<BasicBlock, ExecutionBlock>> CodeToGenerate = new Queue<Tuple<BasicBlock, ExecutionBlock>>();

        public Logger Log { get; }

        public Dictionary<Block, BasicBlock> BlockMapping { get; } = new Dictionary<Block, BasicBlock>();

        public List<BasicBlock> Blocks { get; private set; } = new List<BasicBlock>();

        public BasicBlock CurrentBasicBlock { get; internal set; }

        public ExecutionBlock CurrentBlock { get; internal set; }

        public bool IsInsideLoop { get; set; }

        internal VisualScriptCompilerContext(VisualScriptAsset asset, Method method, Logger log)
        {
            // Create first block
            this.asset = asset;
            this.method = method;
            Log = log;
        }

        public BasicBlock GetOrCreateBasicBlockFromSlot(Slot executionSlot)
        {
            if (executionSlot != null)
            {
                var nextExecutionLink = method.Links.Values.FirstOrDefault(x => x.Source == executionSlot && x.Target != null);
                if (nextExecutionLink != null)
                {
                    return GetOrCreateBasicBlock((ExecutionBlock)nextExecutionLink.Target.Owner);
                }
            }

            return null;
        }

        public IEnumerable<Link> FindOutputLinks(Slot outputSlot)
        {
            return method.Links.Values.Where(x => x.Source == outputSlot && x.Target != null);
        }

        public Link FindInputLink(Slot inputSlot)
        {
            return method.Links.Values.FirstOrDefault(x => x.Target == inputSlot && x.Source != null);
        }

        public ExpressionSyntax GenerateExpression(Slot slot)
        {
            // Automatically flow to next execution slot (if it has a null name => default behavior)
            if (slot != null)
            {
                // 1. First check if there is a link and use its expression
                var sourceLink = method.Links.Values.FirstOrDefault(x => x.Target == slot);
                if (sourceLink != null)
                {
                    ExpressionSyntax expression;

                    // Generate code
                    var sourceBlock = sourceLink.Source.Owner;

                    var sourceExecutionBlock = sourceBlock as ExecutionBlock;
                    if (sourceExecutionBlock != null)
                    {
                        // If block is execution block, it must have been executed in all path until now
                        // Note: We don't care about non execution block, since we do a full expression evaluation on them.
                        ExecutionBlockLinkState sourceExecutionState;
                        if (!connectivityToCurrentBlock.TryGetValue(sourceExecutionBlock, out sourceExecutionState))
                            sourceExecutionState = ExecutionBlockLinkState.Never;

                        switch (sourceExecutionState)
                        {
                            case ExecutionBlockLinkState.Never:
                                Log.Error($"{slot} in block {slot.Owner} uses a value from execution block {sourceBlock}, however it is never executed. Slot will take default value instead.", CallerInfo.Get());
                                sourceBlock = null;
                                break;
                            case ExecutionBlockLinkState.Sometimes:
                                Log.Error($"{slot} in block {slot.Owner} uses a value from execution block {sourceBlock}, however it is executed but not in all cases. Are you using result from a conditional branch? Slot will take default value instead.", CallerInfo.Get());
                                // Note: we still let it generate code, so that the user can also see the error in the generated source code
                                //sourceBlock = null;
                                break;
                            case ExecutionBlockLinkState.Always:
                                // We're good, this value is always defined when we reach CurrentBlock
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    // Only proceed if sourceBlock has not been nulled by previous checks
                    if (sourceBlock != null)
                    {
                        string localName;
                        if (outputSlotLocals.TryGetValue(sourceLink.Source, out localName))
                        {
                            expression = IdentifierName(localName);
                        }
                        else
                        {
                            expression = (sourceBlock as IExpressionBlock)?.GenerateExpression(this, sourceLink.Source);
                        }

                        if (expression != null)
                        {
                            // Add annotation on both source block and link (so that we can keep track of what block/link generated what source code)
                            expression = expression.WithAdditionalAnnotations(GenerateAnnotation(sourceLink.Source.Owner), GenerateAnnotation(sourceLink));

                            return expression;
                        }
                    }
                }

                // 2. If a custom value is set, use it
                if (slot.Value != null)
                {
                    return ParseExpression(slot.Value).WithAdditionalAnnotations(GenerateAnnotation(slot.Owner));
                }

                // 3. Fallback: use slot name
                if (slot.Name != null)
                    return IdentifierName(slot.Name).WithAdditionalAnnotations(GenerateAnnotation(slot.Owner));
            }

            // TODO: Issue an error
            Log.Error($"{slot} in block {slot?.Owner} could not be resolved.", CallerInfo.Get());
            return IdentifierName("unknown").WithAdditionalAnnotations(GenerateAnnotation(CurrentBlock));
        }

        public List<StatementSyntax> ProcessInnerLoop(Slot executionSlot)
        {
            if (executionSlot != null)
            {
                var nextExecutionLink = method.Links.Values.FirstOrDefault(x => x.Source == executionSlot && x.Target != null);
                if (nextExecutionLink != null)
                {
                    return ProcessInnerLoop((ExecutionBlock)nextExecutionLink.Target.Owner);
                }
            }

            return null;
        }


        public List<StatementSyntax> ProcessInnerLoop(ExecutionBlock block)
        {
            // TODO: Some analysis before running it that there is no impossible execution links between inner loop and outer code
            if (BlockMapping.ContainsKey(block))
            {
                throw new InvalidOperationException("Inner block can only be used once");
            }

            // Save states (might be necssary if processing immediatly)
            var currentBlock = CurrentBlock;
            var currentBasicBlock = CurrentBasicBlock;
            var codeToGenerate = CodeToGenerate;
            var blocks = Blocks;

            // Start with empty state for the inner block
            CodeToGenerate = new Queue<Tuple<BasicBlock, ExecutionBlock>>();
            Blocks = new List<BasicBlock>();

            // Process right now the current queue
            BasicBlock newBasicBlock;
            CreateAndEnqueueBasicBlock(block, out newBasicBlock);
            ProcessCodeToGenerate();

            var result = Blocks;

            // Restore states
            CodeToGenerate = codeToGenerate;
            CurrentBlock = currentBlock;
            CurrentBasicBlock = currentBasicBlock;
            Blocks = blocks;

            return result.SelectMany(x => x.Statements).ToList();
        }

        public BasicBlock GetOrCreateBasicBlock(ExecutionBlock block)
        {
            BasicBlock newBasicBlock;
            if (!BlockMapping.TryGetValue(block, out newBasicBlock))
            {
                CreateAndEnqueueBasicBlock(block, out newBasicBlock);
            }

            return newBasicBlock;
        }

        private void CreateAndEnqueueBasicBlock(ExecutionBlock block, out BasicBlock newBasicBlock)
        {
            newBasicBlock = new BasicBlock(Blocks.Count);
            Blocks.Add(newBasicBlock);
            BlockMapping.Add(block, newBasicBlock);

            // Enqueue it for processing
            CodeToGenerate.Enqueue(Tuple.Create(newBasicBlock, (ExecutionBlock)block));
        }

        public GotoStatementSyntax CreateGotoStatement(BasicBlock target)
        {
            return GotoStatement(SyntaxKind.GotoStatement, IdentifierName(GetOrCreateLabel(target).Identifier));
        }

        public void AddStatement(StatementSyntax statement)
        {
            // Add annotation on block (so that we can keep track of what block generated what source code)
            statement = statement.WithAdditionalAnnotations(GenerateAnnotation(CurrentBlock));

            // If there is already a label with an empty statement (still no instructions), replace its inner statement
            if (CurrentBasicBlock.Label != null && CurrentBasicBlock.Statements.Count == 1 && CurrentBasicBlock.Label.Statement is EmptyStatementSyntax)
            {
                CurrentBasicBlock.Label = CurrentBasicBlock.Label.WithStatement(statement);
                CurrentBasicBlock.Statements[0] = CurrentBasicBlock.Label;
            }
            else
            {
                CurrentBasicBlock.Statements.Add(statement);
            }
        }

        private LabeledStatementSyntax GetOrCreateLabel(BasicBlock basicBlock)
        {
            if (basicBlock.Label == null)
            {
                basicBlock.Label = LabeledStatement(Identifier($"block{labelCount++}"), basicBlock.Statements.Count > 0 ? basicBlock.Statements[0] : EmptyStatement());
                basicBlock.Statements.Insert(0, basicBlock.Label);
            }

            return basicBlock.Label;
        }

        public string GenerateLocalVariableName(string nameHint = null)
        {
            return $"{nameHint ?? "local"}{localCount++}";
        }


        public void RegisterLocalVariable(Slot slot, string localVariableName)
        {
            outputSlotLocals.Add(slot, localVariableName);
        }

        private static SyntaxAnnotation GenerateAnnotation(Block block)
        {
            return new SyntaxAnnotation("Block", block.Id.ToString());
        }

        private static SyntaxAnnotation GenerateAnnotation(Link link)
        {
            return new SyntaxAnnotation("Link", link.Id.ToString());
        }

        internal void ProcessCodeToGenerate()
        {
            // Process blocks to generate statements
            while (CodeToGenerate.Count > 0)
            {
                var codeToGenerate = CodeToGenerate.Dequeue();
                CurrentBasicBlock = codeToGenerate.Item1;
                CurrentBlock = codeToGenerate.Item2;
                var currentBlock = codeToGenerate.Item2;

                // Build list of what was executed so far
                BuildCurrentBlockConnectivityCache();

                // Generate code for current node
                currentBlock.GenerateCode(this);

                // Automatically flow to next execution slot (if it has a null name => default behavior)
                var nextExecutionSlot = currentBlock.Slots.FirstOrDefault(x => x.Kind == SlotKind.Execution && x.Direction == SlotDirection.Output && x.Flags == SlotFlags.AutoflowExecution);
                if (nextExecutionSlot != null)
                {
                    var nextExecutionLink = method.Links.Values.FirstOrDefault(x => x.Source == nextExecutionSlot && x.Target != null);
                    if (nextExecutionLink == null)
                    {
                        // Nothing connected, no need to generate a goto to an empty return
                        goto InterruptFlow;
                    }

                    var nextBasicBlock = GetOrCreateBasicBlock((ExecutionBlock)nextExecutionLink.Target.Owner);
                    CurrentBasicBlock.NextBlock = nextBasicBlock;
                }

                // Is there a next block to flow to?
                if (CurrentBasicBlock.NextBlock != null)
                {
                    var nextBlock = CurrentBasicBlock.NextBlock;

                    // Do we need a goto? (in case there is some intermediary block in between)
                    if (nextBlock.Index != CurrentBasicBlock.Index + 1)
                    {
                        AddStatement(CreateGotoStatement(nextBlock));
                    }

                    continue;
                }

            InterruptFlow:
                // If there's some unrelated code (that we shouldn't flow into) after current node,
                // let's put a return to not automatically go into it
                if (CodeToGenerate.Count > 0)
                {
                    AddStatement(IsInsideLoop ? (StatementSyntax)ContinueStatement() : ReturnStatement());
                }
            }
        }

        public void ProcessEntryBlock(ExecutionBlock functionStartBlock)
        {
            BuildGlobalConnectivityCache();

            this.functionStartBlock = functionStartBlock;

            // Force generation of start block
            GetOrCreateBasicBlock(functionStartBlock);

            // Keep processing
            ProcessCodeToGenerate();

            // Clear states
            executionInputs.Clear();
            executionOutputs.Clear();
        }

        private void BuildGlobalConnectivityCache()
        {
            // Collect execution connectivity information from links
            foreach (var link in method.Links.Values)
            {
                if (link.Source.Kind == SlotKind.Execution)
                {
                    var sourceBlock = link.Source.Owner as ExecutionBlock;
                    var targetBlock = link.Target.Owner as ExecutionBlock;

                    List<ExecutionBlock> sourceOutputs, targetInputs;

                    // Store target in source
                    if (sourceBlock != null)
                    {
                        if (!executionOutputs.TryGetValue(sourceBlock, out sourceOutputs))
                            executionOutputs.Add(sourceBlock, sourceOutputs = new List<ExecutionBlock>());
                        if (!sourceOutputs.Contains(targetBlock))
                            sourceOutputs.Add(targetBlock);
                    }

                    // Store source in target
                    if (targetBlock != null)
                    {
                        if (!executionInputs.TryGetValue(targetBlock, out targetInputs))
                            executionInputs.Add(targetBlock, targetInputs = new List<ExecutionBlock>());
                        if (!targetInputs.Contains(sourceBlock))
                            targetInputs.Add(sourceBlock);
                    }
                }
            }
        }

        private void BuildCurrentBlockConnectivityCache()
        {
            // Possible optimization: build this list incrementally by reusing previous state results?
            // this could become quite complex though
            connectivityToCurrentBlock.Clear();

            // Build list of all paths between start and current block
            var paths = new List<ImmutableStack<ExecutionBlock>>();
            FindAllPaths(paths, ImmutableStack.Create(functionStartBlock), functionStartBlock, CurrentBlock);

            // Let's check which blocks are reached, and if yes, are they reached in all paths
            var reachedBlockCounts = new Dictionary<ExecutionBlock, int>();
            foreach (var path in paths)
            {
                foreach (var block in path)
                {
                    int count;
                    if (reachedBlockCounts.TryGetValue(block, out count))
                        reachedBlockCounts[block] = count + 1;
                    else
                        reachedBlockCounts.Add(block, 1);
                }
            }

            foreach (var reachedBlockCount in reachedBlockCounts)
            {
                // Reached once per path => this is always executed before current block
                // If less than once per path => this is reached but not in all cases
                connectivityToCurrentBlock[reachedBlockCount.Key] = (reachedBlockCount.Value == paths.Count) ? ExecutionBlockLinkState.Always : ExecutionBlockLinkState.Sometimes;
            }
        }

        private void FindAllPaths(List<ImmutableStack<ExecutionBlock>> paths, ImmutableStack<ExecutionBlock> currentPath, ExecutionBlock sourceBlock, ExecutionBlock targetBlock)
        {
            List<ExecutionBlock> nextBlocks;
            if (!executionOutputs.TryGetValue(sourceBlock, out nextBlocks))
                return;

            foreach (var nextBlock in nextBlocks)
            {
                if (nextBlock == targetBlock)
                {
                    // We've reached our target, record this path
                    paths.Add(currentPath);
                }
                else if (!currentPath.Contains(nextBlock)) // avoid cycles
                {
                    // Recurse
                    FindAllPaths(paths, currentPath.Push(nextBlock), nextBlock, targetBlock);
                }
            }
        }

        enum ExecutionBlockLinkState
        {
            /// <summary>
            /// Never executed.
            /// </summary>
            Never = 0,

            /// <summary>
            /// Executed but not all the time.
            /// </summary>
            Sometimes = 1,

            /// <summary>
            /// Always executed.
            /// </summary>
            Always = 2,
        }
    }

    public class VisualScriptCompilerResult : LoggerResult
    {
        public string GeneratedSource { get; set; }
        public SyntaxTree SyntaxTree { get; set; }
    }

    public class VisualScriptCompilerOptions
    {
        public string FilePath { get; set; }

        public string DefaultNamespace { get; set; }

        public string Class { get; set; }
    }

    public class VisualScriptCompiler
    {
        public static VisualScriptCompilerResult Generate(VisualScriptAsset visualScriptAsset, VisualScriptCompilerOptions options)
        {
            var result = new VisualScriptCompilerResult();

            var members = new List<MemberDeclarationSyntax>();
            var className = options.Class;

            // Generate variables
            foreach (var variable in visualScriptAsset.Properties)
            {
                var variableType = variable.Type;
                if (variableType == null)
                {
                    result.Error($"Variable {variable.Name} has no type, using \"object\" instead.");
                    variableType = "object";
                }

                var field =
                    FieldDeclaration(
                        VariableDeclaration(
                            ParseTypeName(variableType))
                        .WithVariables(
                            SingletonSeparatedList(
                                VariableDeclarator(
                                    Identifier(variable.Name)))))
                    .WithModifiers(
                        TokenList(
                            Token(SyntaxKind.PublicKeyword)));

                members.Add(field);
            }

            // Process each function
            foreach (var method in visualScriptAsset.Methods)
            {
                var functionStartBlock = method.Blocks.Values.OfType<FunctionStartBlock>().FirstOrDefault();
                if (functionStartBlock == null)
                    continue;

                var context = new VisualScriptCompilerContext(visualScriptAsset, method, result);

                context.ProcessEntryBlock(functionStartBlock);

                var methodModifiers = new SyntaxTokenList();
                methodModifiers = ConvertAccessibility(methodModifiers, method.Accessibility);
                methodModifiers = ConvertVirtualModifier(methodModifiers, method.VirtualModifier);
                if (method.IsStatic)
                    methodModifiers = methodModifiers.Add(Token(SyntaxKind.StaticKeyword));

                var parameters = new List<SyntaxNodeOrToken>();
                foreach (var parameter in method.Parameters)
                {
                    if (parameters.Count > 0)
                        parameters.Add(Token(SyntaxKind.CommaToken));

                    parameters.Add(
                        Parameter(Identifier(parameter.Name))
                        .WithModifiers(ConvertRefKind(parameter.RefKind))
                        .WithType(ParseTypeName(parameter.Type)));
                }

                // Generate method
                var methodDeclaration =
                    MethodDeclaration(
                        method.ReturnType == "void" ? PredefinedType(Token(SyntaxKind.VoidKeyword)) : ParseTypeName(method.ReturnType),
                        Identifier(method.Name))
                    .WithModifiers(methodModifiers)
                    .WithParameterList(ParameterList(
                        SeparatedList<ParameterSyntax>(parameters)))
                    .WithBody(
                        Block(context.Blocks.SelectMany(x => x.Statements)))
                    .WithAdditionalAnnotations(GenerateAnnotation(method));

                members.Add(methodDeclaration);
            }

            // Generate class
            var classModifiers = new SyntaxTokenList();
            classModifiers = ConvertAccessibility(classModifiers, visualScriptAsset.Accessibility).Add(Token(SyntaxKind.PartialKeyword));
            if (visualScriptAsset.IsStatic)
                classModifiers = classModifiers.Add(Token(SyntaxKind.StaticKeyword));

            var @class =
                ClassDeclaration(className)
                .WithMembers(List(members))
                .WithModifiers(classModifiers);

            if (visualScriptAsset.BaseType != null)
                @class = @class.WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(IdentifierName(visualScriptAsset.BaseType)))));

            // Generate namespace around class (if any)
            MemberDeclarationSyntax namespaceOrClass = @class;
            var @namespace = !string.IsNullOrEmpty(visualScriptAsset.Namespace) ? visualScriptAsset.Namespace : options.DefaultNamespace;
            if (@namespace != null)
            {
                namespaceOrClass =
                    NamespaceDeclaration(
                        IdentifierName(@namespace))
                    .WithMembers(
                        SingletonList<MemberDeclarationSyntax>(@class));
            }

            // Generate compilation unit
            var compilationUnit =
                CompilationUnit()
                .WithUsings(
                    List(visualScriptAsset.UsingDirectives.Select(x => 
                        UsingDirective(
                            IdentifierName(x)))))
                .WithMembers(
                    SingletonList(namespaceOrClass))
                .NormalizeWhitespace();

            // Generate actual source code
            result.GeneratedSource = compilationUnit.ToFullString();
            result.SyntaxTree = SyntaxTree(compilationUnit, path: options.FilePath ?? string.Empty);

            return result;
        }

        private static SyntaxTokenList ConvertAccessibility(SyntaxTokenList tokenList, Accessibility accessibity)
        {
            switch (accessibity)
            {
                case Accessibility.Public:
                    return tokenList.Add(Token(SyntaxKind.PublicKeyword));
                case Accessibility.Private:
                    return tokenList.Add(Token(SyntaxKind.PrivateKeyword));
                case Accessibility.Protected:
                    return tokenList.Add(Token(SyntaxKind.ProtectedKeyword));
                case Accessibility.Internal:
                    return tokenList.Add(Token(SyntaxKind.InternalKeyword));
                case Accessibility.ProtectedOrInternal:
                    return tokenList.Add(Token(SyntaxKind.ProtectedKeyword)).Add(Token(SyntaxKind.InternalKeyword));
                default:
                    throw new ArgumentOutOfRangeException(nameof(accessibity), accessibity, null);
            }
        }

        private static SyntaxTokenList ConvertVirtualModifier(SyntaxTokenList tokenList, VirtualModifier virtualModifier)
        {
            switch (virtualModifier)
            {
                case VirtualModifier.None:
                    return tokenList;
                case VirtualModifier.Abstract:
                    return tokenList.Add(Token(SyntaxKind.AbstractKeyword));
                case VirtualModifier.Virtual:
                    return tokenList.Add(Token(SyntaxKind.VirtualKeyword));
                case VirtualModifier.Override:
                    return tokenList.Add(Token(SyntaxKind.OverrideKeyword));
                default:
                    throw new ArgumentOutOfRangeException(nameof(virtualModifier), virtualModifier, null);
            }
        }

        private static SyntaxTokenList ConvertRefKind(ParameterRefKind refKind)
        {
            switch (refKind)
            {
                case ParameterRefKind.None:
                    return TokenList();
                case ParameterRefKind.Ref:
                    return TokenList(Token(SyntaxKind.RefKeyword));
                case ParameterRefKind.Out:
                    return TokenList(Token(SyntaxKind.OutKeyword));
                default:
                    throw new ArgumentOutOfRangeException(nameof(refKind), refKind, null);
            }
        }

        private static SyntaxAnnotation GenerateAnnotation(Method method)
        {
            return new SyntaxAnnotation("Method", method.Id.ToString());
        }
    }
}
