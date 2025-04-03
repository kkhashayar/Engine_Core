using Engine_Core;
using FluentAssertions;

namespace UnitTests;


public class SetBitCountTests
{

    /*
     *      ********************* Brian Kernighan’s Way Bit Hack for Counting Set Bits *********************
     *      
     *      bitboard &= bitboard -1;
     *      by incrementing one set bit in each loop until there is no set bit left
     *      that's why we dont need to loop the whole chain and we have faster counter.
     *      
     */

    [Theory]
    [InlineData(0b0UL, 0)]
    [InlineData(0b1UL, 1)]
    [InlineData(0b1011UL, 3)]
    [InlineData(0b11111111UL, 8)]
    [InlineData(0xFFFFFFFFFFFFFFFFUL, 64)]
    public void CountBits_Returns_Correct_Set_Bit_Count(ulong bitboard, int expectedCountNumber)
    {
        var sut = Globals.CountBits(bitboard);
        sut.Should().Be(expectedCountNumber);
    }


    [Theory]
    [InlineData(0b0UL, 0)]
    [InlineData(0b1UL, 1)]
    [InlineData(0b1011UL, 3)]
    [InlineData(0b11111111UL, 8)]
    [InlineData(0xFFFFFFFFFFFFFFFFUL, 64)]
    public void CountBits_Returns_Correct_Set_Bit_Count_Classic(ulong bitboard, int expectedCountNumber)
    {
        var sut = Globals.CountSetBitClassic(bitboard);     
        sut.Should().Be(expectedCountNumber);
    }

}



