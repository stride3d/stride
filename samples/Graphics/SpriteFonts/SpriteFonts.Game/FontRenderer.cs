// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Graphics;
using Stride.Input;

namespace SpriteFonts
{
    /// <summary>
    /// This sample shows how to easily manipulate font in several different ways for rendering using SpriteBatch.
    /// The features of font described in here includes: 
    /// 1. Static font
    /// 2. Dynamic font with different size
    /// 3. Font styles {Bold, Italic}
    /// 4. Alias modes {Aliased, Anti-aliased, Clear}
    /// 5. Different languages supported
    /// 6. Three alignment modes {Left, Center, Right}
    /// 7. Animated text
    /// </summary>
    public class FontRenderer : SceneRendererBase
    {
        // Time to display text groups where the first index corresponding to introduction text, and the rest corresponding to text groups
        private static readonly float[] TimeToDisplayTextGroups = { 3f /*Intro*/, 5f /*Static*/, 5f /*Dynamic*/, 4f /*Style*/, 5f /*Alias*/,
                                                                    5f /*Language*/, 5f /*Alignment*/, 10f /*Animated*/};

        private readonly List<Action> screenRenderers = new List<Action>();

        private const float FadeInDuration = 1f;
        private const float FadeOutDuration = 1f;

        private const float DynamicFontContentSize = 50;

        private const string RefenceText = @"
In the first centuries of typesetting,
quotations were distinguished merely by
indicating the speaker, and this can still
be seen in some editions of the Bible.
During the Renaissance, quotations
were distinguished by setting in a typeface
contrasting with the main body text
(often Italic type with roman,
or the other way round).
Block quotations were set this way
at full size and full measure";

        private Vector2 centerVirtualPosition;
        private Vector2 screenSize;
        private SpriteBatch spriteBatch;

        public Texture Background;

        public SpriteFont StaticFont;
        public SpriteFont DynamicFont;
        public SpriteFont BoldFont;
        public SpriteFont ItalicFont;
        public SpriteFont AliasedFont;
        public SpriteFont AntialiasedFont;
        public SpriteFont ClearTypeFont;
        public SpriteFont JapaneseFont;
        public SpriteFont TimesNewRoman;
        public SpriteFont HeaderFont;
        
        private Vector2 animatedFontPosition;
        private float animatedFontScale;
        private float animatedFontRotation;
        private float animatedFontAlpha;
        private bool isPlaying = true;
        private int currentScreenIndex;
        private float currentTime;

        private readonly Vector2 headerPosition = new Vector2(0.5f, 0.25f);
        private readonly Vector2 contentPosition = new Vector2(0.5f, 0.4f);

        private readonly Color strideColor = new Color(0xFF3008da);

        private Vector2 virtualResolution = new Vector2(1920, 1080);

        private InputManager input;

        protected override void InitializeCore()
        {
            base.InitializeCore();

            input = Context.Services.GetServiceAs<InputManager>();

            // Create the SpriteBatch used to render them
            spriteBatch = new SpriteBatch(GraphicsDevice) { VirtualResolution = new Vector3(virtualResolution, 1000) };

            centerVirtualPosition = new Vector2(virtualResolution.X * 0.5f, virtualResolution.Y * 0.5f);
            screenSize = new Vector2(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height);

            screenRenderers.Add(DrawIntroductionCategory);
            screenRenderers.Add(DrawStaticCategory);
            screenRenderers.Add(DrawDynamicCategory);
            screenRenderers.Add(DrawStyleCategory);
            screenRenderers.Add(DrawAliasCategory);
            screenRenderers.Add(DrawLanguageCategory);
            screenRenderers.Add(DrawAlignmentCategory);
            screenRenderers.Add(DrawAnimationCategory);
        }

        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            // Clear
            drawContext.CommandList.Clear(drawContext.CommandList.RenderTarget, Color.Green);
            drawContext.CommandList.Clear(drawContext.CommandList.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);

            UpdateAnimatedFontParameters();
            UpdateInput();
            UpdateCurrentScreenIndex();

            if (isPlaying)
                currentTime += (float)context.Time.Elapsed.TotalSeconds;

            spriteBatch.Begin(drawContext.GraphicsContext);
            
            // Draw background
            var target = drawContext.CommandList.RenderTarget;
            var imageBufferMinRatio = Math.Min(Background.ViewWidth / (float)target.ViewWidth, Background.ViewHeight / (float)target.ViewHeight);
            var sourceSize = new Vector2(target.ViewWidth * imageBufferMinRatio, target.ViewHeight * imageBufferMinRatio);
            var source = new RectangleF((Background.ViewWidth - sourceSize.X) / 2, (Background.ViewHeight - sourceSize.Y) / 2, sourceSize.X, sourceSize.Y);
            spriteBatch.Draw(Background, new RectangleF(0, 0, virtualResolution.X, virtualResolution.Y), source, Color.White, 0, Vector2.Zero);

            screenRenderers[currentScreenIndex]();
            spriteBatch.End();
        }

        #region Draw Methods

        private void DrawHeader(string headerPart1, string headerPart2, string headerPart3)
        {
            const float headerSize = 70;

            var position = GetVirtualPosition(headerPosition);

            // Find the X position offset for the first part of text
            position -= spriteBatch.MeasureString(HeaderFont, headerPart1 + headerPart2 + headerPart3, headerSize, screenSize) * 0.5f;

            // Draw each part separately because we need to have a different color in the 2nd part
            spriteBatch.DrawString(HeaderFont, headerPart1, headerSize, position, Color.White * GetInterpolatedAlpha());

            position.X += spriteBatch.MeasureString(HeaderFont, headerPart1, headerSize, screenSize).X;

            spriteBatch.DrawString(HeaderFont, headerPart2, headerSize, position, strideColor * GetInterpolatedAlpha());

            position.X += spriteBatch.MeasureString(HeaderFont, headerPart2, headerSize, screenSize).X;

            spriteBatch.DrawString(HeaderFont, headerPart3, headerSize, position, Color.White * GetInterpolatedAlpha());
        }

        /// <summary>
        /// Draw "Introduction" text group.
        /// Render Stride SpriteFont sample introduction page.
        /// </summary>
        private void DrawIntroductionCategory()
        {
            // Draw Create {cross-platform} {game} {in C#} in three pieces separately
            const float textSize = 80;
            const string textPart1 = "Create cross-platform";
            const string textPart2 = " games ";
            const string textPart3 = "in C#";

            var position = GetVirtualPosition(0.5f, 0.5f);

            // Find the X position offset for the first part of text
            position -= spriteBatch.MeasureString(DynamicFont, textPart1 + textPart2 + textPart3, textSize, screenSize) * 0.5f;

            // Draw each part separately because we need to have a different color in the 2nd part
            spriteBatch.DrawString(DynamicFont, textPart1, textSize, position, Color.White * GetInterpolatedAlpha());

            position.X += spriteBatch.MeasureString(DynamicFont, textPart1, textSize, screenSize).X;

            spriteBatch.DrawString(DynamicFont, textPart2, textSize, position, strideColor * GetInterpolatedAlpha());

            position.X += spriteBatch.MeasureString(DynamicFont, textPart2, textSize, screenSize).X;

            spriteBatch.DrawString(DynamicFont, textPart3, textSize, position, Color.White * GetInterpolatedAlpha());
        }

        /// <summary>
        /// Draw "Static" text group.
        /// Render text created in compiling-time which could not change in run-time.
        /// </summary>
        private void DrawStaticCategory()
        {
            DrawHeader("Compile-time rendered ", "static", " fonts");

            var position = GetVirtualPosition(contentPosition);

            var text = "Embeds only required characters into the database\n" +
                       "Does not require any rendering time at execution\n" +
                       "Cannot adjust their size to the virtual resolution\n" +
                       "Cannot modify their size at run-time";

            position.X -= spriteBatch.MeasureString(StaticFont, text, screenSize).X / 2;

            spriteBatch.DrawString(StaticFont, text, position, Color.White * GetInterpolatedAlpha());
        }

        /// <summary>
        /// Draw "Dynamic" text group.
        /// Display text created dynamically in different sizes.
        /// </summary>
        private void DrawDynamicCategory()
        {
            DrawHeader("Run-time rendered ", "dynamic", " fonts");

            var text = "Embeds all characters of the font into the database\n" +
                       "Is rendered at execution time and requires some time for rendering\n" +
                       "Can adjust their size to the virtual resolution\n";

            var position = GetVirtualPosition(contentPosition);
            var firstTextSize = spriteBatch.MeasureString(DynamicFont, text, DynamicFontContentSize, screenSize);

            position.X -= firstTextSize.X / 2;

            spriteBatch.DrawString(DynamicFont, text, DynamicFontContentSize, position, Color.White * GetInterpolatedAlpha());

            text = "Can modify their size at execution time";

            position.Y += firstTextSize.Y;
            spriteBatch.DrawString(DynamicFont, text, 80, position, Color.White * GetInterpolatedAlpha());
        }

        /// <summary>
        /// Draw "Style" text group.
        /// Illustrate possible styles of font that can be rendered in compile-time {Italic, Bold}
        /// </summary>
        private void DrawStyleCategory()
        {
            DrawHeader("Support common font ", "styles", "");

            var position = GetVirtualPosition(contentPosition);

            var text = "None - This is a sample sentence.";
            var firstTextSize = spriteBatch.MeasureString(DynamicFont, text, DynamicFontContentSize, screenSize);
            position.X -= firstTextSize.X / 2;
            spriteBatch.DrawString(DynamicFont, text, DynamicFontContentSize, position, Color.White * GetInterpolatedAlpha());

            text = "Italic - This is a sample sentence.";
            position.Y += firstTextSize.Y;
            spriteBatch.DrawString(ItalicFont, text, DynamicFontContentSize, position, Color.White * GetInterpolatedAlpha());

            text = "Bold - This is a sample sentence.";
            position.Y += spriteBatch.MeasureString(ItalicFont, text, DynamicFontContentSize, screenSize).Y;
            spriteBatch.DrawString(BoldFont, text, DynamicFontContentSize, position, Color.White * GetInterpolatedAlpha());
        }

        /// <summary>
        /// Draw "Alias" text group.
        /// Display all three possible alias modes {Aliased, Anti-aliased, Clear type}.
        /// </summary>
        private void DrawAliasCategory()
        {
            DrawHeader("Support common ", "anti-aliasing", " modes");

            var position = GetVirtualPosition(contentPosition);

            var text = "Aliased - This is a sample sentence.";
            var firstTextSize = spriteBatch.MeasureString(AliasedFont, text, DynamicFontContentSize, screenSize);
            position.X -= firstTextSize.X / 2;
            spriteBatch.DrawString(AliasedFont, text, position, Color.White * GetInterpolatedAlpha());

            position.Y += firstTextSize.Y;
            text = "Anti-aliased - This is a sample sentence.";
            spriteBatch.DrawString(AntialiasedFont, text, position, Color.White * GetInterpolatedAlpha());

            position.Y += spriteBatch.MeasureString(AntialiasedFont, text, screenSize).Y;
            text = "Clear-type - This is a sample sentence.";
            spriteBatch.DrawString(ClearTypeFont, text, position, Color.White * GetInterpolatedAlpha());
        }

        /// <summary>
        /// Draw "Language" text group.
        /// Show Japanese dynamic font, by rendering a japanese paragraph.
        /// Other pictogram alphabets are supported as well.
        /// </summary>
        private void DrawLanguageCategory()
        {
            DrawHeader("Support ", "pictogram-based", " fonts");

            var sizeIncreament = 15;
            var scale = 0.8f;
            var position = GetVirtualPosition(contentPosition);
            var text = "Japanese dynamic sprite font\nあいうえおかきくけこ   天竜の\nアイウエオカキクケコ   幅八町の\n一二三四五六七八九十   梅雨濁り";

            position.X -= spriteBatch.MeasureString(JapaneseFont, text, scale * (DynamicFontContentSize + sizeIncreament), screenSize).X / 2;
            spriteBatch.DrawString(JapaneseFont, text, scale * (DynamicFontContentSize + sizeIncreament), position, Color.White * GetInterpolatedAlpha());
        }

        /// <summary>
        /// Draw "Alignment" text group.
        /// Display three paragraphs showing possible alignments {Left, Center, Right}
        /// </summary>
        private void DrawAlignmentCategory()
        {
            DrawHeader("Support standard ", "text alignment", " modes");

            var position = GetVirtualPosition(contentPosition);

            // Draw content
            position.X = virtualResolution.X * 0.03f;
            var text = "LEFT-ALIGNED TEXT\n" + RefenceText;

            var textSize = 28;

            spriteBatch.DrawString(TimesNewRoman, text, textSize, position, Color.White * GetInterpolatedAlpha());

            position.X = centerVirtualPosition.X - 0.5f * spriteBatch.MeasureString(TimesNewRoman, text, textSize, screenSize).X;
            text = "CENTERED TEXT\n" + RefenceText;

            spriteBatch.DrawString(TimesNewRoman, text, textSize, position, Color.White * GetInterpolatedAlpha(), TextAlignment.Center);

            position.X = virtualResolution.X - spriteBatch.MeasureString(TimesNewRoman, text, textSize, screenSize).X - virtualResolution.X * 0.03f;
            text = "RIGHT-ALIGNED TEXT\n" + RefenceText;

            spriteBatch.DrawString(TimesNewRoman, text, textSize, position, Color.White * GetInterpolatedAlpha(), TextAlignment.Right);
        }

        /// <summary>
        /// Draw "Animation" text group.
        /// Illustrate an animate text.
        /// </summary>
        private void DrawAnimationCategory()
        {
            DrawHeader("Easily ", "animate", " your texts!");

            // Draw content
            var text = "Stride Engine";

            spriteBatch.DrawString(DynamicFont, text, DynamicFontContentSize, animatedFontPosition, animatedFontAlpha * Color.White * GetInterpolatedAlpha(), animatedFontRotation,
                0.5f * spriteBatch.MeasureString(DynamicFont, text, DynamicFontContentSize, screenSize), animatedFontScale * Vector2.One, SpriteEffects.None, 0f, TextAlignment.Left);
        }

        #endregion Draw methods

        /// <summary>
        /// Check if there is any input command.
        /// Input commands are for controlling: 1. Text group advancing, 2. Previous/Next text group selection.
        /// </summary>
        /// <returns></returns>
        private void UpdateInput()
        {
            // Toggle play/not play
            if (input.IsKeyPressed(Keys.Space) || input.PointerEvents.Any(pointerEvent => pointerEvent.EventType == PointerEventType.Pressed))
            {
                isPlaying = !isPlaying;
            }
            else if (input.IsKeyPressed(Keys.Left) || input.IsKeyPressed(Keys.Right))
            {
                currentTime = 0;
                currentScreenIndex = (currentScreenIndex + (input.IsKeyPressed(Keys.Left) ? -1 : +1) + screenRenderers.Count) % screenRenderers.Count;
            }
        }

        private void UpdateCurrentScreenIndex()
        {
            var upperBound = TimeToDisplayTextGroups[currentScreenIndex];

            if (currentTime > upperBound)
            {
                currentTime = 0;
                currentScreenIndex = (currentScreenIndex + 1) % screenRenderers.Count;
            }
        }

        /// <summary>
        /// Update the main font parameters according to sample state.
        /// </summary>
        /// <returns></returns>
        private void UpdateAnimatedFontParameters()
        {
            if (!isPlaying)
                return;

            animatedFontAlpha = GetVaryingValue(1.6f * currentTime);
            animatedFontRotation = 2f * currentTime * (float)Math.PI;
            animatedFontPosition = GetVirtualPosition(0.5f, 0.65f) + 160 * new Vector2(1.5f * (float)Math.Cos(1.5f * currentTime), (float)Math.Sin(1.5f * currentTime));
            animatedFontScale = 0.9f + 0.2f * GetVaryingValue(2.5f * currentTime);
        }

        /// <summary>
        /// Return interpolated value for alpha channel of a text that controls opacity.
        /// Value, that is outside the bound, would not invisible.
        /// </summary>
        /// <returns></returns>
        private float GetInterpolatedAlpha()
        {
            var upperBound = TimeToDisplayTextGroups[currentScreenIndex];

            if (currentTime < FadeInDuration)
                return currentTime / FadeInDuration;

            if (currentTime < upperBound - FadeOutDuration)
                return 1f;

            return Math.Max(upperBound - currentTime, 0) / FadeOutDuration;
        }

        /// <summary>
        /// Return position in virtual resolution coordinate by given relative position [0, 1]
        /// </summary>
        /// <param name="relativePositionX"></param>
        /// <param name="relativePositionY"></param>
        /// <returns></returns>
        private Vector2 GetVirtualPosition(float relativePositionX, float relativePositionY)
        {
            return GetVirtualPosition(new Vector2(relativePositionX, relativePositionY));
        }

        /// <summary>
        /// Return position in virtual resolution coordinate by given relative position [0, 1]
        /// </summary>
        /// <returns></returns>
        private Vector2 GetVirtualPosition(Vector2 relativePosition)
        {
            return new Vector2(virtualResolution.X * relativePosition.X, virtualResolution.Y * relativePosition.Y);
        }

        /// <summary>
        /// Get a varying value between [0,1] depending on the time
        /// </summary>
        /// <param name="time">the current time</param>
        /// <returns>the varying value</returns>
        private static float GetVaryingValue(float time)
        {
            return (float)Math.Cos(time) * 0.5f + 0.5f;
        }
    }
}
