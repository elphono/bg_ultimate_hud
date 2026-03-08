using System.Reflection;

namespace BronzebeardHud.GameState;

public static class CardDb
{
    private static readonly Lazy<Dictionary<string, string>> _cards = new(LoadCards);

    public static string? CardName(string cardId) =>
        _cards.Value.GetValueOrDefault(cardId);

    public static string DisplayName(string cardId) =>
        CardName(cardId) ?? cardId;

    public static int Count => _cards.Value.Count;

    private static Dictionary<string, string> LoadCards()
    {
        var dict = new Dictionary<string, string>();
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(
            "BronzebeardHud.GameState.Data.bg_cards.tsv");
        if (stream == null) return dict;
        using var reader = new StreamReader(stream);

        while (reader.ReadLine() is { } line)
        {
            var tabIndex = line.IndexOf('\t');
            if (tabIndex > 0)
            {
                dict[line[..tabIndex]] = line[(tabIndex + 1)..];
            }
        }
        return dict;
    }
}
