namespace BronzebeardHud.GameState;

public class EntityRegistry
{
    private readonly Dictionary<uint, Entity> _entities = new();

    public void Create(uint id, string cardId) => _entities[id] = new Entity(id, cardId);

    public Entity? Get(uint id) => _entities.GetValueOrDefault(id);

    public void SetTag(uint entityId, string tag, string value)
    {
        if (!_entities.TryGetValue(entityId, out var entity))
        {
            entity = new Entity(entityId, "");
            _entities[entityId] = entity;
        }
        entity.SetTag(tag, value);
    }

    public List<Entity> Find(Func<Entity, bool> predicate) =>
        _entities.Values.Where(predicate).ToList();

    public void Clear() => _entities.Clear();
}
