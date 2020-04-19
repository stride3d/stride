// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Assets.Presentation.ViewModel
{
    public class StrideAssetsViewModel : DispatcherViewModel
    {
        private static readonly TaskCompletionSource<StrideAssetsViewModel> instance = new TaskCompletionSource<StrideAssetsViewModel>();

        public StrideAssetsViewModel(SessionViewModel session) : base(session.ServiceProvider)
        {
            Session = session;

            Code = new CodeViewModel(this);

            if (Instance != null)
                throw new InvalidOperationException($"The {nameof(StrideAssetsViewModel)} class can be instanced only once.");

            instance.TrySetResult(this);
            Instance = this;
        }

        public static Task<StrideAssetsViewModel> InstanceTask => instance.Task;

        public static StrideAssetsViewModel Instance { get; private set; }

        public SessionViewModel Session { get; }

        public CodeViewModel Code { get; }
    }
}
