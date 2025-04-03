using Engine_Core;

namespace UnitTests;

public class SquareToCoordinateTests
{
    [Fact]
    public void SquareToCoordinate_Throws_OutOfRTange_Exception()
    {
        Assert.Throws<IndexOutOfRangeException>(() =>
        {
            var _ = Globals.SquareToCoordinates[65];
        });
    }


    [Theory]
    [InlineData(0, "a8")]
    [InlineData(7, "h8")]
    [InlineData(8, "a7")]
    [InlineData(63, "h1")]
    public void SquareToCoordinate_Returns_Correct_Coordinate(int squareIndex, string expectedCoordinate)
    {
        var sut = Globals.SquareToCoordinates[squareIndex];

        Assert.Equal(expectedCoordinate, sut);
    }
}
