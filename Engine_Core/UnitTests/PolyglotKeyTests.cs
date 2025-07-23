using Engine_Core;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests;

public class PolyglotKeyTests
{
    [Theory]
    [InlineData(0, 0x9D39247E33776D41)]
    public void GetPolyglotKey_FirstValidIndex_ReturnExceptedValue(int index, ulong expected)
    {
        var result = Polyglot.PolyglotRandomKeys[index];
        result.Should().Be(expected);   
    }

    [Fact]
    public void GetPolyglotKey_LastValidIndex_ReturnExceptedValue()
    {
        var result = Polyglot.PolyglotRandomKeys[^1];
        result.Should().Be(0xBE5CC29389B0A011);
    }

}
