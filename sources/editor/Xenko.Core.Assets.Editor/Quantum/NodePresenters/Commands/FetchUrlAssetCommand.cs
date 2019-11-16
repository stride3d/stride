using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Presentation.Quantum.Presenters;

namespace Xenko.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    public class FetchUrlAssetCommand : FetchAssetCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FetchAssetCommand"/> class.
        /// </summary>
        /// <param name="session">The current session.</param>
        public FetchUrlAssetCommand(SessionViewModel session) : base(session)
        {
        }

        /// <inheritdoc />
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            return UrlReferenceEditorHelper.ContainsUrlReferenceType(nodePresenter.Descriptor);
        }

        /// <inheritdoc />
        public override async Task Execute(INodePresenter nodePresenter, object parameter, object preExecuteResult)
        {
            var asset = UrlReferenceEditorHelper.GetReferenceTarget(Session, nodePresenter.Value);
            if (asset != null)
            {
                await Session.Dispatcher.InvokeAsync(() => Session.ActiveAssetView.SelectAssetCommand.Execute(asset));
            }
        }
    }
}
