// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Games;
using Xenko.Graphics;
using Xenko.Input;
using Xenko.UI.Events;

namespace Xenko.UI.Controls
{
    /// <summary>
    /// Represents a slider element.
    /// </summary>
    [DataContract(nameof(Slider))]
    [Display(category: InputCategory)]
    public class Slider : UIElement
    {
        private float value;

        private bool shouldSnapToTicks;

        private Orientation orientation = Orientation.Horizontal;
        private float tickFrequency = 10.0f;
        private float minimum;
        private float maximum = 1.0f;
        private float step = 0.1f;
        private float tickOffset = 10.0f;
        private ISpriteProvider trackBackgroundImageSource;
        private Sprite trackBackgroundSprite;

        static Slider()
        {
            EventManager.RegisterClassHandler(typeof(Slider), ValueChangedEvent, ValueChangedClassHandler);
        }

        /// <summary>
        /// Create a new instance of slider.
        /// </summary>
        public Slider()
        {
            CanBeHitByUser = true;
            VerticalAlignment = VerticalAlignment.Center;
            HorizontalAlignment = HorizontalAlignment.Center;
            DrawLayerNumber += 4; // track background, track foreground, ticks, thumb
        }

        /// <summary>
        /// Gets or sets the current value of the slider.
        /// </summary>
        /// <remarks>The value is coerced in the range [<see cref="Minimum"/>, <see cref="Maximum"/>].</remarks>
        /// <userdoc>The current value of the slider.</userdoc>
        [DataMember]
        [DefaultValue(0.0f)]
        public float Value
        {
            get { return value; }
            set
            {
                if (float.IsNaN(value))
                    return;
                var oldValue = Value;

                this.value = MathUtil.Clamp(value, Minimum, Maximum);
                if (ShouldSnapToTicks)
                    this.value = CalculateClosestTick(this.value);

                if (Math.Abs(oldValue - this.value) > MathUtil.ZeroTolerance)
                    RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
            }
        }

        /// <summary>
        /// Gets or sets the smallest possible value of the slider.
        /// </summary>
        /// <remarks>The value is coerced in the range [<see cref="float.MinValue"/>, <see cref="float.MaxValue"/>].</remarks>
        /// <userdoc>The smallest possible value of the slider.</userdoc>
        [DataMember]
        [DefaultValue(0.0f)]
        public float Minimum
        {
            get { return minimum; }
            set
            {
                if (float.IsNaN(value))
                    return;
                minimum = MathUtil.Clamp(value, float.MinValue, float.MaxValue);
                CoerceMaximum(maximum);
            }
        }

        /// <summary>
        /// Gets or sets the greatest possible value of the slider.
        /// </summary>
        /// <remarks>The value is coerced in the range [<see cref="Minimum"/>, <see cref="float.MaxValue"/>].</remarks>
        /// <userdoc>The greatest possible value of the slider.</userdoc>
        [DataMember]
        [DefaultValue(1.0f)]
        public float Maximum
        {
            get { return maximum; }
            set
            {
                if (float.IsNaN(value))
                    return;
                CoerceMaximum(value);
            }
        }

        /// <summary>
        /// Gets or sets the step of a <see cref="Value"/> change.
        /// </summary>
        /// <remarks>The value is coerced in the range [0, <see cref="float.MaxValue"/>].</remarks>
        /// <userdoc>The step of a change of the value.</userdoc>
        [DataMember]
        [DataMemberRange(0, 3)]
        [DefaultValue(0.1f)]
        public float Step
        {
            get { return step; }
            set
            {
                if (float.IsNaN(value))
                    return;
                step = MathUtil.Clamp(value, 0.0f, float.MaxValue);
            }
        }

        /// <summary>
        /// Gets or sets the image to display as Track background.
        /// </summary>
        /// <userdoc>The image to display as Track background.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider TrackBackgroundImage
        {
            get { return trackBackgroundImageSource; }
            set
            {
                if (trackBackgroundImageSource == value)
                    return;

                trackBackgroundImageSource = value;
                OnTrackBackgroundSpriteChanged(trackBackgroundImageSource?.GetSprite());
            }
        }

        /// <summary>
        /// Gets or sets the image to display as Track foreground.
        /// </summary>
        /// <userdoc>The image to display as Track foreground.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider TrackForegroundImage { get; set; }

        /// <summary>
        /// Gets or sets the left/right offsets specifying where the track region starts. 
        /// </summary>
        /// <userdoc>The left/right offsets specifying where the track region starts. </userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        public Vector2 TrackStartingOffsets { get; set; }

        /// <summary>
        /// Gets or sets the image to display as slider thumb (button).
        /// </summary>
        /// <userdoc>The image to display as slider thumb (button).</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider ThumbImage { get; set; }

        /// <summary>
        /// Gets or sets the image to display as slider thumb (button) when the mouse is over the slider.
        /// </summary>
        /// <userdoc>The image to display as slider thumb (button) when the mouse is over the slider.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider MouseOverThumbImage { get; set; }

        /// <summary>
        /// Gets or sets the image to display as tick.
        /// </summary>
        /// <userdoc>The image to display as tick.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider TickImage { get; set; }

        /// <summary>
        /// Gets or sets the value indicating if the ticks should be displayed or not.
        /// </summary>
        /// <userdoc>True if the ticks should be displayed, false otherwise.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(false)]
        public bool AreTicksDisplayed { get; set; } = false;

        /// <summary>
        /// Gets or sets the frequency of the ticks on the slider track.
        /// </summary>
        /// <remarks>The value is coerced in the range [1, <see cref="float.MaxValue"/>].</remarks>
        /// <userdoc>The frequency of the ticks on the slider track.</userdoc>
        [DataMember]
        [DataMemberRange(1, 3)]
        [Display(category: AppearanceCategory)]
        [DefaultValue(10.0f)]
        public float TickFrequency
        {
            get { return tickFrequency; }
            set
            {
                if (float.IsNaN(value))
                    return;
                tickFrequency = MathUtil.Clamp(value, 1.0f, float.MaxValue);
                Value = Value; // snap if enabled
            }
        }

        /// <summary>
        /// Gets or sets the offset in virtual pixels between the center of the track and center of the ticks (for an not-stretched slider).
        /// </summary>
        /// <remarks>The value is coerced in the range [0, <see cref="float.MaxValue"/>].</remarks>
        /// <userdoc>The offset in virtual pixels between the center of the track and center of the ticks (for an not-stretched slider).</userdoc>
        [DataMember]
        [DataMemberRange(0, 3)]
        [Display(category: AppearanceCategory)]
        [DefaultValue(10.0f)]
        public float TickOffset
        {
            get { return tickOffset; }
            set
            {
                if (float.IsNaN(value))
                    return;
                tickOffset = MathUtil.Clamp(value, 0.0f, float.MaxValue);
            }
        }

        /// <summary>
        /// Gets or sets the value indicating if the slider <see cref="Value"/> should be snapped to the ticks or not.
        /// </summary>
        /// <userdoc>True if the slider valuye should be snapped to the ticks, false otherwise.</userdoc>
        [DataMember]
        [Display(category: BehaviorCategory)]
        [DefaultValue(false)]
        public bool ShouldSnapToTicks
        {
            get { return shouldSnapToTicks; }
            set 
            { 
                shouldSnapToTicks = value;
                Value = Value; // snap if enabled
            }
        }

        /// <summary>
        /// Gets or sets the value indicating if the default direction of the slider should reversed or not.
        /// </summary>
        /// <userdoc>True if the default direction of the slider should reversed, false otherwise.</userdoc>
        [DataMember]
        [Display(category: BehaviorCategory)]
        [DefaultValue(false)]
        public bool IsDirectionReversed { get; set; } = false;

        /// <summary>
        /// Gets or sets the orientation of the slider.
        /// </summary>
        /// <userdoc>The orientation of the slider.</userdoc>
        [DataMember]
        [Display(category: LayoutCategory)]
        [DefaultValue(Orientation.Horizontal)]
        public Orientation Orientation
        {
            get { return orientation; }
            set
            {
                orientation = value;
                InvalidateMeasure();
            }
        }
        
        /// <summary>
        /// Gets a value that indicates whether the is currently touched down.
        /// </summary>
        [DataMemberIgnore]
        protected virtual bool IsTouchedDown { get; set; }

        /// <summary>
        /// Snap the current <see cref="Value"/> to the closest tick.
        /// </summary>
        public void SnapToClosestTick()
        {
            Value = CalculateClosestTick(Value);
        }

        /// <summary>
        /// Calculate the value of the closest tick to the provided value.
        /// </summary>
        /// <param name="rawValue">The current raw value</param>
        /// <returns>The value adjusted to the closest tick</returns>
        protected float CalculateClosestTick(float rawValue)
        {
            var absoluteValue = rawValue - Minimum;
            var step = (Maximum - Minimum) / TickFrequency;
            var times = (float)Math.Round(absoluteValue / step);
            return times * step;
        }

        /// <summary>
        /// Increase the <see cref="Value"/> by <see cref="Step"/>.
        /// </summary>
        /// <remarks>If <see cref="ShouldSnapToTicks"/> is <value>True</value> then it increases of at least one tick.</remarks>
        public void Increase()
        {
            Value += CalculateIncreamentValue();
        }

        /// <summary>
        /// Decrease the <see cref="Value"/> by <see cref="Step"/>.
        /// </summary>
        /// <remarks>If <see cref="ShouldSnapToTicks"/> is <value>True</value> then it decreases of at least one tick.</remarks>
        public void Decrease()
        {
            Value -= CalculateIncreamentValue();
        }

        private float CalculateIncreamentValue()
        {
            return shouldSnapToTicks ? Math.Max(Step, (Maximum - Minimum)/TickFrequency) : Step;
        }

        protected override Vector3 MeasureOverride(Vector3 availableSizeWithoutMargins)
        {
            if (trackBackgroundSprite == null)
                return base.MeasureOverride(availableSizeWithoutMargins);

            var idealSize = trackBackgroundSprite.SizeInPixels.Y;
            var desiredSize = new Vector3(idealSize, idealSize, 0)
            {
                [(int)Orientation] = availableSizeWithoutMargins[(int)Orientation]
            };

            return desiredSize;
        }

        /// <summary>
        /// Occurs when the value of the slider changed.
        /// </summary>
        /// <remarks>A ValueChanged event is bubbling</remarks>
        public event EventHandler<RoutedEventArgs> ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ValueChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> ValueChangedEvent = EventManager.RegisterRoutedEvent<RoutedEventArgs>(
            nameof(ValueChanged),
            RoutingStrategy.Bubble,
            typeof(Slider));
        
        /// <summary>
        /// The class handler of the event <see cref="ValueChanged"/>.
        /// This method can be overridden in inherited classes to perform actions common to all instances of a class.
        /// </summary>
        /// <param name="args">The arguments of the event</param>
        protected virtual void OnValueChanged(RoutedEventArgs args)
        {

        }

        private static void ValueChangedClassHandler(object sender, RoutedEventArgs args)
        {
            var slider = (Slider)sender;

            slider.OnValueChanged(args);
        }

        protected override void OnTouchDown(TouchEventArgs args)
        {
            base.OnTouchDown(args);
            
            SetValueFromTouchPosition(args.WorldPosition);
            IsTouchedDown = true;
        }

        protected override void OnTouchUp(TouchEventArgs args)
        {
            base.OnTouchUp(args);

            IsTouchedDown = false;
        }

        protected override void OnTouchLeave(TouchEventArgs args)
        {
            base.OnTouchLeave(args);

            IsTouchedDown = false;
        }

        protected override void OnTouchMove(TouchEventArgs args)
        {
            base.OnTouchMove(args);

            if (IsTouchedDown)
            {
                SetValueFromTouchPosition(args.WorldPosition);
            }
        }

        internal override void OnKeyDown(KeyEventArgs args)
        {
            base.OnKeyDown(args);

            if (args.Key == Keys.Right)
                Increase();
            if (args.Key == Keys.Left)
                Decrease();
        }

        /// <summary>
        /// Set <see cref="Value"/> from the world position of a touch event.
        /// </summary>
        /// <param name="touchPostionWorld">The world position of the touch</param>
        protected void SetValueFromTouchPosition(Vector3 touchPostionWorld)
        {
            var axis = (int)Orientation;
            var offsets = TrackStartingOffsets;
            var elementSize = RenderSize[axis];
            var touchPosition = touchPostionWorld[axis] - WorldMatrixInternal[12 + axis] + elementSize/2;
            var ratio = (touchPosition - offsets.X) / (elementSize - offsets.X - offsets.Y);
            Value = MathUtil.Lerp(Minimum, Maximum, Orientation == Orientation.Vertical ^ IsDirectionReversed ? 1 - ratio : ratio);
        }

        protected override void Update(GameTime time)
        {
            var currentSprite = trackBackgroundImageSource?.GetSprite();
            if (trackBackgroundSprite != currentSprite)
            {
                OnTrackBackgroundSpriteChanged(currentSprite);
            }
        }

        private void CoerceMaximum(float newValue)
        {
            maximum = MathUtil.Clamp(newValue, minimum, float.MaxValue);
        }

        private void InvalidateMeasure(object sender, EventArgs eventArgs)
        {
            InvalidateMeasure();
        }

        private void OnTrackBackgroundSpriteChanged(Sprite currentSprite)
        {
            if (trackBackgroundSprite != null)
            {
                trackBackgroundSprite.SizeChanged -= InvalidateMeasure;
                trackBackgroundSprite.BorderChanged -= InvalidateMeasure;
            }
            trackBackgroundSprite = currentSprite;
            InvalidateMeasure();
            if (trackBackgroundSprite != null)
            {
                trackBackgroundSprite.SizeChanged += InvalidateMeasure;
                trackBackgroundSprite.BorderChanged += InvalidateMeasure;
            }
        }
    }
}
