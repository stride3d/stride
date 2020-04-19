// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Presentation.ViewModel
{
    /// <summary>
    /// This abstract class is an implementation of <see cref="ViewModelBase"/> that uses a dispatcher to invoke
    /// <see cref="OnPropertyChanging"/> and <see cref="OnPropertyChanged"/> handlers.
    /// </summary>
    public abstract class DispatcherViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DispatcherViewModel"/> class.
        /// </summary>
        /// <param name="serviceProvider">A service provider that can provide a <see cref="IDispatcherService"/> to use for this view model.</param>
        protected DispatcherViewModel([NotNull] IViewModelServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            Dispatcher = serviceProvider.TryGet<IDispatcherService>();
            if (Dispatcher == null) throw new InvalidOperationException($"{nameof(DispatcherViewModel)} requires a {nameof(IDispatcherService)} in the service provider.");
        }

        /// <summary>
        /// Gets a dispatcher that is capable of executing code in the UI thread.
        /// </summary>
        [DataMemberIgnore]
        [NotNull]
        public IDispatcherService Dispatcher { get; }

        /// <inheritdoc/>
        protected override void OnPropertyChanging(params string[] propertyNames)
        {
            if (HasPropertyChangingSubscriber)
                Dispatcher.Invoke(() => base.OnPropertyChanging(propertyNames));
            else
                base.OnPropertyChanging(propertyNames);
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(params string[] propertyNames)
        {
            if (HasPropertyChangedSubscriber)
                Dispatcher.Invoke(() => base.OnPropertyChanged(propertyNames));
            else
                base.OnPropertyChanged(propertyNames);
        }
    }
}
