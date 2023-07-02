using Sajuuk.Utils;

namespace Sajuuk.Tests.Utils;

public class TimeUtilsTests {
    [Theory]
    [InlineData(0, "00:00")]
    [InlineData(22, "00:00")]
    [InlineData(23, "00:01")]
    [InlineData(TimeUtils.FramesPerSecond * 60, "01:00")]
    [InlineData(TimeUtils.FramesPerSecond * 72, "01:12")]
    [InlineData(TimeUtils.FramesPerSecond * 60 * 134, "134:00")]
    public void GivenACertainFrame_WhenGetGameTimeString_ThenReturnsTimeFormattedWithMinutesAndSeconds(uint frame, string expected) {
        // Act
        var timeString = TimeUtils.GetGameTimeString(frame);

        // Assert
        Assert.Equal(expected, timeString);
    }
}
