using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Core.Diagnostics;
using Stride.Input;
using Stride.Engine;
using Stride.Profiling;
using Stride.Core;

namespace ##Namespace##
{
    public class ##Scriptname## : AsyncScript
    {
        /// <summary>
        /// Enables or disable the game profiling
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// The color of the text displayed during profiling
        /// </summary>
        [Display(4, "Text color")]
        public Color TextColor { get; set; } = Color.LightGreen;

        /// <summary>
        /// The time between two refreshes of the profiling information in milliseconds.
        /// </summary>
        [Display(2, "Refresh interval (ms)")]
        public double RefreshTime { get; set; } = 500;

        /// <summary>
        /// Gets or set the sorting mode of the profiling entries
        /// </summary>
        [Display(1, "Sort by")]
        public GameProfilingSorting SortingMode { get; set; } = GameProfilingSorting.ByTime;

        /// <summary>
        /// Gets or sets the type of the profiling to display: CPU or GPU
        /// </summary>
        [Display(0, "Filter")]
        public GameProfilingResults FilteringMode { get; set; } = GameProfilingResults.Fps;

        /// <summary>
        /// Gets or sets the current profiling result page to display.
        /// </summary>
        [Display(3, "Display page")]
        public uint ResultPage { get; set; } = 1;

        public override async Task Execute()
        {
            if (Enabled)
                GameProfiler.EnableProfiling();

            while (Game.IsRunning)
            {
                GameProfiler.TextColor = TextColor;
                GameProfiler.RefreshTime = RefreshTime;
                GameProfiler.SortingMode = SortingMode;
                GameProfiler.FilteringMode = FilteringMode;
                GameProfiler.CurrentResultPage = ResultPage;
                ResultPage = GameProfiler.CurrentResultPage;

                if (Input.IsKeyDown(Keys.LeftShift) && Input.IsKeyDown(Keys.LeftCtrl) && Input.IsKeyReleased(Keys.P))
                {
                    if (Enabled)
                    {
                        GameProfiler.DisableProfiling();
                        Enabled = false;
                    }
                    else
                    {
                        GameProfiler.EnableProfiling();
                        Enabled = true;
                    }
                }

                if (Enabled)
                {
                    // Toggle the filtering mode
                    if (Input.IsKeyPressed(Keys.F1))
                    {
                        FilteringMode = (GameProfilingResults)(((int)FilteringMode + 1) % Enum.GetValues(typeof(GameProfilingResults)).Length);
                    }
                    // Toggle the sorting mode
                    if (Input.IsKeyPressed(Keys.F2))
                    {
                        SortingMode = (GameProfilingSorting)(((int)SortingMode + 1) % Enum.GetValues(typeof(GameProfilingSorting)).Length);
                    }

                    // Update the result page
                    if (Input.IsKeyPressed(Keys.F3))
                    {
                        ResultPage = Math.Max(1, --ResultPage);
                    }
                    else if (Input.IsKeyPressed(Keys.F4))
                    {
                        ++ResultPage;
                    }
                    if (Input.IsKeyPressed(Keys.D1))
                    {
                        ResultPage = 1;
                    }
                    else if (Input.IsKeyPressed(Keys.D2))
                    {
                        ResultPage = 2;
                    }
                    else if (Input.IsKeyPressed(Keys.D3))
                    {
                        ResultPage = 3;
                    }
                    else if (Input.IsKeyPressed(Keys.D4))
                    {
                        ResultPage = 4;
                    }
                    else if (Input.IsKeyPressed(Keys.D5))
                    {
                        ResultPage = 5;
                    }

                    // Update the refreshing speed
                    if (Input.IsKeyPressed(Keys.Subtract) || Input.IsKeyPressed(Keys.OemMinus))
                    {
                        RefreshTime = Math.Min(RefreshTime * 2, 10000);
                    }
                    else if (Input.IsKeyPressed(Keys.Add) || Input.IsKeyPressed(Keys.OemPlus))
                    {
                        RefreshTime = Math.Max(RefreshTime / 2, 100);
                    }
                }

                await Script.NextFrame();
            }
        }
    }
}
