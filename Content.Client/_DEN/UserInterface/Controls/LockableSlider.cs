using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client._DEN.UserInterface.Controls;

public sealed class LockableSlider : Slider
{
    public float LockMaxValue;
    public float LockMinValue;
    
    public new event Action<Slider>? OnGrabbed;
    public new event Action<Slider>? OnReleased;

    private bool _grabbed;

    public bool Grabbed => _grabbed;

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick || Disabled)
        {
            return;
        }

        HandlePositionChange(args.RelativePosition);
        _grabbed = true;
        OnGrabbed?.Invoke(this);
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function != EngineKeyFunctions.UIClick || !_grabbed) return;

        _grabbed = false;
        OnReleased?.Invoke(this);
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        if (!_grabbed)
        {
            return;
        }

        HandlePositionChange(args.RelativePosition);
    }

    private void HandlePositionChange(Vector2 position)
    {
        var grabberWidth = _grabber.DesiredSize.X;
        var ratio = (position.X - grabberWidth / 2) / (Width - grabberWidth);
        var value = MathHelper.Clamp(ratio * (MaxValue - MinValue) + MinValue, LockMinValue, LockMaxValue);
        Value = value;
    }
}