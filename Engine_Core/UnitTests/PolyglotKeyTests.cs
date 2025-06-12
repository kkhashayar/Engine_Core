using Engine_Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests;

public class PolyglotKeyTests
{
    [Fact]
    public void GetPolyglotKey_FirstValidIndex_ReturnExceptedValue()
    {
        // Arrange 
        ulong expected = 0x9D39247E33776D41;
        // Act
        var sut = Globals.GetPolyglotKey(0);
        // Assert
        Assert.Equal(expected, sut);    
    }
    [Fact]
    public void GetPolyglotKey_LastValidIndex_ReturnExceptedValue()
    {
        // Arrange 
        var lastIndex = Polyglot.PolyglotRandomKeys.Count() - 1;  
        ulong expected = 0xBE5CC29389B0A011;
        // Act
        var sut = Globals.GetPolyglotKey(lastIndex);
        // Assert
        Assert.Equal(expected, sut);
    }



}
