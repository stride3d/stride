// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Transactions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Extensions;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Quantum;
using Stride.Core.Quantum.References;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Scripts;
using Stride.Assets.Rendering;
using Accessibility = Stride.Assets.Scripts.Accessibility;
using RoslynAccessibility = Microsoft.CodeAnalysis.Accessibility;

namespace Stride.Assets.Presentation.AssetEditors.VisualScriptEditor
{
    [AssetEditorViewModel(typeof(VisualScriptAsset), typeof(VisualScriptEditorView))]
    public partial class VisualScriptEditorViewModel : AssetEditorViewModel
    {
        private static readonly object NewMethodSymbol = new object();

        protected readonly GraphViewModelService ViewModelService;
        private readonly MemberGraphNodeBinding<string> baseTypeNodeBinding;
        private readonly IMemberNode propertiesNode;

        private readonly Lazy<BlockTemplateDescriptionCollectionViewModel> blockTemplateDescriptionCollection;

        private GraphViewModel properties;

        private IScriptSourceCodeResolver sourceResolver;
        private SemanticModel semanticModel;

        private bool symbolSearchOpen;
        private string symbolSearchText;
        private ISymbol symbolSearchValidatedItem;

        private VisualScriptMethodViewModel selectedMethod;
        private VisualScriptMethodEditorViewModel visibleMethod;
        private Task lastSwitchFunctionEditor;

        public VisualScriptEditorViewModel(IVisualScriptViewModelService visualScriptViewModelService, VisualScriptViewModel visualScript) : base(visualScript)
        {
            // Create the service needed to manage observable view models
            ViewModelService = new GraphViewModelService(Session.AssetNodeContainer);

            // Update the service provider of this view model to contains the ObservableViewModelService we created.
            ServiceProvider = new ViewModelServiceProvider(ServiceProvider, ViewModelService.Yield());

            VisualScriptViewModelService = visualScriptViewModelService;

            blockTemplateDescriptionCollection = new Lazy<BlockTemplateDescriptionCollectionViewModel>(() => new BlockTemplateDescriptionCollectionViewModel(this));

            AddNewPropertyCommand = new AnonymousCommand(ServiceProvider, AddNewProperty);
            RemoveSelectedPropertiesCommand = new AnonymousCommand(ServiceProvider, RemoveSelectedProperties);

            //ShowAddBlockDialogCommand = new AnonymousTaskCommand(ServiceProvider, ShowAddBlockDialog);

            AddNewMethodCommand = new AnonymousCommand(ServiceProvider, AddNewMethod);
            RemoveSelectedMethodCommand = new AnonymousCommand(ServiceProvider, RemoveSelectedFunction);

            var rootNode = Session.AssetNodeContainer.GetNode(visualScript.Asset);
            baseTypeNodeBinding = new MemberGraphNodeBinding<string>(rootNode[nameof(VisualScriptAsset.BaseType)], nameof(BaseType), OnPropertyChanging, OnPropertyChanged, visualScript.UndoRedoService);
            propertiesNode = rootNode[nameof(VisualScriptAsset.Properties)];
        }

        /// <inheritdoc/>
        public sealed override async Task<bool> Initialize()
        {
            sourceResolver = ServiceProvider.Get<IScriptSourceCodeResolver>();
            if (sourceResolver.LatestCompilation == null)
            {
                // Wait for initial compilation to be done before continuing initialization
                var compilationReady = new TaskCompletionSource<bool>();
                EventHandler compilationChange = (sender, args) => compilationReady.TrySetResult(true);

                sourceResolver.LatestCompilationChanged += compilationChange;
                await compilationReady.Task;
                sourceResolver.LatestCompilationChanged -= compilationChange;
            }

            properties = GraphViewModel.Create(ServiceProvider, new[] { new SinglePropertyProvider(propertiesNode.Target) });

            // Since Roslyn compilation is ready, regenerate slots
            await RegenerateSlots();

            // Regenerate slot on next compilations
            sourceResolver.LatestCompilationChanged += CompilationUpdated;

            // Listen to changes
            Session.AssetPropertiesChanged += Session_AssetPropertiesChanged;

            // Select first function
            SelectedMethod = Methods.FirstOrDefault();

            // Trigger initial compilation
            TriggerBackgroundCompilation().Forget();

            return true;
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            // Close function editor
            // TODO: Make this non-blocking (need async Destroy)
            SwitchFunctionEditor(selectedMethod, null).Wait();

            // Stop listening to changes
            Session.AssetPropertiesChanged -= Session_AssetPropertiesChanged;

            sourceResolver.LatestCompilationChanged -= CompilationUpdated;

            properties.Destroy();

            base.Destroy();
        }

        public IVisualScriptViewModelService VisualScriptViewModelService { get; }

        /// <summary>
        /// List of all methods defined in this class, corresponding to <see cref="VisualScriptAsset.Methods"/>.
        /// </summary>
        public ObservableList<VisualScriptMethodViewModel> Methods => Asset.Methods;

        public SemanticModel SemanticModel { get { return semanticModel; } set { SetValue(ref semanticModel, value); } }

        /// <summary>
        /// List of all methods that could be overriden.
        /// </summary>
        /// <remarks>
        /// First value will always be null (to add a new method).
        /// </remarks>
        public ObservableList<object> OverridableMethods { get; } = new ObservableList<object> { NewMethodSymbol };

        #region Symbol search properties
        public bool SymbolSearchOpen { get { return symbolSearchOpen; } set { SetValue(ref symbolSearchOpen, value); } }

        public string SymbolSearchText { get { return symbolSearchText; } set { SetValue(ref symbolSearchText, value); } }

        public ISymbol SymbolSearchValidatedItem { get { return symbolSearchValidatedItem; } set { SetValue(ref symbolSearchValidatedItem, value); } }

        public ObservableCollection<ISymbol> SymbolSearchValues { get; } = new ObservableCollection<ISymbol>();
        #endregion

        #region Commands
        public ICommandBase AddNewPropertyCommand { get; }

        public ICommandBase RemoveSelectedPropertiesCommand { get; }

        public ICommandBase MovePropertyCommand { get; }

        public ICommandBase AddNewMethodCommand { get; }

        public ICommandBase RemoveSelectedMethodCommand { get; }
        #endregion

        public BlockTemplateDescriptionCollectionViewModel BlockTemplateDescriptionCollection => blockTemplateDescriptionCollection.Value;

        public new VisualScriptViewModel Asset => (VisualScriptViewModel)base.Asset;

        public string BaseType { get { return baseTypeNodeBinding.Value; } set { baseTypeNodeBinding.Value = value; } }

        public NodeViewModel Properties => properties.RootNode;

        public ObservableList<object> SelectedProperties { get; } = new ObservableList<object>();

        /// <summary>
        /// The function selected by the user. It might still be initializing.
        /// </summary>
        public VisualScriptMethodViewModel SelectedMethod
        {
            get { return selectedMethod; }
            set
            {
                var previousFunction = selectedMethod;
                if (SetValue(ref selectedMethod, value))
                {
                    lastSwitchFunctionEditor = SwitchFunctionEditor(previousFunction, selectedMethod);
                }
            }
        }

        /// <summary>
        /// The function visible for editing. It follows SelectedFunction changes.
        /// </summary>
        public VisualScriptMethodEditorViewModel VisibleMethod { get { return visibleMethod; } private set { SetValue(ref visibleMethod, value); } }

        private async void CompilationUpdated(object sender, EventArgs e)
        {
            await RegenerateSlots();
        }

        private async Task RegenerateSlots()
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                // Regenerate slots
                foreach (var function in Asset.Methods)
                {
                    await function.RegenerateSlots();
                }

                UndoRedoService.SetName(transaction, "Regenerated slots (code update)");
            }
        }

        private void AddNewProperty()
        {
            var property = new Property("bool", $"Member{Properties.Children.Count}");

            Asset.AddProperty(property);

            //SelectedProperties.Clear();
            //SelectedProperties.Add(Session.AssetNodeContainer.GetNode(property));
        }

        private void RemoveSelectedProperties()
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                foreach (var property in SelectedProperties.Cast<NodeViewModel>().Select(x => (Property)x.NodeValue).ToList())
                {
                    Asset.RemoveProperty(property);
                }

                UndoRedoService.SetName(transaction, "Delete propertie(s)");
            }
        }

        private void AddNewMethod(object methodSymbolObject)
        {
            Method method;

            var methodSymbol = methodSymbolObject as IMethodSymbol;
            if (methodSymbol == null)
            {
                // New method
                method = new Method($"Method{Methods.Count}");
            }
            else
            {
                method = new Method(methodSymbol.Name);

                // Process parameters
                foreach (var parameterSymbol in methodSymbol.Parameters)
                {
                    // Ignore this parameter
                    if (parameterSymbol.IsThis)
                        continue;

                    var parameter = new Parameter
                    {
                        Name = parameterSymbol.Name,
                        // TODO: Properly qualify types
                        Type = parameterSymbol.Type.ToDisplayString(),
                    };

                    // Generate default name if empty
                    if (string.IsNullOrEmpty(parameter.Name))
                        parameter.Name = $"param{parameterSymbol.Ordinal + 1}";

                    switch (parameterSymbol.RefKind)
                    {
                        case RefKind.None:
                            break;
                        case RefKind.Out:
                            parameter.RefKind = ParameterRefKind.Out;
                            break;
                        case RefKind.Ref:
                            parameter.RefKind = ParameterRefKind.Ref;
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    method.Parameters.Add(parameter);
                }

                // Keep accessibility
                switch (methodSymbol.DeclaredAccessibility)
                {
                    case RoslynAccessibility.Protected:
                        method.Accessibility = Scripts.Accessibility.Protected;
                        break;
                    case RoslynAccessibility.Internal:
                        method.Accessibility = Scripts.Accessibility.Internal;
                        break;
                    case RoslynAccessibility.ProtectedOrInternal:
                        method.Accessibility = Scripts.Accessibility.ProtectedOrInternal;
                        break;
                    case RoslynAccessibility.Public:
                        method.Accessibility = Scripts.Accessibility.Public;
                        break;
                    default:
                        // Default to protected if we are not sure
                        method.Accessibility = Scripts.Accessibility.Protected;
                        break;
                }

                // If it's an interface method, make it virtual, otherwise it's an override
                method.VirtualModifier = methodSymbol.ContainingType.TypeKind == TypeKind.Interface
                    ? VirtualModifier.Virtual
                    : VirtualModifier.Override;

                // TODO: Properly qualify types
                method.ReturnType = methodSymbol.ReturnType.ToDisplayString();
            }

            Asset.AddMethod(method);
        }

        private void RemoveSelectedFunction()
        {
            if (SelectedMethod == null)
                return;

            using (var transaction = UndoRedoService.CreateTransaction())
            {
                UndoRedoService.SetName(transaction, $"Delete function {SelectedMethod.Method.Name}");
                Asset.RemoveMethod(SelectedMethod.Method);
            }
        }

        private async Task SwitchFunctionEditor(VisualScriptMethodViewModel previousMethod, VisualScriptMethodViewModel newMethod)
        {
            // Make sure the previous switch is finished
            if (lastSwitchFunctionEditor != null)
                await lastSwitchFunctionEditor;

            if (previousMethod != null)
            {
                previousMethod.Destroy();

                // Unbind old function
                VisibleMethod = null;
            }

            if (newMethod != null)
            {
                var functionEditor = new VisualScriptMethodEditorViewModel(this, newMethod);

                await functionEditor.Initialize();

                // Bind new function
                VisibleMethod = functionEditor;
            }
        }

        #region Symbol Search

        public async Task<ISymbol> AskUserForSymbol()
        {
            ISymbol result = null;

            using (new SymbolSearchHelper(this))
            {
                // Wait for user to close the symbol search popup
                var userClosed = new TaskCompletionSource<bool>();
                PropertyChangedEventHandler propertyChanged = (sender, e) =>
                {
                    if (e.PropertyName == nameof(SymbolSearchOpen) && SymbolSearchOpen == false)
                        userClosed.TrySetResult(true);
                };

                PropertyChanged += propertyChanged;
                await userClosed.Task;
                PropertyChanged -= propertyChanged;

                if (SymbolSearchValidatedItem != null)
                {
                    // User selected a symbol, let's return it
                    result = symbolSearchValidatedItem;
                    symbolSearchValidatedItem = null;
                }
            }

            return result;
        }

        class SymbolSearchHelper : IDisposable
        {
            private const int MaxResults = 100;
            private readonly VisualScriptEditorViewModel viewModel;
            private readonly IScriptSourceCodeResolver sourceResolver;
            private CancellationTokenSource symbolSearchCancellationToken;

            public SymbolSearchHelper(VisualScriptEditorViewModel viewModel)
            {
                this.viewModel = viewModel;

                viewModel.PropertyChanged += ViewModel_PropertyChanged;
                viewModel.SymbolSearchValues.Clear();
                viewModel.SymbolSearchOpen = true;

                sourceResolver = viewModel.ServiceProvider.Get<IScriptSourceCodeResolver>();

                // Start initial search
                symbolSearchCancellationToken = new CancellationTokenSource();
                Task.Run(() => GenerateSymbolSearchValues(sourceResolver.LatestCompilation, symbolSearchCancellationToken.Token)).Forget();

                // Listen for changes
                sourceResolver.LatestCompilationChanged += RestartSearch;
            }

            private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(SymbolSearchText))
                {
                    RestartSearch(this, EventArgs.Empty);
                }
            }

            private void RestartSearch(object sender, EventArgs e)
            {
                // Cancel previous search (if any)
                symbolSearchCancellationToken?.Cancel();

                // Start new search
                symbolSearchCancellationToken = new CancellationTokenSource();
                viewModel.Dispatcher.Invoke(() => viewModel.SymbolSearchValues.Clear());
                Task.Run(() => GenerateSymbolSearchValues(sourceResolver.LatestCompilation, symbolSearchCancellationToken.Token));
            }

            public void Dispose()
            {
                sourceResolver.LatestCompilationChanged -= RestartSearch;
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;

                // Cancel search
                symbolSearchCancellationToken.Cancel();
                symbolSearchCancellationToken = null;

                viewModel.SymbolSearchText = null;
                viewModel.SymbolSearchValues.Clear();
                viewModel.SymbolSearchOpen = false;
            }

            private void GenerateSymbolSearchValues(Compilation latestCompilation, CancellationToken cancellationToken)
            {
                if (latestCompilation != null)
                {
                    int count = 0;

                    // Go through namespace to find module ones, and sort by priority: current assembly, Stride, others, System
                    foreach (var assembly in GetAssemblies(latestCompilation, cancellationToken)
                        .OrderBy(assembly =>
                        {
                            if (assembly == latestCompilation.Assembly)
                                return 0;

                            if (assembly.Name.Contains("Stride"))
                                return 1;

                            if (assembly.Name == "mscorlib" || assembly.Name.StartsWith("System"))
                                return 3;

                            return 2;
                        }))
                    {
                        var methods = new List<IMethodSymbol>();

                        // List types that are either public or part of current assembly
                        foreach (var type in GetAllTypes(assembly, cancellationToken)
                            .Where(type => type.DeclaredAccessibility != RoslynAccessibility.Private || type.ContainingAssembly == latestCompilation.Assembly))
                        {
                            // List methods
                            foreach (var method in type.GetMembers().OfType<IMethodSymbol>()
                                .Where(member => member.DeclaredAccessibility != RoslynAccessibility.Private || type.ContainingAssembly == latestCompilation.Assembly))
                            {
                                // Ignore .ctor, property getter/setter, events, etc...
                                if (method.MethodKind != MethodKind.Ordinary
                                    && method.MethodKind != MethodKind.UserDefinedOperator
                                    && method.MethodKind != MethodKind.BuiltinOperator
                                    && method.MethodKind != MethodKind.Conversion)
                                    continue;

                                // Filter text
                                var methodString = method.ToDisplayString();
                                if (!string.IsNullOrEmpty(viewModel.SymbolSearchText) && !viewModel.SymbolSearchText.MatchCamelCase(methodString))
                                    continue;

                                // Early exit once we have enough entries
                                if (count++ > MaxResults)
                                    break;

                                methods.Add(method);
                            }

                            if (count > MaxResults)
                                break;
                        }

                        // Add all methods from a single assembly at once to reduce dispatch count
                        if (methods.Count > 0)
                        {
                            viewModel.Dispatcher.Invoke(() =>
                            {
                                // If we have been cancelled, let's early exit
                                if (cancellationToken.IsCancellationRequested)
                                    return;

                                foreach (var method in methods)
                                    viewModel.SymbolSearchValues.Add(method);
                            });
                        }
                    }
                }
            }

            private static IEnumerable<IAssemblySymbol> GetAssemblies(
                Compilation compilation,
                CancellationToken cancellationToken)
            {
                var stack = new Stack<IAssemblySymbol>();
                var processedAssemblies = new HashSet<IAssemblySymbol>();

                processedAssemblies.Add(compilation.Assembly);
                stack.Push(compilation.Assembly);

                while (stack.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var current = stack.Pop();

                    yield return current;

                    foreach (var module in current.Modules)
                    {
                        foreach (var referencedAssembly in module.ReferencedAssemblySymbols)
                        {
                            if (processedAssemblies.Add(referencedAssembly))
                            {
                                stack.Push(referencedAssembly);
                            }
                        }
                    }
                }
            }

            private static IEnumerable<INamedTypeSymbol> GetAllTypes(
                IAssemblySymbol assemblySymbol,
                CancellationToken cancellationToken)
            {
                var stack = new Stack<INamespaceOrTypeSymbol>();
                stack.Push(assemblySymbol.GlobalNamespace);

                while (stack.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var current = stack.Pop();
                    var currentNs = current as INamespaceSymbol;
                    if (currentNs != null)
                    {
                        foreach (var member in currentNs.GetMembers())
                            stack.Push(member);
                    }
                    else
                    {
                        var namedType = (INamedTypeSymbol)current;
                        foreach (var member in namedType.GetTypeMembers())
                            stack.Push(member);
                        yield return namedType;
                    }
                }
            }
        }

        #endregion
    }
}
