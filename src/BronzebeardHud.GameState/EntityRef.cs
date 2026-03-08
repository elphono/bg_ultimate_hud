namespace BronzebeardHud.GameState;

public enum EntityRefKind
{
    GameEntity,
    Id,
    Name,
    BracketRef,
}

public sealed class EntityRef : IEquatable<EntityRef>
{
    public EntityRefKind Kind { get; }
    public uint Id { get; }
    public string EntityName { get; }
    public string Zone { get; }
    public uint ZonePos { get; }
    public string CardId { get; }
    public uint Player { get; }

    private EntityRef(EntityRefKind kind, uint id = 0, string entityName = "",
        string zone = "", uint zonePos = 0, string cardId = "", uint player = 0)
    {
        Kind = kind;
        Id = id;
        EntityName = entityName ?? "";
        Zone = zone ?? "";
        ZonePos = zonePos;
        CardId = cardId ?? "";
        Player = player;
    }

    public static EntityRef GameEntity() => new(EntityRefKind.GameEntity);
    public static EntityRef ById(uint id) => new(EntityRefKind.Id, id: id);
    public static EntityRef ByName(string name) => new(EntityRefKind.Name, entityName: name);
    public static EntityRef Bracket(string entityName, uint id, string zone,
        uint zonePos, string cardId, uint player) =>
        new(EntityRefKind.BracketRef, id, entityName, zone, zonePos, cardId, player);

    public bool Equals(EntityRef? other)
    {
        if (other is null) return false;
        if (Kind != other.Kind) return false;
        return Kind switch
        {
            EntityRefKind.GameEntity => true,
            EntityRefKind.Id => Id == other.Id,
            EntityRefKind.Name => EntityName == other.EntityName,
            EntityRefKind.BracketRef => Id == other.Id && EntityName == other.EntityName
                && Zone == other.Zone && ZonePos == other.ZonePos
                && CardId == other.CardId && Player == other.Player,
            _ => false,
        };
    }

    public override bool Equals(object? obj) => Equals(obj as EntityRef);
    public override int GetHashCode() => HashCode.Combine(Kind, Id, EntityName);
}
