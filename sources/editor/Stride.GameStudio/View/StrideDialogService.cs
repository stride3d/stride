// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.GameStudio.Services;
using Stride.Core.Assets.Editor.View;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;

namespace Stride.GameStudio.View
{
    public class StrideDialogService : EditorDialogService, IStrideDialogService
    {
        public StrideDialogService(IDispatcherService dispatcher, string applicationName)
            : base(dispatcher, applicationName)
        {
        }

        public ICredentialsDialog CreateCredentialsDialog()
        {
            return new CredentialsDialog(this);
        }

        public void ShowAboutPage()
        {
            var page = new AboutPage(this);
            page.ShowModal().Forget();
        }

    }
}
