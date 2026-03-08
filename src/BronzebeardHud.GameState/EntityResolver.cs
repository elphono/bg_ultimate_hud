namespace BronzebeardHud.GameState;

public class EntityResolver
{
    private uint? _gameEntityId;
    private readonly Dictionary<string, uint> _nameToId = new();

    public void SetGameEntity(uint id) => _gameEntityId = id;

    public void RegisterName(string name, uint entityId) => _nameToId[name] = entityId;

    public uint? Resolve(EntityRef entityRef) => entityRef.Kind switch
    {
        EntityRefKind.GameEntity => _gameEntityId,
        EntityRefKind.Id => entityRef.Id,
        EntityRefKind.Name => _nameToId.TryGetValue(entityRef.EntityName, out var id) ? id : null,
        EntityRefKind.BracketRef => entityRef.Id,
        _ => null,
    };

    public void Clear()
    {
        _gameEntityId = null;
        _nameToId.Clear();
    }
}
