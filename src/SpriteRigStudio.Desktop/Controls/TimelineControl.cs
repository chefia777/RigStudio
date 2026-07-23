using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System.Globalization;
using SpriteRigStudio.Domain.Animations;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Skeletons;

namespace SpriteRigStudio.Desktop.Controls;

/// <summary>
/// Displays animation bone tracks and lets the user scrub the playhead or select keyframes.
/// </summary>
public sealed class TimelineControl : Control
{
    private const double LabelWidth = 116;
    private const double RulerHeight = 20;
    private const double RowHeight = 24;
    private AnimationClipDefinition? _animation;
    private SkeletonDefinition? _skeleton;
    private double _currentTime;
    private BoneId? _selectedBoneId;
    private double? _selectedKeyframeTime;
    private bool _isScrubbing;

    public event Action<double>? TimeChanged;
    public event Action<BoneId>? BoneSelected;

    public AnimationClipDefinition? Animation
    {
        get => _animation;
        set { _animation = value; InvalidateVisual(); }
    }

    public SkeletonDefinition? Skeleton
    {
        get => _skeleton;
        set { _skeleton = value; InvalidateVisual(); }
    }

    public double CurrentTime
    {
        get => _currentTime;
        set { _currentTime = Math.Max(0, value); InvalidateVisual(); }
    }

    public BoneId? SelectedBoneId
    {
        get => _selectedBoneId;
        set { _selectedBoneId = value; InvalidateVisual(); }
    }

    public double? SelectedKeyframeTime
    {
        get => _selectedKeyframeTime;
        set { _selectedKeyframeTime = value; InvalidateVisual(); }
    }

    public TimelineControl()
    {
        Focusable = true;
        ClipToBounds = true;
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
    }

    protected override Size MeasureOverride(Size availableSize) => availableSize;

    protected override Size ArrangeOverride(Size finalSize) => finalSize;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(Brushes.Transparent, Bounds);

        var duration = _animation?.DurationSeconds ?? 0;
        if (duration <= 0 || Bounds.Width <= LabelWidth) return;

        var tracks = GetTracks();
        var timelineWidth = Bounds.Width - LabelWidth;
        var timeToX = (double time) => LabelWidth + Math.Clamp(time / duration, 0, 1) * timelineWidth;

        context.FillRectangle(new SolidColorBrush(Color.FromRgb(38, 38, 42)),
            new Rect(LabelWidth, 0, timelineWidth, Bounds.Height));
        context.FillRectangle(new SolidColorBrush(Color.FromRgb(48, 48, 52)),
            new Rect(0, 0, LabelWidth, Bounds.Height));

        DrawRuler(context, duration, timeToX);

        for (var row = 0; row < tracks.Count; row++)
        {
            var y = RulerHeight + row * RowHeight;
            var track = tracks[row];
            var isSelected = _selectedBoneId == track.BoneId;
            if (isSelected)
                context.FillRectangle(new SolidColorBrush(Color.FromArgb(48, 70, 110, 160)),
                    new Rect(0, y, Bounds.Width, RowHeight));

            context.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(70, 70, 76)), 1),
                new Point(LabelWidth, y + RowHeight / 2),
                new Point(Bounds.Width, y + RowHeight / 2));
            context.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(60, 60, 65)), 1),
                new Point(LabelWidth - 1, y), new Point(LabelWidth - 1, y + RowHeight));

            var label = GetBoneName(track.BoneId);
            context.DrawText(new FormattedText(
                label,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                12,
                Brushes.White),
                new Point(6, y + 4));

            foreach (var keyframe in track.Keyframes)
            {
                var x = timeToX(keyframe.TimeSeconds);
                var selected = isSelected && _selectedKeyframeTime.HasValue &&
                               Math.Abs(_selectedKeyframeTime.Value - keyframe.TimeSeconds) < 0.001;
                var brush = selected ? Brushes.Orange : Brushes.Gold;
                context.DrawEllipse(brush, null, new Point(x, y + RowHeight / 2), 5, 5);
            }
        }

        var playheadX = timeToX(_currentTime);
        context.DrawLine(new Pen(Brushes.IndianRed, 2),
            new Point(playheadX, 0), new Point(playheadX, Bounds.Height));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        Focus();
        _isScrubbing = true;
        e.Pointer.Capture(this);
        UpdateFromPointer(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_isScrubbing)
        {
            UpdateFromPointer(e.GetPosition(this));
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!_isScrubbing) return;
        _isScrubbing = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void UpdateFromPointer(Point point)
    {
        var duration = _animation?.DurationSeconds ?? 0;
        if (duration <= 0 || Bounds.Width <= LabelWidth) return;

        var timelineWidth = Bounds.Width - LabelWidth;
        var time = Math.Clamp((point.X - LabelWidth) / timelineWidth, 0, 1) * duration;
        var keyframe = FindKeyframe(point, time);
        if (keyframe.HasValue)
        {
            _selectedBoneId = keyframe.Value.BoneId;
            _selectedKeyframeTime = keyframe.Value.Time;
            BoneSelected?.Invoke(keyframe.Value.BoneId);
        }
        else
        {
            _selectedKeyframeTime = null;
        }

        _currentTime = time;
        InvalidateVisual();
        TimeChanged?.Invoke(time);
    }

    private (BoneId BoneId, double Time)? FindKeyframe(Point point, double time)
    {
        var tracks = GetTracks();
        var row = (int)Math.Floor((point.Y - RulerHeight) / RowHeight);
        if (row < 0 || row >= tracks.Count) return null;

        var track = tracks[row];
        var duration = _animation?.DurationSeconds ?? 1;
        var x = LabelWidth + Math.Clamp(time / duration, 0, 1) * (Bounds.Width - LabelWidth);
        var keyframe = track.Keyframes.FirstOrDefault(k => Math.Abs(k.TimeSeconds - time) <= 0.04 * duration);
        return keyframe != null && Math.Abs(x - (LabelWidth + keyframe.TimeSeconds / duration * (Bounds.Width - LabelWidth))) <= 9
            ? (track.BoneId, keyframe.TimeSeconds)
            : null;
    }

    private List<BoneTrack> GetTracks()
    {
        if (_skeleton != null)
        {
            return _skeleton.Bones.Values
                .Select(bone => _animation?.BoneTracks.TryGetValue(bone.BoneId.ToKeyString(), out var track) == true
                    ? track
                    : new BoneTrack { BoneId = bone.BoneId })
                .ToList();
        }

        return _animation?.BoneTracks.Values.OrderBy(track => track.BoneId.ToKeyString()).ToList() ?? new List<BoneTrack>();
    }

    private string GetBoneName(BoneId boneId)
    {
        return _skeleton?.Bones.TryGetValue(boneId.ToKeyString(), out var bone) == true
            ? bone.Name
            : boneId.ToKeyString()[..8];
    }

    private void DrawRuler(DrawingContext context, double duration, Func<double, double> timeToX)
    {
        var rulerPen = new Pen(new SolidColorBrush(Color.FromRgb(105, 105, 112)), 1);
        var fps = Math.Max(1, _animation?.FramesPerSecond ?? 12);
        var frameCount = Math.Min((int)Math.Ceiling(duration * fps), 240);
        for (var frame = 0; frame <= frameCount; frame++)
        {
            var time = Math.Min(duration, frame / (double)fps);
            var x = timeToX(time);
            var major = frame % fps == 0;
            context.DrawLine(rulerPen, new Point(x, RulerHeight - (major ? 10 : 5)), new Point(x, RulerHeight));
            if (major)
            {
                context.DrawText(new FormattedText(
                    $"{time:F1}s",
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    11,
                    Brushes.White),
                    new Point(x + 2, 2));
            }
        }

        context.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(85, 85, 90)), 1),
            new Point(0, RulerHeight), new Point(Bounds.Width, RulerHeight));
    }
}
