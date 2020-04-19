// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

using System.Linq;
using Stride.Core.Shaders.Ast;

namespace Stride.Core.Shaders.Visitor
{
    /// <summary>
    /// A Scope declaration provides a way to retrieve all scope declaration (variable, methods...etc.) 
    /// and attached nodes.
    /// </summary>
    public class ScopeDeclaration
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ScopeDeclaration"/> class.
        /// </summary>
        public ScopeDeclaration() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScopeDeclaration"/> class.
        /// </summary>
        /// <param name="scopeContainer">The scope container.</param>
        public ScopeDeclaration(IScopeContainer scopeContainer)
        {
            ScopeContainer = scopeContainer;
            Declarations = new Dictionary<string, List<IDeclaration>>();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the scope container.
        /// </summary>
        /// <value>
        /// The scope container.
        /// </value>
        public IScopeContainer ScopeContainer { get; set; }

        /// <summary>
        /// Gets or sets the declarations.
        /// </summary>
        /// <value>
        /// The declarations.
        /// </value>
        public IDictionary<string, List<IDeclaration>> Declarations { get; private set; }

        public IEnumerable<IDeclaration> FindDeclaration(string name)
        {
            List<IDeclaration> declarationList;
            if (Declarations.TryGetValue(name, out declarationList))
                return declarationList;

            return Enumerable.Empty<IDeclaration>();
        }

        public void AddDeclaration(IDeclaration declaration)
        {
            List<IDeclaration> declarations;
            if (declaration.Name != null)
            {

                string name = declaration.Name.Text;
                if (string.IsNullOrEmpty(name))
                    return;

                if (!Declarations.TryGetValue(name, out declarations))
                {
                    declarations = new List<IDeclaration>();
                    Declarations.Add(name, declarations);
                }
            }
            else
            {
                declarations = new List<IDeclaration>();
            }

            var genericsDeclarator = declaration as IGenerics;
            if (genericsDeclarator != null)
            {
                for (int i = 0; i < genericsDeclarator.GenericParameters.Count; i++)
                {
                    var genericArgument = genericsDeclarator.GenericParameters[i];
                    var genericName = genericArgument.Name;
                    if (!Declarations.TryGetValue(genericName.Text, out declarations))
                    {
                        declarations = new List<IDeclaration>();
                        Declarations.Add(genericName, declarations);
                    }

                    declarations.Add(new GenericDeclaration(genericName, genericsDeclarator, i, false) { Span = genericArgument.Span });

                    genericName = new Identifier($"__{genericArgument.Name}_base");
                    if (!Declarations.TryGetValue(genericName.Text, out declarations))
                    {
                        declarations = new List<IDeclaration>();
                        Declarations.Add(genericName, declarations);
                    }
                    declarations.Add(new GenericDeclaration(genericName, genericsDeclarator, i, true) { Span = genericArgument.Span });
                }
            }
            
            if (!declarations.Contains(declaration))
                declarations.Add(declaration);
        }

        public void AddDeclarations(IEnumerable<IDeclaration> declarations)
        {
            foreach (var declaration in declarations)
                AddDeclaration(declaration);
        }

        public void RemoveDeclaration(IDeclaration declaration)
        {
            string name = declaration.Name.Text;
            if (string.IsNullOrEmpty(name))
                return;

            List<IDeclaration> declarations;
            if (Declarations.TryGetValue(name, out declarations))
                declarations.Remove(declaration);
        }

        #endregion

        public class DeclarationList
        {
            public DeclarationList()
            {
                Standard = new List<IDeclaration>();
                Generics = new List<IGenerics>();
            }

            public List<IDeclaration> Standard;

            public List<IGenerics> Generics;

            public void Add(IDeclaration declaration)
            {
                Standard.Add(declaration);
                var genDecl = declaration as IGenerics;
                if (genDecl != null && genDecl.GenericParameters.Count > 0)
                {
                    Generics.Add((IGenerics)declaration);
                }
            }

            public void Remove(IDeclaration declaration)
            {
                Standard.Remove(declaration);
                if (declaration is IGenerics)
                    Generics.Remove((IGenerics)declaration);
            }
        }
    }
}
