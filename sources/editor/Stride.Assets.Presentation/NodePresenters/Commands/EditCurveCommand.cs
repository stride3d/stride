// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Mathematics;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Animations;

namespace Stride.Assets.Presentation.NodePresenters.Commands
{
    public class EditCurveCommand : SyncNodePresenterCommandBase
    {
        /// <summary>
        /// The name of this command.
        /// </summary>
        public const string CommandName = "EditCurve";

        /// <summary>
        /// The current session.
        /// </summary>
        private readonly SessionViewModel session;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditCurveCommand"/> class.
        /// </summary>
        /// <param name="session">The current session.</param>
        public EditCurveCommand(SessionViewModel session)
        {
            this.session = session;
        }

        /// <inheritdoc/>
        public override string Name => CommandName;

        /// <inheritdoc/>
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            return MatchType(nodePresenter.Type);            
        }

        /// <inheritdoc/>
        protected override void ExecuteSync(INodePresenter nodePresenter, object parameter, object preExecuteResult)
        {
            var currentValue = nodePresenter.Value;
            if (currentValue != null)
            {
                // TODO: we should display the path instead of the name for the title
                session.ServiceProvider.Get<IEditorDialogService>().AssetEditorsManager.OpenCurveEditorWindow(currentValue, nodePresenter.Name);
            }
        }

        private static bool MatchType(Type type)
        {
            return type.HasInterface(typeof(IComputeCurve<Color4>))
                || type.HasInterface(typeof(IComputeCurve<float>))
                || type.HasInterface(typeof(IComputeCurve<Quaternion>))
                || type.HasInterface(typeof(IComputeCurve<Vector2>))
                || type.HasInterface(typeof(IComputeCurve<Vector3>))
                || type.HasInterface(typeof(IComputeCurve<Vector4>));
        }
    }
}
