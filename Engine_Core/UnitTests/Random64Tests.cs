using Engine_Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests;

public class Random64Tests
{

    [Fact]
    public void GetFixedRandom64_ShouldReturnValidUlong()
    {
        ulong value = Globals.GetFixedRandom64Numbers();

        Assert.True(value > 0);
    }

    [Fact]
    public void GetFixedRandom64_ShouldReturnDifferentValues()
    {
        // Arrange --> Nothing to arrange

        // Act 
        ulong value1 = Globals.GetFixedRandom64Numbers();
        ulong Valie2 = Globals.GetFixedRandom64Numbers();

        // Assert 

        Assert.NotEqual(value1, Valie2);
    }


    [Fact]
    public void GetFixedRandom64_ShouldHaveNonZeroHighBits()
    {
        ulong value = Globals.GetFixedRandom64Numbers();
        ulong highBits = value >> 32;
        Assert.NotEqual(value, highBits);
    }
}
