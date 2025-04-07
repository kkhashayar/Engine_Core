using Engine_Core;
using FluentAssertions;

namespace UnitTests;

public class PolyGlotTests
{
    [Fact]
    public void ReadPolyglotBook_ShouldLoadEntries()
    {
        // Arrange 

        var path = "D:\\Data\\Repo\\K_Chess_2\\komodo.bin";

        var hash = 0ul;
        var sut = IO.ReadPolyglotBook(path, hash);
        // Act 

        var entries = sut;

        // Assert 

        entries.Should().BeOfType<List<IO.PolyglotEntry>>();   
    }
}
