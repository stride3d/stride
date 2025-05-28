// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
#pragma warning disable SA1402 // File may only contain a single type

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Graphics;

namespace Stride.Games
{

    /// <summary>
    /// This class determines what devices the game has access to. ie graphics devices, GameWindow and Input devices.
    /// </summary>
    public abstract class GamePlatform : ReferenceBase, IGraphicsDeviceFactory, IGamePlatform
    {
        private bool hasExitRan = false;

        public string FullName { get; protected set; } = string.Empty;

        public IServiceRegistry Services { get; protected set; }

        public abstract string DefaultAppDirectory { get; }

        public event EventHandler<EventArgs> Activated;

        public event EventHandler<EventArgs> Deactivated;

        public event EventHandler<EventArgs> Exiting;

        public event EventHandler<EventArgs> Idle;

        public event EventHandler<EventArgs> Resume;

        public event EventHandler<EventArgs> Suspend;

        public bool IsBlockingRun { get; protected set; }

        public virtual void Initialize(IServiceRegistry services)
        {
            Services = services;
        }

        public virtual void Exit() { }

        protected void OnActivated(object source, EventArgs e)
        {
            Activated?.Invoke(this, e);
        }

        protected void OnDeactivated(object source, EventArgs e)
        {
            Deactivated?.Invoke(this, e);
        }

        protected void OnExiting(object source, EventArgs e)
        {
            Exiting?.Invoke(this, e);
        }

        protected void OnIdle(object source, EventArgs e)
        {
            Idle?.Invoke(this, e);
        }

        protected void OnResume(object source, EventArgs e)
        {
            Resume?.Invoke(this, e);
        }

        protected void OnSuspend(object source, EventArgs e)
        {
            Suspend?.Invoke(this, e);
        }

        protected void AddDevice(DisplayMode mode,  GraphicsDeviceInformation deviceBaseInfo, GameGraphicsParameters preferredParameters, List<GraphicsDeviceInformation> graphicsDeviceInfos)
        {
            // TODO: Temporary woraround
            mode ??= new DisplayMode(PixelFormat.R8G8B8A8_UNorm, 800, 480, new Rational(60, 1));

            var deviceInfo = deviceBaseInfo.Clone();

            deviceInfo.PresentationParameters.RefreshRate = mode.RefreshRate;

            if (preferredParameters.IsFullScreen)
            {
                deviceInfo.PresentationParameters.BackBufferFormat = mode.Format;
                deviceInfo.PresentationParameters.BackBufferWidth = mode.Width;
                deviceInfo.PresentationParameters.BackBufferHeight = mode.Height;
            }
            else
            {
                deviceInfo.PresentationParameters.BackBufferFormat = preferredParameters.PreferredBackBufferFormat;
                deviceInfo.PresentationParameters.BackBufferWidth = preferredParameters.PreferredBackBufferWidth;
                deviceInfo.PresentationParameters.BackBufferHeight = preferredParameters.PreferredBackBufferHeight;
            }

            deviceInfo.PresentationParameters.DepthStencilFormat = preferredParameters.PreferredDepthStencilFormat;
            deviceInfo.PresentationParameters.MultisampleCount = preferredParameters.PreferredMultisampleCount;

            if (!graphicsDeviceInfos.Contains(deviceInfo))
            {
                graphicsDeviceInfos.Add(deviceInfo);
            }
        }

        public abstract List<GraphicsDeviceInformation> FindBestDevices(GameGraphicsParameters preferredParameters);

        public abstract GraphicsDevice CreateDevice(GraphicsDeviceInformation deviceInformation);

        public virtual void RecreateDevice(GraphicsDevice currentDevice, GraphicsDeviceInformation deviceInformation)
        {
        }

        public virtual void DeviceChanged(GraphicsDevice currentDevice, GraphicsDeviceInformation deviceInformation)
        {

        }

        public virtual GraphicsDevice ChangeOrCreateDevice(GraphicsDevice currentDevice, GraphicsDeviceInformation deviceInformation)
        {
            if (currentDevice == null)
            {
                currentDevice = CreateDevice(deviceInformation);
            }
            else
            {
                RecreateDevice(currentDevice, deviceInformation);
            }

            DeviceChanged(currentDevice, deviceInformation);

            return currentDevice;
        }

        protected override void Destroy()
        {
            Activated = null;
            Deactivated = null;
            Exiting = null;
            Idle = null;
            Resume = null;
            Suspend = null;
        }
    }
}
