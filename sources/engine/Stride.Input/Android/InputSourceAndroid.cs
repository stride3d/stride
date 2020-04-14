// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_ANDROID

using System;
using System.Collections.Generic;
using System.Linq;
using Android.Hardware;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Games.Android;

namespace Stride.Input
{
    /// <summary>
    /// Provides support for pointer/keyboard/sensor input on Android
    /// </summary>
    internal class InputSourceAndroid : InputSourceBase
    {
        private AndroidStrideGameView uiControl;
        
        private KeyboardAndroid keyboard;
        private PointerAndroid pointer;

        private AndroidSensorListener accelerometerListener;
        private AndroidSensorListener gyroscopeListener;
        private AndroidSensorListener linearAccelerationListener;
        private AndroidSensorListener orientationListener;
        private AndroidSensorListener gravityListener;
        
        private AccelerometerSensor accelerometerSensor;
        private UserAccelerationSensor userAccelerationSensor;
        private GyroscopeSensor gyroscopeSensor;
        private OrientationSensor orientationSensor;
        private GravitySensor gravitySensor;
        private CompassSensor compassSensor;

        private float[] quaternionArray = new float[4];
        private float[] rotationVector = new float[3];

        public override void Initialize(InputManager inputManager)
        {
            var context = inputManager.Game.Context as GameContextAndroid;
            uiControl = context.Control;

            // Create android pointer and keyboard
            keyboard = new KeyboardAndroid(this, uiControl);
            pointer = new PointerAndroid(this, uiControl);
            RegisterDevice(keyboard);
            RegisterDevice(pointer);

            // Create android sensors
            if ((accelerometerListener = TryGetSensorListener(SensorType.Accelerometer)) != null)
            {
                accelerometerSensor = new AccelerometerSensor(this, "Android");
                RegisterDevice(accelerometerSensor);
            }

            if ((linearAccelerationListener = TryGetSensorListener(SensorType.LinearAcceleration)) != null)
            {
                userAccelerationSensor = new UserAccelerationSensor(this, "Android");
                RegisterDevice(userAccelerationSensor);
            }

            if ((gyroscopeListener = TryGetSensorListener(SensorType.Gyroscope)) != null)
            {
                gyroscopeSensor = new GyroscopeSensor(this, "Android");
                RegisterDevice(gyroscopeSensor);
            }

            if ((gravityListener = TryGetSensorListener(SensorType.Gravity)) != null)
            {
                gravitySensor = new GravitySensor(this, "Android");
                RegisterDevice(gravitySensor);
            }

            if ((orientationListener = TryGetSensorListener(SensorType.RotationVector)) != null)
            {
                orientationSensor = new OrientationSensor(this, "Android");
                compassSensor = new CompassSensor(this, "Android");
                RegisterDevice(orientationSensor);
                RegisterDevice(compassSensor);
            }
        }

        public void UpdateSensorPair<TSensor>(AndroidSensorListener listener, TSensor sensor, Action<TSensor, AndroidSensorListener> updater) where TSensor : Sensor
        {
            if (listener != null)
            {
                bool enable = sensor.IsEnabled;
                if (enable != listener.Enabled)
                {
                    if (enable)
                    {
                        listener.Enable();
                    }
                    else
                    {
                        listener.Disable();
                    }
                }

                if (enable)
                {
                    updater.Invoke(sensor, listener);
                }
            }
        }

        public override void Update()
        {
            base.Update();

            // Update sensors
            // Enable/disable supported sensors and update enabled sensors
            UpdateSensorPair(accelerometerListener, accelerometerSensor, 
                (sensor, listener) => sensor.Acceleration = listener.GetCurrentValuesAsVector());
            UpdateSensorPair(gyroscopeListener, gyroscopeSensor, 
                (sensor, listener) => sensor.RotationRate = -listener.GetCurrentValuesAsVector());
            UpdateSensorPair(linearAccelerationListener, userAccelerationSensor, 
                (sensor, listener) => sensor.Acceleration = listener.GetCurrentValuesAsVector());
            UpdateSensorPair(gravityListener, gravitySensor, 
                (sensor, listener) => sensor.Vector = listener.GetCurrentValuesAsVector());

            // Enabled/Disable/Update Orientation
            if (orientationListener != null)
            {
                bool enable = orientationSensor.IsEnabled || compassSensor.IsEnabled; // Orientation is used for compass as well
                if (enable != orientationListener.Enabled)
                {
                    if (enable)
                    {
                        orientationListener.Enable();
                    }
                    else
                    {
                        orientationListener.Disable();
                    }
                }

                if (enable)
                {
                    // Update orientation
                    base.Update();

                    var orientationValues = orientationListener.GetValues();
                    if (orientationValues == null || orientationValues.Count < 3)
                        return;

                    rotationVector[0] = orientationValues[0];
                    rotationVector[1] = orientationValues[1];
                    rotationVector[2] = orientationValues[2];
                    SensorManager.GetQuaternionFromVector(quaternionArray, rotationVector);

                    var quaternion = Quaternion.Identity;
                    quaternion.W = +quaternionArray[0];
                    quaternion.X = -quaternionArray[1];
                    quaternion.Y = +quaternionArray[3];
                    quaternion.Z = +quaternionArray[2];
                    quaternion = Quaternion.RotationY(MathUtil.Pi) * quaternion; // align the orientation with north.
                    orientationSensor.FromQuaternion(quaternion);

                    // Update compass
                    compassSensor.Heading = -orientationSensor.Yaw;
                }
            }
        }

        public override void Pause()
        {
            // Disable all sensors when application is paused
            accelerometerListener?.Disable();
            gyroscopeListener?.Disable();
            linearAccelerationListener?.Disable();
            orientationListener?.Disable();
            gravityListener?.Disable();
        }

        public override void Resume()
        {
            base.Resume();
            // Automatic resume when update is called
        }

        /// <summary>
        /// Tries to create a sensor listener if it is supported
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public AndroidSensorListener TryGetSensorListener(SensorType type)
        {
            var listener = new AndroidSensorListener(type);
            if (!listener.Enable())
            {
                listener.Dispose();
                return null;
            }

            listener.Disable();
            return listener;
        }
    }
}

#endif
