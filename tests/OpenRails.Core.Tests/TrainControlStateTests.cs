using OpenRails.Core.Simulation;
using Xunit;

namespace OpenRails.Core.Tests;

public class TrainControlStateTests
{
    [Fact]
    public void Throttle_and_brake_are_clamped_and_notify_once_per_change()
    {
        var state = new TrainControlState();
        var changes = 0;
        state.Changed += (_, _) => changes++;

        state.Throttle = 0.4f;
        state.Throttle = 0.4f;
        state.Brake = 2f;
        state.Brake = -1f;

        Assert.Equal(0.4f, state.Throttle);
        Assert.Equal(0f, state.Brake);
        Assert.Equal(3, changes);
    }

    [Fact]
    public void Direction_toggles_between_forward_and_reverse()
    {
        var state = new TrainControlState();

        Assert.Equal(TrainDirection.Forward, state.Direction);
        Assert.Equal(1, state.DirectionSign);

        state.ToggleDirection();
        Assert.Equal(TrainDirection.Reverse, state.Direction);
        Assert.Equal(-1, state.DirectionSign);

        state.ToggleDirection();
        Assert.Equal(TrainDirection.Forward, state.Direction);
    }

    [Fact]
    public void Speed_kph_is_absolute()
    {
        var state = new TrainControlState { SpeedMetersPerSecond = -10f };

        Assert.Equal(36f, state.SpeedKph, 3);
    }
}
