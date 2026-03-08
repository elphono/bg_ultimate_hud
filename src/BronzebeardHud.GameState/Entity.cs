namespace BronzebeardHud.GameState;

public class Entity
{
    public uint Id { get; }
    public string CardId { get; set; }
    public Dictionary<string, string> Tags { get; } = new();

    public Entity(uint id, string cardId)
    {
        Id = id;
        CardId = cardId;
    }

    public string? Tag(string key) => Tags.GetValueOrDefault(key);

    public int TagInt(string key) =>
        Tags.TryGetValue(key, out var v) && int.TryParse(v, out var n) ? n : 0;

    public void SetTag(string key, string value) => Tags[key] = value;

    public bool IsHero => Tag("CARDTYPE") == "HERO";
    public bool IsMinion => Tag("CARDTYPE") == "MINION";
    public string? Zone => Tag("ZONE");
    public int Controller => TagInt("CONTROLLER");
}
