// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Assets.Presentation.ViewModel
{
    public class XenkoAssetsViewModel : DispatcherViewModel
    {
        private static readonly TaskCompletionSource<XenkoAssetsViewModel> instance = new TaskCompletionSource<XenkoAssetsViewModel>();

        public XenkoAssetsViewModel(SessionViewModel session) : base(session.ServiceProvider)
        {
            Session = session;

            Code = new CodeViewModel(this);

            if (Instance != null)
                throw new InvalidOperationException($"The {nameof(XenkoAssetsViewModel)} class can be instanced only once.");

            instance.TrySetResult(this);
            Instance = this;
        }

        public static Task<XenkoAssetsViewModel> InstanceTask => instance.Task;

        public static XenkoAssetsViewModel Instance { get; private set; }

        public SessionViewModel Session { get; }

        public CodeViewModel Code { get; }
    }
}
