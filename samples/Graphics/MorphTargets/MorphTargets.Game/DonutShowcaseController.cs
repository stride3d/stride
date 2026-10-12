using Stride.Animations;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace MorphTargets;

/// <summary>
/// Morph weight sliders for the donut (skinned), the cube and the icosphere (morph only), and the donut's clip with a
/// timeline that keeps its time across the glTF and FBX imports.
/// </summary>
[DataContract("DonutShowcaseController")]
public sealed class DonutShowcaseController : SyncScript
{
    private const string ClipName = "Clip";
    // Frame rate the donut was authored at, for the frame number next to the time.
    private const double FramesPerSecond = 30;
    private const float MinimumWeight = -1;
    private const float MaximumWeight = 2;
    private const float WeightSliderWidth = 210;
    private static readonly Color Animated = new(120, 180, 245);

    /// <summary>The donut entity placed in the scene, so its model and morph weights can be inspected in the editor.</summary>
    public ModelComponent Donut { get; set; }
    /// <summary>A cube with four morph targets and no skeleton.</summary>
    public ModelComponent Cube { get; set; }
    /// <summary>An icosphere with four morph targets and no skeleton.</summary>
    public ModelComponent Icosphere { get; set; }
    /// <summary>Morph weight clip of the cube.</summary>
    public AnimationClip CubeAnimation { get; set; }
    /// <summary>Morph weight clip of the icosphere.</summary>
    public AnimationClip IcosphereAnimation { get; set; }
    public Model Model { get; set; }
    public AnimationClip Animation { get; set; }
    /// <summary>The same donut imported from FBX, with its own clip.</summary>
    public Model FbxModel { get; set; }
    public AnimationClip FbxAnimation { get; set; }
    public Material Material { get; set; }
    public SpriteFont Font { get; set; }

    private ModelComponent donut;
    // The model the morph sliders edit: the donut, the cube or the icosphere.
    private ModelComponent selected;
    private StackPanel animationControls;
    private StackPanel sourceHost;
    // The clip each model plays, by model.
    private readonly Dictionary<ModelComponent, AnimationClip> clipByModel = new();
    private StackPanel sliderHost;
    private SampleUI ui;
    private AnimationComponent animation;
    private AnimationClip clip;
    private FrameTimings timings;
    private TextBlock[] stats;
    private Slider[] sliders = Array.Empty<Slider>();
    private TextBlock[] valueLabels = Array.Empty<TextBlock>();
    private TextBlock[] nameLabels = Array.Empty<TextBlock>();
    private TextBlock playLabel;
    private TextBlock timeLabel;
    private Slider timeline;
    private bool updatingSliders;
    // Sliders are bound to target names: FBX and glTF imports order their targets differently.
    private string[] names = Array.Empty<string>();

    // Applied: the clip drives the pose, running or paused. Not applied: the sliders set the weights by hand.
    private PlayingAnimation Applied => animation.PlayingAnimations.Count > 0 ? animation.PlayingAnimations[0] : null;
    private bool Running => Applied is { TimeFactor: not 0 };
    private double Duration => clip?.Duration.TotalSeconds ?? 0;

    public override void Start()
    {
        if (Model == null)
            throw new InvalidOperationException("Assign Model on the DonutShowcaseController.");
        // Vsync on; the benchmark scenes turn it off.
        GraphicsDevice.Presenter.PresentInterval = PresentInterval.One;

        if (Donut == null)
            throw new InvalidOperationException("Assign Donut on the DonutShowcaseController.");
        donut = Donut;
        clipByModel[donut] = Animation;
        if (Cube != null)
            clipByModel[Cube] = CubeAnimation;
        if (Icosphere != null)
            clipByModel[Icosphere] = IcosphereAnimation;
        // Every model starts playing its clip.
        foreach (var model in clipByModel.Keys.Where(model => model != donut))
        {
            Focus(model);
            UseClip(clipByModel[model], null, false, TimeSpan.Zero);
            TogglePlay();
        }
        Focus(donut);
        SetSource(Model, Animation);
        CreateUI();
        timings = new FrameTimings(this);
        TogglePlay();
    }

    public override void Update()
    {
        timings.Report(stats);
        var applied = Applied;
        if (applied == null)
            return;
        double time = Duration > 0 ? applied.CurrentTime.TotalSeconds % Duration : 0;
        timeLabel.Text = FormatTime(time);

        // While the clip drives the pose, the sliders follow the animated weights and time.
        updatingSliders = true;
        timeline.Value = (float)time;
        for (int i = 0; i < sliders.Length; i++)
        {
            float weight = Weight(i);
            sliders[i].Value = weight;
            valueLabels[i].Text = Format(weight);
            // Highlight the targets the clip actually moves.
            if (MathF.Abs(weight) > 0.001f)
                nameLabels[i].TextColor = Animated;
        }
        updatingSliders = false;
    }

    public override void Cancel() => timings?.Dispose();

    private float Weight(int index) => selected.FindMorphTarget(names[index]) is var slot and >= 0 ? selected.GetMorphWeight(slot) : 0;

    private static string FormatTime(double seconds) => $"{seconds:0.00} s  f{seconds * FramesPerSecond:0}";

    /// <summary>Swaps the donut's model and clip, keeping the weights by target name and the clip's time and play state.</summary>
    private void SetSource(Model model, AnimationClip source)
    {
        var applied = Applied;
        bool running = Running;
        var time = applied?.CurrentTime ?? TimeSpan.Zero;
        var weights = names.Select((_, i) => Weight(i)).ToArray();
        animation.PlayingAnimations.Clear();
        donut.Model = model;
        donut.Materials.Clear();
        if (Material != null)
        {
            for (int i = 0; i < Math.Max(1, model.Materials.Count); i++)
                donut.Materials[i] = Material;
        }
        for (int i = 0; i < names.Length; i++)
        {
            if (donut.FindMorphTarget(names[i]) is var slot and >= 0)
                donut.SetMorphWeight(slot, weights[i]);
        }
        UseClip(source, applied, running, time);
    }

    /// <summary>Points <see cref="animation"/> and <see cref="clip"/> at a model's animation component and its clip.</summary>
    private void Focus(ModelComponent model)
    {
        selected = model;
        animation = model.Entity.GetOrCreate<AnimationComponent>();
        clip = animation.Animations.TryGetValue(ClipName, out var current) ? current : null;
    }

    /// <summary>Makes <paramref name="source"/> the clip, applied again at <paramref name="time"/> if a clip was applied.</summary>
    private void UseClip(AnimationClip source, PlayingAnimation applied, bool running, TimeSpan time)
    {
        clip = source;
        animation.Animations.Clear();
        if (clip != null)
            animation.Animations.Add(ClipName, clip);
        if (applied != null && clip != null)
            Apply(TimeSpan.FromSeconds(time.TotalSeconds % Math.Max(Duration, 1e-6)), running);
        if (timeline != null)
            timeline.Maximum = (float)Duration;
    }

    /// <summary>Lets the clip drive the pose from <paramref name="time"/>, running or paused there.</summary>
    private void Apply(TimeSpan time, bool running)
    {
        var applied = Applied ?? animation.Play(ClipName);
        applied.CurrentTime = time;
        applied.TimeFactor = running ? 1 : 0;
        if (playLabel != null)
            playLabel.Text = running ? "Pause" : "Play";
    }

    private void TogglePlay()
    {
        if (clip == null)
            return;
        Apply(Applied?.CurrentTime ?? TimeSpan.Zero, !Running);
    }

    /// <summary>Pauses the clip at the timeline's time.</summary>
    private void Seek(float seconds)
    {
        if (clip != null)
            Apply(TimeSpan.FromSeconds(seconds), false);
    }

    /// <summary>Stops the clip so the morph sliders set the weights by hand.</summary>
    private void Release()
    {
        animation.PlayingAnimations.Clear();
        playLabel.Text = "Play";
    }

    private void Reset()
    {
        selected.SetAllMorphWeights(0);
        updatingSliders = true;
        Release();
        timeline.Value = 0;
        timeLabel.Text = FormatTime(0);
        for (int slot = 0; slot < sliders.Length; slot++)
        {
            sliders[slot].Value = 0;
            valueLabels[slot].Text = Format(0);
            nameLabels[slot].TextColor = Color.White;
        }
        updatingSliders = false;
    }

    private static string Format(float value) => value.ToString("+0.00;-0.00");

    /// <summary>Points the morph sliders at another model; the clip controls only apply to the donut.</summary>
    private void Select(ModelComponent model)
    {
        Focus(model);
        animationControls.Visibility = clipByModel.GetValueOrDefault(model) != null ? Visibility.Visible : Visibility.Collapsed;
        sourceHost.Visibility = model == donut ? Visibility.Visible : Visibility.Collapsed;
        playLabel.Text = Running ? "Pause" : "Play";
        timeline.Maximum = (float)Duration;
        timeLabel.Text = FormatTime(Applied?.CurrentTime.TotalSeconds ?? 0);
        CreateSliders();
    }

    private void CreateSliders()
    {
        names = selected.MorphTargetNames.ToArray();
        var grid = new UniformGrid { Columns = 1, Rows = Math.Max(1, names.Length) };
        sliders = new Slider[names.Length];
        valueLabels = new TextBlock[names.Length];
        nameLabels = new TextBlock[names.Length];
        for (int slot = 0; slot < names.Length; slot++)
        {
            var name = nameLabels[slot] = ui.Text(names[slot], 14, Color.White, Thickness.UniformCuboid(0));
            name.Width = 80;
            var value = valueLabels[slot] = ui.Text(Format(Weight(slot)), 14, Color.White, new Thickness(8, 0, 0, 0));
            value.Width = 52;
            value.TextAlignment = TextAlignment.Right;
            var slider = sliders[slot] = ui.Slider(MinimumWeight, MaximumWeight, Weight(slot), WeightSliderWidth, bipolar: true);
            slider.Step = 0.01f;
            slider.Margin = Thickness.UniformCuboid(0);
            // A tick at zero, drawn under the slider: zero is a third of the way along, not at the start.
            var zeroTick = new Border
            {
                BackgroundColor = SampleUI.Muted,
                Width = 2,
                Height = 14,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(WeightSliderWidth * -MinimumWeight / (MaximumWeight - MinimumWeight) - 1, 0, 0, 0),
                CanBeHitByUser = false,
            };
            var track = new Grid { Width = WeightSliderWidth };
            track.Children.Add(zeroTick);
            track.Children.Add(slider);
            var index = slot;
            slider.ValueChanged += (_, _) =>
            {
                if (updatingSliders)
                    return;
                // Editing a weight by hand takes the pose back from the clip, running or paused.
                if (Applied != null)
                    Release();
                if (selected.FindMorphTarget(names[index]) is var target and >= 0)
                    selected.SetMorphWeight(target, slider.Value);
                value.Text = Format(slider.Value);
            };

            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };
            row.Children.Add(name);
            row.Children.Add(track);
            row.Children.Add(value);
            row.DependencyProperties.Set(GridBase.RowPropertyKey, slot);
            grid.Children.Add(row);
        }
        sliderHost.Children.Clear();
        sliderHost.Children.Add(grid);
    }

    private void CreateUI()
    {
        ui = new SampleUI(this, Font);
        var panel = new StackPanel { Orientation = Orientation.Vertical, Width = 360 };
        panel.Children.Add(ui.Heading("Morph targets"));
        // Left to right, as the models stand in the scene.
        var models = new[] { donut, Cube, Icosphere }.Where(model => model != null).OrderBy(model => model.Entity.Transform.Position.X).ToArray();
        if (models.Length > 1)
            ui.RadioGroup(panel, "Mesh", models.Select(model => model.Entity.Name).ToArray(), Array.IndexOf(models, donut), index => Select(models[index]));

        var weightsHeader = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        weightsHeader.ColumnDefinitions.Add(new StripDefinition(StripType.Star));
        weightsHeader.ColumnDefinitions.Add(new StripDefinition(StripType.Auto));
        weightsHeader.Children.Add(ui.Section("Weights"));
        var reset = ui.Button(ui.Label("Reset", 13), Reset);
        reset.Padding = new Thickness(8, 2, 8, 2);
        reset.Margin = new Thickness(0, 8, 0, 2);
        reset.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
        weightsHeader.Children.Add(reset);
        panel.Children.Add(weightsHeader);
        panel.Children.Add(sliderHost = new StackPanel { Orientation = Orientation.Vertical });
        CreateSliders();

        // The selected model's clip and timeline; the glTF/FBX import is donut only.
        animationControls = new StackPanel { Orientation = Orientation.Vertical };
        animationControls.Children.Add(ui.Section("Animation"));
        var transport = new StackPanel { Orientation = Orientation.Horizontal };
        var play = ui.Button(playLabel = ui.Label("Play", 14), TogglePlay);
        play.Width = 64;
        play.Margin = new Thickness(0, 0, 8, 0);
        playLabel.HorizontalAlignment = HorizontalAlignment.Center;
        transport.Children.Add(play);
        timeline = ui.Slider(0, (float)Duration, 0, 200);
        timeline.Step = (float)(1 / FramesPerSecond / 10);
        timeline.ValueChanged += (_, _) =>
        {
            if (updatingSliders)
                return;
            Seek(timeline.Value);
            timeLabel.Text = FormatTime(timeline.Value);
        };
        transport.Children.Add(timeline);
        transport.Children.Add(timeLabel = ui.Text(FormatTime(0), 13, SampleUI.Muted, new Thickness(8, 0, 0, 0)));
        animationControls.Children.Add(transport);
        animationControls.Children.Add(sourceHost = new StackPanel { Orientation = Orientation.Vertical });
        if (FbxModel != null)
            ui.RadioGroup(sourceHost, "Source", ["glTF", "FBX"], 0, index => SetSource(index == 0 ? Model : FbxModel, index == 0 ? Animation : FbxAnimation));
        panel.Children.Add(animationControls);

        panel.Children.Add(ui.Text("F1 hides the panels", 12, SampleUI.Muted, new Thickness(0, 10, 0, 0)));
        SampleUI.Show(this, ui.Panel(panel, HorizontalAlignment.Left), ui.TimingsPanel(out stats));
    }
}
