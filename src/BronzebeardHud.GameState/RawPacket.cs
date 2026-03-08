namespace BronzebeardHud.GameState;

public abstract record RawPacket
{
    public sealed record CreateGame : RawPacket;
    public sealed record GameEntity(uint EntityId) : RawPacket;
    public sealed record PlayerEntity(uint EntityId, uint PlayerId) : RawPacket;
    public sealed record TagValue(string Tag, string Value) : RawPacket;
    public sealed record FullEntityCreate(uint Id, string CardId) : RawPacket;
    public sealed record FullEntityUpdate(EntityRef Entity, string CardId) : RawPacket;
    public sealed record ShowEntity(EntityRef Entity, string CardId) : RawPacket;
    public sealed record HideEntity(EntityRef Entity, string Tag, string Value) : RawPacket;
    public sealed record TagChange(EntityRef Entity, string Tag, string Value, bool DefChange) : RawPacket;
    public sealed record ChangeEntity(EntityRef Entity, string CardId) : RawPacket;
    public sealed record BlockStart(string BlockType, EntityRef Entity, EntityRef Target) : RawPacket;
    public sealed record BlockEnd : RawPacket;
    public sealed record MetaData(string Meta, string Data, uint InfoCount) : RawPacket;
    public sealed record MetaDataInfo(uint Index, EntityRef Entity) : RawPacket;
    public sealed record PlayerName(uint PlayerId, string Name) : RawPacket;
}
