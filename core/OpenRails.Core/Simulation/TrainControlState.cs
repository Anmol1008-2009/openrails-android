namespace OpenRails.Core.Simulation;

public enum TrainDirection
{
    Forward = 1,
    Reverse = -1,
}

/// <summary>
/// Portable cab-control state shared by the touch overlay and the 3D view.
/// Throttle and brake are normalized 0..1; speed is written by the simulation.
/// </summary>
public sealed class TrainControlState
{
    private float _throttle;
    private float _brake;
    private TrainDirection _direction = TrainDirection.Forward;
    private float _speedMetersPerSecond;

    public event EventHandler? Changed;

    public float Throttle
    {
        get => _throttle;
        set => SetNormalized(ref _throttle, value);
    }

    public float Brake
    {
        get => _brake;
        set => SetNormalized(ref _brake, value);
    }

    public TrainDirection Direction
    {
        get => _direction;
        private set
        {
            if (_direction == value)
            {
                return;
            }

            _direction = value;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public int DirectionSign => _direction == TrainDirection.Forward ? 1 : -1;

    /// <summary>Signed speed in simulation units per second, set by the renderer.</summary>
    public float SpeedMetersPerSecond
    {
        get => _speedMetersPerSecond;
        set => _speedMetersPerSecond = value;
    }

    public float SpeedKph => MathF.Abs(_speedMetersPerSecond) * 3.6f;

    public void ToggleDirection() =>
        Direction = _direction == TrainDirection.Forward
            ? TrainDirection.Reverse
            : TrainDirection.Forward;

    private void SetNormalized(ref float field, float value)
    {
        var clamped = Math.Clamp(value, 0f, 1f);
        if (MathF.Abs(field - clamped) < 0.0001f)
        {
            return;
        }

        field = clamped;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
