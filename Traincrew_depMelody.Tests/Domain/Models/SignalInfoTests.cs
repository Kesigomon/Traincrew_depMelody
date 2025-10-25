using FluentAssertions;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Tests.Domain.Models;

public class SignalInfoTests
{
    [Fact]
    public void IsOpen_ProceedAspect_ReturnsTrue()
    {
        // Arrange
        var signalInfo = new SignalInfo
        {
            Aspect = SignalAspect.Proceed
        };

        // Act & Assert
        signalInfo.IsOpen.Should().BeTrue();
    }

    [Fact]
    public void IsOpen_StopAspect_ReturnsFalse()
    {
        // Arrange
        var signalInfo = new SignalInfo
        {
            Aspect = SignalAspect.Stop
        };

        // Act & Assert
        signalInfo.IsOpen.Should().BeFalse();
    }
}
