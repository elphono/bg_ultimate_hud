namespace BronzebeardHud.GameState.Tests;

public class CardDbTests
{
    [Fact]
    public void CardName_KnownHero()
    {
        Assert.Equal("Varden Dawngrasp", CardDb.CardName("BG22_HERO_004"));
    }

    [Fact]
    public void CardName_KnownMinion()
    {
        Assert.Equal("Dune Dweller", CardDb.CardName("BG31_815"));
    }

    [Fact]
    public void CardName_Unknown_ReturnsNull()
    {
        Assert.Null(CardDb.CardName("TOTALLY_FAKE_CARD"));
    }

    [Fact]
    public void DisplayName_Fallback()
    {
        Assert.Equal("Varden Dawngrasp", CardDb.DisplayName("BG22_HERO_004"));
        Assert.Equal("UNKNOWN_CARD", CardDb.DisplayName("UNKNOWN_CARD"));
    }

    [Fact]
    public void Database_HasThousandsOfCards()
    {
        Assert.True(CardDb.Count > 1000);
    }
}
