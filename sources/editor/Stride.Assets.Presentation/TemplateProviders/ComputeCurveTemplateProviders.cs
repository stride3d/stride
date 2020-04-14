// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.View;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Animations;

namespace Stride.Assets.Presentation.TemplateProviders
{
    /// <summary>
    /// A base class for implementations of <see cref="NodeViewModelTemplateProvider"/>
    /// that can provide templates for <see cref="IComputeCurve{TValue}"/> instances.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class ComputeCurveTemplateProvider<TValue> : NodeViewModelTemplateProvider
        where TValue : struct
    {
        private static readonly Type ComputeCurveType = typeof(IComputeCurve<TValue>);

        public override string Name => $"IComputeCurve<{typeof(TValue)}>";

        /// <inheritdoc/>
        public override bool MatchNode(NodeViewModel node)
        {
            return MatchType(node, ComputeCurveType);
        }
    }

    public class ComputeCurveColor4TemplateProvider : ComputeCurveTemplateProvider<Color4> { }

    public class ComputeCurveFloatTemplateProvider : ComputeCurveTemplateProvider<float> { }

    public class ComputeCurveQuaternionTemplateProvider : ComputeCurveTemplateProvider<Quaternion> { }

    public class ComputeCurveVector2TemplateProvider : ComputeCurveTemplateProvider<Vector2> { }

    public class ComputeCurveVector3TemplateProvider : ComputeCurveTemplateProvider<Vector3> { }

    public class ComputeCurveVector4TemplateProvider : ComputeCurveTemplateProvider<Vector4> { }
}
