// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Core.Diagnostics;
using Xenko.Core.Mathematics;
using Xenko.Games;

namespace Xenko.Input
{
    /// <summary>
    /// Manages collecting input from connected input device in the form of <see cref="IInputDevice"/> objects. Also provides some convenience functions for most commonly used devices
    /// </summary>
    public partial class InputManager : GameSystemBase
    {
        //this is used in some mobile platform for accelerometer stuff
        internal const float G = 9.81f;
        internal const float DesiredSensorUpdateRate = 60;
        
        /// <summary>
        /// The deadzone amount applied to all game controller axes
        /// </summary>
        public static float GameControllerAxisDeadZone = 0.05f;
        
        internal static Logger Logger = GlobalLogger.GetLogger("Input");

        private readonly List<IInputDevice> devices = new List<IInputDevice>();

        private readonly List<InputEvent> events = new List<InputEvent>();
        private readonly List<GestureEvent> currentGestureEvents = new List<GestureEvent>();

        private readonly Dictionary<GestureConfig, GestureRecognizer> gestureConfigToRecognizer = new Dictionary<GestureConfig, GestureRecognizer>();
        private readonly List<Dictionary<object, float>> virtualButtonValues = new List<Dictionary<object, float>>();

        // Mapping of device guid to device
        private readonly Dictionary<Guid, IInputDevice> devicesById = new Dictionary<Guid, IInputDevice>();

        // List mapping GamePad index to the guid of the device
        private readonly List<List<IGamePadDevice>> gamePadRequestedIndex = new List<List<IGamePadDevice>>();

        private readonly List<IKeyboardDevice> keyboards = new List<IKeyboardDevice>();
        private readonly List<IPointerDevice> pointers = new List<IPointerDevice>();
        private readonly List<IGameControllerDevice> gameControllers = new List<IGameControllerDevice>();
        private readonly List<IGamePadDevice> gamePads = new List<IGamePadDevice>();
        private readonly List<ISensorDevice> sensors = new List<ISensorDevice>();

        private readonly Dictionary<Type, IInputEventRouter> eventRouters = new Dictionary<Type, IInputEventRouter>();

        private Dictionary<IInputSource, EventHandler<TrackingCollectionChangedEventArgs>> devicesCollectionChangedActions = new Dictionary<IInputSource, EventHandler<TrackingCollectionChangedEventArgs>>();

#if XENKO_INPUT_RAWINPUT
        private bool rawInputEnabled = false;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="InputManager"/> class.
        /// </summary>
        internal InputManager(IServiceRegistry registry) : base(registry)
        {
            Enabled = true;
            
            Gestures = new TrackingCollection<GestureConfig>();
            Gestures.CollectionChanged += GesturesOnCollectionChanged;

            Sources = new TrackingCollection<IInputSource>();
            Sources.CollectionChanged += SourcesOnCollectionChanged;
        }

        /// <summary>
        /// Gets or sets the configuration for virtual buttons.
        /// </summary>
        /// <value>The current binding.</value>
        public VirtualButtonConfigSet VirtualButtonConfigSet { get; set; }

        /// <summary>
        /// List of the gestures to recognize.
        /// </summary>
        public TrackingCollection<GestureConfig> Gestures { get; }
        
        /// <summary>
        /// Input sources
        /// </summary>
        public TrackingCollection<IInputSource> Sources { get; }

        /// <summary>
        /// Gets the reference to the accelerometer sensor. The accelerometer measures all the acceleration forces applied on the device.
        /// </summary>
        public IAccelerometerSensor Accelerometer { get; private set; }

        /// <summary>
        /// Gets the reference to the compass sensor. The compass measures the angle between the device top and the north.
        /// </summary>
        public ICompassSensor Compass { get; private set; }

        /// <summary>
        /// Gets the reference to the gyroscope sensor. The gyroscope measures the rotation speed of the device.
        /// </summary>
        public IGyroscopeSensor Gyroscope { get; private set; }

        /// <summary>
        /// Gets the reference to the user acceleration sensor. The user acceleration sensor measures the acceleration produce by the user on the device (no gravity).
        /// </summary>
        public IUserAccelerationSensor UserAcceleration { get; private set; }

        /// <summary>
        /// Gets the reference to the gravity sensor. The gravity sensor measures the gravity vector applied to the device.
        /// </summary>
        public IGravitySensor Gravity { get; private set; }

        /// <summary>
        /// Gets the reference to the orientation sensor. The orientation sensor measures orientation of device in the world.
        /// </summary>
        public IOrientationSensor Orientation { get; private set; }

        /// <summary>
        /// Gets the value indicating if the mouse position is currently locked or not.
        /// </summary>
        public bool IsMousePositionLocked => HasMouse && Mouse.IsPositionLocked;

        /// <summary>
        /// All input events that happened since the last frame
        /// </summary>
        public IReadOnlyList<InputEvent> Events => events;

        /// <summary>
        /// Gets the collection of gesture events since the previous updates.
        /// </summary>
        /// <value>The gesture events.</value>
        public IReadOnlyList<GestureEvent> GestureEvents => currentGestureEvents;

        /// <summary>
        /// Gets a value indicating whether pointer device is available.
        /// </summary>
        /// <value><c>true</c> if pointer devices are available; otherwise, <c>false</c>.</value>
        public bool HasPointer { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the mouse is available.
        /// </summary>
        /// <value><c>true</c> if the mouse is available; otherwise, <c>false</c>.</value>
        public bool HasMouse { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the keyboard is available.
        /// </summary>
        /// <value><c>true</c> if the keyboard is available; otherwise, <c>false</c>.</value>
        public bool HasKeyboard { get; private set; }

        /// <summary>
        /// Gets a value indicating whether game controllers are available.
        /// </summary>
        /// <value><c>true</c> if game controllers are available; otherwise, <c>false</c>.</value>
        public bool HasGameController { get; private set; }

        /// <summary>
        /// Gets a value indicating whether gamepads are available.
        /// </summary>
        /// <value><c>true</c> if gamepads are available; otherwise, <c>false</c>.</value>
        public bool HasGamePad { get; private set; }

        /// <summary>
        /// Gets the number of game controllers connected.
        /// </summary>
        /// <value>The number of game controllers connected.</value>
        public int GameControllerCount { get; private set; }

        /// <summary>
        /// Gets the number of gamepads connected.
        /// </summary>
        /// <value>The number of gamepads connected.</value>
        public int GamePadCount { get; private set; }

        /// <summary>
        /// Gets the first pointer device, or null if there is none
        /// </summary>
        public IPointerDevice Pointer { get; private set; }

        /// <summary>
        /// Gets the first mouse pointer device, or null if there is none
        /// </summary>
        public IMouseDevice Mouse { get; private set; }

        /// <summary>
        /// Gets the first keyboard device, or null if there is none
        /// </summary>
        public IKeyboardDevice Keyboard { get; private set; }

        /// <summary>
        /// First device that supports text input, or null if there is none
        /// </summary>
        public ITextInputDevice TextInput { get; private set; }

        /// <summary>
        /// Gets the first gamepad that was added to the device
        /// </summary>
        public IGamePadDevice DefaultGamePad { get; private set; }

        /// <summary>
        /// Gets the collection of connected game controllers
        /// </summary>
        public IReadOnlyList<IGameControllerDevice> GameControllers => gameControllers;

        /// <summary>
        /// Gets the collection of connected gamepads
        /// </summary>
        public IReadOnlyList<IGamePadDevice> GamePads => gamePads;

        /// <summary>
        /// Gets the collection of connected pointing devices (mouses, touchpads, etc)
        /// </summary>
        public IReadOnlyList<IPointerDevice> Pointers => pointers;

        /// <summary>
        /// Gets the collection of connected keyboard inputs
        /// </summary>
        public IReadOnlyList<IKeyboardDevice> Keyboards => keyboards;

        /// <summary>
        /// Gets the collection of connected sensor devices
        /// </summary>
        public IReadOnlyList<ISensorDevice> Sensors => sensors;
        
        /// <summary>
        /// Should raw input be used on windows
        /// </summary>
        public bool UseRawInput
        {
#if XENKO_INPUT_RAWINPUT
            get
            {
                return rawInputEnabled;
            }
            set
            {
                InputSourceWindowsRawInput rawInputSource = Sources.OfType<InputSourceWindowsRawInput>().FirstOrDefault();

                if (value)
                {
                    if (rawInputSource == null)
                    {
                        rawInputSource = new InputSourceWindowsRawInput();
                        Sources.Add(rawInputSource);
                    }
                }
                else
                {
                    // Disable by removing the raw input source
                    if (rawInputSource != null)
                    {
                        Sources.Remove(rawInputSource);
                    }
                }
                rawInputEnabled = value;
            }
#else
            get { return false; }
            set { }
#endif
        }   

        /// <summary>
        /// Raised before new input is sent to their respective event listeners
        /// </summary>
        public event EventHandler<InputPreUpdateEventArgs> PreUpdateInput;

        /// <summary>
        /// Raised when a device was removed from the system
        /// </summary>
        public event EventHandler<DeviceChangedEventArgs> DeviceRemoved;

        /// <summary>
        /// Raised when a device was added to the system
        /// </summary>
        public event EventHandler<DeviceChangedEventArgs> DeviceAdded;
        
        /// <summary>
        /// Helper method to transform mouse and pointer event positions to sub rectangles
        /// </summary>
        /// <param name="fromSize">the size of the source rectangle</param>
        /// <param name="destinationRectangle">The destination viewport rectangle</param>
        /// <param name="screenCoordinates">The normalized screen coordinates</param>
        /// <returns></returns>
        public static Vector2 TransformPosition(Size2F fromSize, RectangleF destinationRectangle, Vector2 screenCoordinates)
        {
            return new Vector2((screenCoordinates.X * fromSize.Width - destinationRectangle.X) / destinationRectangle.Width,
                (screenCoordinates.Y * fromSize.Height - destinationRectangle.Y) / destinationRectangle.Height);
        }

        public override void Initialize()
        {
            base.Initialize();

            Game.Activated += OnApplicationResumed;
            Game.Deactivated += OnApplicationPaused;

            AddSources();

            // After adding initial devices, reassign gamepad id's
            // this creates a beter index assignment in the case where you have both an xbox controller and another controller at startup
            var sortedGamePads = GamePads.OrderBy(x => x.CanChangeIndex);
            
            foreach (var gamePad in sortedGamePads)
            {
                if (gamePad.CanChangeIndex)
                    gamePad.Index = GetFreeGamePadIndex(gamePad);
            }

            // Register event types
            RegisterEventType<KeyEvent>();
            RegisterEventType<TextInputEvent>();
            RegisterEventType<MouseButtonEvent>();
            RegisterEventType<MouseWheelEvent>();
            RegisterEventType<PointerEvent>();
            RegisterEventType<GameControllerButtonEvent>();
            RegisterEventType<GameControllerAxisEvent>();
            RegisterEventType<GameControllerDirectionEvent>();
            RegisterEventType<GamePadButtonEvent>();
            RegisterEventType<GamePadAxisEvent>();

            // Add global input state to listen for input events
            AddListener(this);
        }

        /// <summary>
        /// Lock the mouse's position and hides it until the next call to <see cref="UnlockMousePosition"/>.
        /// </summary>
        /// <param name="forceCenter">If true will make sure that the mouse cursor position moves to the center of the client window</param>
        /// <remarks>This function has no effects on devices that does not have mouse</remarks>
        public void LockMousePosition(bool forceCenter = false)
        {
            // Lock primary mouse
            if (HasMouse)
            {
                Mouse.LockPosition(forceCenter);
            }
        }

        /// <summary>
        /// Unlock the mouse's position previously locked by calling <see cref="LockMousePosition"/> and restore the mouse visibility.
        /// </summary>
        /// <remarks>This function has no effects on devices that does not have mouse</remarks>
        public void UnlockMousePosition()
        {
            if (HasMouse)
            {
                Mouse.UnlockPosition();
            }
        }

        /// <summary>
        /// Gets the first gamepad with a specific index
        /// </summary>
        /// <param name="gamePadIndex">The index of the gamepad</param>
        /// <returns>The gamepad, or null if no gamepad has this index</returns>
        /// <exception cref="IndexOutOfRangeException">When <paramref name="gamePadIndex"/> is less than 0</exception>
        public IGamePadDevice GetGamePadByIndex(int gamePadIndex)
        {
            if (gamePadIndex < 0) throw new IndexOutOfRangeException(nameof(gamePadIndex));
            if (gamePadIndex >= gamePadRequestedIndex.Count)
                return null;
            return gamePadRequestedIndex[gamePadIndex].FirstOrDefault();
        }

        /// <summary>
        /// Gets all the gamepads with a specific index
        /// </summary>
        /// <param name="gamePadIndex">The index of the gamepad</param>
        /// <returns>The gamepads, or null if no gamepad has this index</returns>
        /// <exception cref="IndexOutOfRangeException">When <paramref name="gamePadIndex"/> is less than 0</exception>
        public IEnumerable<IGamePadDevice> GetGamePadsByIndex(int gamePadIndex)
        {
            if (gamePadIndex < 0) throw new IndexOutOfRangeException(nameof(gamePadIndex));
            if (gamePadIndex >= gamePadRequestedIndex.Count)
                return null;
            return gamePadRequestedIndex[gamePadIndex];
        }

        /// <summary>
        /// Rescans all input devices in order to query new device connected. See remarks.
        /// </summary>
        /// <remarks>
        /// This method could take several milliseconds and should be used at specific time in a game where performance is not crucial (pause, configuration screen...etc.)
        /// </remarks>
        public void Scan()
        {
            foreach (var source in Sources)
            {
                source.Scan();
            }
        }

        public override void Update(GameTime gameTime)
        {
            ResetGlobalInputState();

            // Recycle input event to reduce garbage generation
            foreach (var evt in events)
            {
                // The router takes care of putting the event back in its respective InputEventPool since it already has the type information
                eventRouters[evt.GetType()].PoolEvent(evt);
            }
            events.Clear();

            // Update all input sources so they can route events to input devices and possible register new devices
            foreach (var source in Sources)
            {
                source.Update();
            }

            // Update all input sources so they can send events and update their state
            foreach (var inputDevice in devices)
            {
                inputDevice.Update(events);
            }

            // Notify PreUpdateInput
            PreUpdateInput?.Invoke(this, new InputPreUpdateEventArgs { GameTime = gameTime });
            
            // Send events to input listeners
            foreach (var evt in events)
            {
                IInputEventRouter router;
                if (!eventRouters.TryGetValue(evt.GetType(), out router))
                    throw new InvalidOperationException($"The event type {evt.GetType()} was not registered with the input manager and cannot be processed");

                router.RouteEvent(evt);
            }

            // Update virtual buttons
            UpdateVirtualButtonValues();

            // Update gestures
            UpdateGestureEvents(gameTime.Elapsed);
        }

        /// <summary>
        /// Registers an object that listens for certain types of events using the specialized versions of <see cref="IInputEventListener&lt;"/>
        /// </summary>
        /// <param name="listener">The listener to register</param>
        public void AddListener(IInputEventListener listener)
        {
            foreach (var router in eventRouters)
            {
                router.Value.TryAddListener(listener);
            }
        }

        /// <summary>
        /// Removes a previously registered event listener
        /// </summary>
        /// <param name="listener">The listener to remove</param>
        public void RemoveListener(IInputEventListener listener)
        {
            foreach (var pair in eventRouters)
            {
                pair.Value.Listeners.Remove(listener);
            }
        }
        
        /// <summary>
        /// Gets a binding value for the specified name and the specified config extract from the current <see cref="VirtualButtonConfigSet"/>.
        /// </summary>
        /// <param name="configIndex">An index to a <see cref="VirtualButtonConfig"/> stored in the <see cref="VirtualButtonConfigSet"/></param>
        /// <param name="bindingName">Name of the binding.</param>
        /// <returns>The value of the binding.</returns>
        public virtual float GetVirtualButton(int configIndex, object bindingName)
        {
            if (VirtualButtonConfigSet == null || configIndex < 0 || configIndex >= virtualButtonValues.Count)
            {
                return 0.0f;
            }

            float value;
            virtualButtonValues[configIndex].TryGetValue(bindingName, out value);
            return value;
        }
        
        private void OnApplicationPaused(object sender, EventArgs e)
        {
            // Pause sources
            foreach (var source in Sources)
            {
                source.Pause();
            }
        }

        private void OnApplicationResumed(object sender, EventArgs e)
        {
            // Resume sources
            foreach (var source in Sources)
            {
                source.Resume();
            }
        }
        
        private void SourcesOnCollectionChanged(object o, TrackingCollectionChangedEventArgs e)
        {
            var source = (IInputSource)e.Item;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (Sources.Count(x => x == source) > 1)
                        throw new InvalidOperationException("Input Source already added");

                    EventHandler<TrackingCollectionChangedEventArgs> eventHandler = (sender, args) => InputDevicesOnCollectionChanged(source, args);
                    devicesCollectionChangedActions.Add(source, eventHandler);
                    source.Devices.CollectionChanged += eventHandler;
                    source.Initialize(this);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    source.Dispose();
                    source.Devices.CollectionChanged -= devicesCollectionChangedActions[source];
                    devicesCollectionChangedActions.Remove(source);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(e.Action));
            }
        }

        /// <summary>
        /// Registers an input event type to process
        /// </summary>
        /// <typeparam name="TEventType">The event type to process</typeparam>
        public void RegisterEventType<TEventType>() where TEventType : InputEvent, new()
        {
            var type = typeof(TEventType);
            eventRouters.Add(type, new InputEventRouter<TEventType>());
        }

        /// <summary>
        /// Inserts any registered event back into it's <see cref="InputEventPool&lt;"/>.
        /// </summary>
        /// <param name="inputEvent">The event to insert into it's event pool</param>
        public void PoolInputEvent(InputEvent inputEvent)
        {
            eventRouters[inputEvent.GetType()].PoolEvent(inputEvent);
        }

        /// <summary>
        /// Resets the <see cref="Sources"/> collection back to it's default values
        /// </summary>
        public void ResetSources()
        {
            Sources.Clear();
            AddSources();
        }
        
        /// <summary>
        /// Suggests an index that is unused for a given <see cref="IGamePadDevice"/>
        /// </summary>
        /// <param name="gamePad">The gamepad to find an index for</param>
        /// <returns>The unused gamepad index</returns>
        public int GetFreeGamePadIndex(IGamePadDevice gamePad)
        {
            if (gamePad == null)
                throw new ArgumentNullException(nameof(gamePad));
            if (!GamePads.Contains(gamePad))
                throw new InvalidOperationException("Not a valid gamepad");

            // Find a new index for this game controller
            int targetIndex = 0;
            for (int i = 0; i < gamePadRequestedIndex.Count; i++)
            {
                var collection = gamePadRequestedIndex[i];
                if (collection.Count == 0 || (collection.Count == 1 && collection[0] == gamePad))
                {
                    targetIndex = i;
                    break;
                }
                targetIndex++;
            }

            return targetIndex;
        }

        private void AddSources()
        {
            // Create input sources
            switch (Game.Context.ContextType)
            {
#if XENKO_UI_SDL
                case AppContextType.DesktopSDL:
                    Sources.Add(new InputSourceSDL());
                    break;
#endif
#if XENKO_PLATFORM_ANDROID
                case AppContextType.Android:
                    Sources.Add(new InputSourceAndroid());
                    break;
#endif
#if XENKO_PLATFORM_IOS
                case AppContextType.iOS:
                    Sources.Add(new InputSourceiOS());
                    break;
#endif
#if XENKO_PLATFORM_UWP
                case AppContextType.UWPXaml:
                case AppContextType.UWPCoreWindow:
                    Sources.Add(new InputSourceUWP());
                    break;
#endif
                case AppContextType.Desktop:
#if XENKO_PLATFORM_WINDOWS && (XENKO_UI_WINFORMS || XENKO_UI_WPF)
                    Sources.Add(new InputSourceWinforms());
                    Sources.Add(new InputSourceWindowsDirectInput());
                    if (InputSourceWindowsXInput.IsSupported())
                        Sources.Add(new InputSourceWindowsXInput());
#endif
#if XENKO_INPUT_RAWINPUT
                    if (rawInputEnabled)
                        Sources.Add(new InputSourceWindowsRawInput());
#endif
                    break;
                default:
                    throw new InvalidOperationException("GameContext type is not supported by the InputManager");
            }
        }

        protected override void Destroy()
        {
            base.Destroy();

            // Unregister all gestures
            Gestures.Clear();

            // Destroy all input sources
            foreach (var source in Sources)
            {
                source.Dispose();
            }

            Game.Activated -= OnApplicationResumed;
            Game.Deactivated -= OnApplicationPaused;

            // ensure that OnApplicationPaused is called before destruction, when Game.Deactivated event is not triggered.
            OnApplicationPaused(this, EventArgs.Empty);
        }

        private void GesturesOnCollectionChanged(object sender, TrackingCollectionChangedEventArgs trackingCollectionChangedEventArgs)
        {
            switch (trackingCollectionChangedEventArgs.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    StartGestureRecognition((GestureConfig)trackingCollectionChangedEventArgs.Item);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    StopGestureRecognition((GestureConfig)trackingCollectionChangedEventArgs.Item);
                    break;
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException("ActivatedGestures collection was modified but the action was not supported by the system.");
                case NotifyCollectionChangedAction.Move:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void StartGestureRecognition(GestureConfig config)
        {
            float aspectRatio = Pointer?.SurfaceAspectRatio ?? 1.0f; 
            gestureConfigToRecognizer.Add(config, config.CreateRecognizer(aspectRatio));
        }

        private void StopGestureRecognition(GestureConfig config)
        {
            gestureConfigToRecognizer.Remove(config);
        }

        private void SetMousePosition(Vector2 normalizedPosition)
        {
            // Set mouse position for first mouse device
            if (HasMouse)
            {
                Mouse.SetPosition(normalizedPosition);
            }
        }

        private void InputDevicesOnCollectionChanged(IInputSource source, TrackingCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    OnInputDeviceAdded(source, (IInputDevice)e.Item);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    OnInputDeviceRemoved((IInputDevice)e.Item);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported collection operation");
            }
        }

        private void OnInputDeviceAdded(IInputSource source, IInputDevice device)
        {
            devices.Add(device);
            if (devicesById.ContainsKey(device.Id))
                throw new InvalidOperationException($"Device with Id {device.Id}({device.Name}) already registered to {devicesById[device.Id].Name}");

            devicesById.Add(device.Id, device);

            if (device is IKeyboardDevice)
            {
                RegisterKeyboard((IKeyboardDevice)device);
                keyboards.Sort((l, r) => -l.Priority.CompareTo(r.Priority));
            }
            else if (device is IPointerDevice)
            {
                RegisterPointer((IPointerDevice)device);
                pointers.Sort((l, r) => -l.Priority.CompareTo(r.Priority));
            }
            else if (device is IGameControllerDevice)
            {
                RegisterGameController((IGameControllerDevice)device);
                gameControllers.Sort((l, r) => -l.Priority.CompareTo(r.Priority));
            }
            else if (device is IGamePadDevice)
            {
                RegisterGamePad((IGamePadDevice)device);
                gamePads.Sort((l, r) => -l.Priority.CompareTo(r.Priority));
            }
            else if (device is ISensorDevice)
            {
                RegisterSensor((ISensorDevice)device);
            }
            UpdateConnectedDevices();

            DeviceAdded?.Invoke(this, new DeviceChangedEventArgs { Device = device, Source = source, Type = DeviceChangedEventType.Added });
        }

        private void OnInputDeviceRemoved(IInputDevice device)
        {
            if (!devices.Contains(device))
                throw new InvalidOperationException("Input device was not registered");

            var source = device.Source;
            devices.Remove(device);
            devicesById.Remove(device.Id);

            if (device is IKeyboardDevice)
            {
                UnregisterKeyboard((IKeyboardDevice)device);
            }
            else if (device is IPointerDevice)
            {
                UnregisterPointer((IPointerDevice)device);
            }
            else if (device is IGameControllerDevice)
            {
                UnregisterGameController((IGameControllerDevice)device);
            }
            else if (device is IGamePadDevice)
            {
                UnregisterGamePad((IGamePadDevice)device);
            }
            else if (device is ISensorDevice)
            {
                UnregisterSensor((ISensorDevice)device);
            }
            UpdateConnectedDevices();

            DeviceRemoved?.Invoke(this, new DeviceChangedEventArgs { Device = device, Source = source, Type = DeviceChangedEventType.Removed });
        }

        private void UpdateConnectedDevices()
        {
            Keyboard = keyboards.FirstOrDefault();
            HasKeyboard = Keyboard != null;

            TextInput = devices.OfType<ITextInputDevice>().FirstOrDefault();

            Mouse = pointers.OfType<IMouseDevice>().FirstOrDefault();
            HasMouse = Mouse != null;

            Pointer = pointers.FirstOrDefault();
            HasPointer = Pointer != null;

            GameControllerCount = GameControllers.Count;
            HasGameController = GameControllerCount > 0;

            GamePadCount = GamePads.Count;
            HasGamePad = GamePadCount > 0;

            gamePads.Sort((l, r) => l.Index.CompareTo(r.Index));

            DefaultGamePad = gamePads.FirstOrDefault();

            Accelerometer = sensors.OfType<IAccelerometerSensor>().FirstOrDefault();
            Gyroscope = sensors.OfType<IGyroscopeSensor>().FirstOrDefault();
            Compass = sensors.OfType<ICompassSensor>().FirstOrDefault();
            UserAcceleration = sensors.OfType<IUserAccelerationSensor>().FirstOrDefault();
            Orientation = sensors.OfType<IOrientationSensor>().FirstOrDefault();
            Gravity = sensors.OfType<IGravitySensor>().FirstOrDefault();
        }

        private void RegisterPointer(IPointerDevice pointer)
        {
            pointers.Add(pointer);
        }

        private void UnregisterPointer(IPointerDevice pointer)
        {
            pointers.Remove(pointer);
        }

        private void RegisterKeyboard(IKeyboardDevice keyboard)
        {
            keyboards.Add(keyboard);
        }

        private void UnregisterKeyboard(IKeyboardDevice keyboard)
        {
            keyboards.Remove(keyboard);
        }

        private void RegisterGamePad(IGamePadDevice gamePad)
        {
            gamePads.Add(gamePad);

            // Check if the gamepad provides an interface for assigning gamepad index
            if (gamePad.CanChangeIndex)
            {
                gamePad.Index = GetFreeGamePadIndex(gamePad);
            }
            
            // Handle later index changed
            gamePad.IndexChanged += GamePadOnIndexChanged;
            UpdateGamePadRequestedIndices();
        }

        private void UnregisterGamePad(IGamePadDevice gamePad)
        {
            // Free the gamepad index in the gamepad list
            // this will allow another gamepad to use this index again
            if (gamePadRequestedIndex.Count <= gamePad.Index || gamePad.Index < 0)
                throw new IndexOutOfRangeException("Gamepad index was out of range");

            gamePadRequestedIndex[gamePad.Index].Remove(gamePad);

            gamePads.Remove(gamePad);
            gamePad.IndexChanged -= GamePadOnIndexChanged;
        }

        private void RegisterGameController(IGameControllerDevice gameController)
        {
            gameControllers.Add(gameController);
        }

        private void UnregisterGameController(IGameControllerDevice gameController)
        {
            gameControllers.Remove(gameController);
        }

        private void GamePadOnIndexChanged(object sender, GamePadIndexChangedEventArgs e)
        {
            UpdateGamePadRequestedIndices();
        }

        private void RegisterSensor(ISensorDevice sensorDevice)
        {
            sensors.Add(sensorDevice);
        }

        private void UnregisterSensor(ISensorDevice sensorDevice)
        {
            sensors.Remove(sensorDevice);
        }

        /// <summary>
        /// Updates the <see cref="gamePadRequestedIndex"/> collection to contains every gamepad with a given index
        /// </summary>
        private void UpdateGamePadRequestedIndices()
        {
            foreach (var gamePads in gamePadRequestedIndex)
            {
                gamePads.Clear();
            }

            foreach (var gamePad in GamePads)
            {
                while (gamePad.Index >= gamePadRequestedIndex.Count)
                {
                    gamePadRequestedIndex.Add(new List<IGamePadDevice>());
                }
                gamePadRequestedIndex[gamePad.Index].Add(gamePad);
            }
        }

        private void UpdateGestureEvents(TimeSpan elapsedGameTime)
        {
            currentGestureEvents.Clear();

            foreach (var gestureRecognizer in gestureConfigToRecognizer.Values)
            {
                gestureRecognizer.ProcessPointerEvents(elapsedGameTime, pointerEvents, currentGestureEvents);
            }
        }

        private void UpdateVirtualButtonValues()
        {
            if (VirtualButtonConfigSet != null)
            {
                for (int i = 0; i < VirtualButtonConfigSet.Count; i++)
                {
                    var config = VirtualButtonConfigSet[i];

                    Dictionary<object, float> mapNameToValue;
                    if (i == virtualButtonValues.Count)
                    {
                        mapNameToValue = new Dictionary<object, float>();
                        virtualButtonValues.Add(mapNameToValue);
                    }
                    else
                    {
                        mapNameToValue = virtualButtonValues[i];
                    }

                    mapNameToValue.Clear();

                    if (config != null)
                    {
                        foreach (var name in config.BindingNames)
                        {
                            mapNameToValue[name] = config.GetValue(this, name);
                        }
                    }
                }
            }
        }

        private interface IInputEventRouter
        {
            HashSet<IInputEventListener> Listeners { get; }

            void PoolEvent(InputEvent evt);

            void RouteEvent(InputEvent evt);

            void TryAddListener(IInputEventListener listener);
        }

        private class InputEventRouter<TEventType> : IInputEventRouter where TEventType : InputEvent, new()
        {
            public HashSet<IInputEventListener> Listeners { get; } = new HashSet<IInputEventListener>(ReferenceEqualityComparer<IInputEventListener>.Default);

            public void RouteEvent(InputEvent evt)
            {
                var listeners = Listeners.ToArray();
                foreach (var gesture in listeners)
                {
                    ((IInputEventListener<TEventType>)gesture).ProcessEvent((TEventType)evt);
                }
            }

            public void TryAddListener(IInputEventListener listener)
            {
                var specific = listener as IInputEventListener<TEventType>;
                if (specific != null)
                {
                    Listeners.Add(specific);
                }
            }

            public void PoolEvent(InputEvent evt)
            {
                InputEventPool<TEventType>.Enqueue((TEventType)evt);
            }
        }
    }
}
