namespace BronzebeardHud.GameState;

public class GameStateEngine
{
    private readonly EntityRegistry _entities = new();
    private readonly EntityResolver _resolver = new();
    private GamePhase _phase = GamePhase.NotStarted;
    private uint _turn;
    private uint _gameEntityId;
    private uint _localPlayerEntityId;
    private uint _localPlayerId;
    private readonly Dictionary<uint, (uint PlayerId, string Name)> _players = new();
    private uint? _activeEntityId;
    private uint _activeEntityIndent;

    public void Process(LogLine line)
    {
        if (line.Packet is null) return;

        if (_activeEntityId is { } activeId)
        {
            if (line.Indent > _activeEntityIndent && line.Packet is RawPacket.TagValue tv)
            {
                _entities.SetTag(activeId, tv.Tag, tv.Value);
                return;
            }
            if (line.Indent <= _activeEntityIndent)
            {
                _activeEntityId = null;
            }
        }

        switch (line.Packet)
        {
            case RawPacket.CreateGame:
                Reset();
                break;

            case RawPacket.GameEntity ge:
                _gameEntityId = ge.EntityId;
                _entities.Create(ge.EntityId, "");
                _resolver.SetGameEntity(ge.EntityId);
                _activeEntityId = ge.EntityId;
                _activeEntityIndent = line.Indent;
                break;

            case RawPacket.PlayerEntity pe:
                _entities.Create(pe.EntityId, "");
                _entities.SetTag(pe.EntityId, "PLAYER_ID", pe.PlayerId.ToString());
                _players[pe.EntityId] = (pe.PlayerId, "");
                _activeEntityId = pe.EntityId;
                _activeEntityIndent = line.Indent;
                break;

            case RawPacket.FullEntityCreate fec:
                _entities.Create(fec.Id, fec.CardId);
                _activeEntityId = fec.Id;
                _activeEntityIndent = line.Indent;
                break;

            case RawPacket.FullEntityUpdate feu:
                if (_resolver.Resolve(feu.Entity) is { } feuId)
                {
                    UpdateCardId(feuId, feu.CardId);
                    _activeEntityId = feuId;
                    _activeEntityIndent = line.Indent;
                }
                break;

            case RawPacket.ShowEntity se:
                if (_resolver.Resolve(se.Entity) is { } seId)
                {
                    UpdateCardId(seId, se.CardId);
                    _activeEntityId = seId;
                    _activeEntityIndent = line.Indent;
                }
                break;

            case RawPacket.ChangeEntity ce:
                if (_resolver.Resolve(ce.Entity) is { } ceId)
                {
                    UpdateCardId(ceId, ce.CardId);
                    _activeEntityId = ceId;
                    _activeEntityIndent = line.Indent;
                }
                break;

            case RawPacket.HideEntity he:
                if (_resolver.Resolve(he.Entity) is { } heId)
                    _entities.SetTag(heId, he.Tag, he.Value);
                break;

            case RawPacket.TagChange tc:
                if (_resolver.Resolve(tc.Entity) is { } tcId)
                {
                    _entities.SetTag(tcId, tc.Tag, tc.Value);

                    if (tcId == _gameEntityId && tc.Tag == "STEP")
                    {
                        if (GamePhaseHelper.FromStep(tc.Value) is { } newPhase)
                            _phase = newPhase;
                    }

                    if (tcId == _localPlayerEntityId && tc.Tag == "TURN"
                        && uint.TryParse(tc.Value, out var t))
                    {
                        _turn = t;
                    }
                }
                break;

            case RawPacket.PlayerName pn:
                var eid = _players.FirstOrDefault(
                    p => p.Value.PlayerId == pn.PlayerId).Key;
                if (eid != 0)
                {
                    _players[eid] = (_players[eid].PlayerId, pn.Name);
                    _resolver.RegisterName(pn.Name, eid);
                }
                break;

            case RawPacket.TagValue:
                break;

            case RawPacket.BlockStart:
            case RawPacket.BlockEnd:
            case RawPacket.MetaData:
            case RawPacket.MetaDataInfo:
                _activeEntityId = null;
                break;
        }
    }

    public void IdentifyLocalPlayer()
    {
        foreach (var (entityId, (playerId, _)) in _players)
        {
            var entity = _entities.Get(entityId);
            if (entity?.Tag("BACON_DUMMY_PLAYER") != "1")
            {
                _localPlayerEntityId = entityId;
                _localPlayerId = playerId;
                return;
            }
        }
    }

    public void RegisterPlayerName(string name, uint entityId)
    {
        _resolver.RegisterName(name, entityId);
        if (_players.TryGetValue(entityId, out var info))
            _players[entityId] = (info.PlayerId, name);
    }

    public GameStateSnapshot Snapshot() => new()
    {
        Phase = _phase,
        Turn = _turn,
        Player = BuildPlayerState(),
        Opponents = BuildOpponentStates(),
        GameEntityId = _gameEntityId,
    };

    private PlayerState BuildPlayerState()
    {
        if (_localPlayerEntityId == 0) return new PlayerState();

        var playerEntity = _entities.Get(_localPlayerEntityId);
        var heroEntityId = playerEntity?.TagInt("HERO_ENTITY") ?? 0;
        var hero = heroEntityId > 0 ? _entities.Get((uint)heroEntityId) : null;

        var name = _players.TryGetValue(_localPlayerEntityId, out var info)
            ? info.Name : "";

        return new PlayerState
        {
            EntityId = _localPlayerEntityId,
            PlayerId = _localPlayerId,
            Name = name,
            HeroCardId = hero?.CardId ?? "",
            HeroEntityId = (uint)heroEntityId,
            Health = (hero?.TagInt("HEALTH") ?? 0) - (hero?.TagInt("DAMAGE") ?? 0),
            Armor = hero?.TagInt("ARMOR") ?? 0,
            TavernTier = playerEntity?.TagInt("PLAYER_TECH_LEVEL") ?? 0,
            Board = BuildBoard(_localPlayerId),
            Shop = BuildShop(_localPlayerId),
            Gold = CalculateGold(playerEntity),
        };
    }

    private List<OpponentState> BuildOpponentStates()
    {
        var opponents = new List<OpponentState>();
        foreach (var (entityId, (playerId, _)) in _players)
        {
            if (entityId == _localPlayerEntityId) continue;
            var entity = _entities.Get(entityId);
            if (entity?.Tag("BACON_DUMMY_PLAYER") == "1") continue;

            var heroEntityId = entity?.TagInt("HERO_ENTITY") ?? 0;
            var hero = heroEntityId > 0 ? _entities.Get((uint)heroEntityId) : null;

            opponents.Add(new OpponentState
            {
                EntityId = entityId,
                PlayerId = playerId,
                HeroCardId = hero?.CardId ?? "",
                HeroEntityId = (uint)heroEntityId,
                Health = (hero?.TagInt("HEALTH") ?? 0) - (hero?.TagInt("DAMAGE") ?? 0),
                TavernTier = entity?.TagInt("PLAYER_TECH_LEVEL") ?? 0,
                LastKnownBoard = new List<Minion>(),
                LastSeenTurn = 0,
            });
        }
        return opponents;
    }

    private List<Minion> BuildBoard(uint playerId)
    {
        return _entities.Find(e =>
                e.IsMinion
                && e.Zone == "PLAY"
                && e.Controller == (int)playerId
                && e.TagInt("ZONE_POSITION") > 0)
            .Select(e => new Minion
            {
                EntityId = e.Id,
                CardId = e.CardId,
                Attack = e.TagInt("ATK"),
                Health = e.TagInt("HEALTH"),
                ZonePos = (uint)e.TagInt("ZONE_POSITION"),
            })
            .OrderBy(m => m.ZonePos)
            .ToList();
    }

    private List<ShopItem> BuildShop(uint playerId)
    {
        if (_phase != GamePhase.Shopping) return new List<ShopItem>();

        return _entities.Find(e =>
                e.Zone == "PLAY"
                && e.Controller != (int)playerId
                && e.Controller > 0
                && e.TagInt("ZONE_POSITION") > 0
                && (e.IsMinion || e.Tag("CARDTYPE") == "BATTLEGROUND_SPELL"))
            .Select(e => new ShopItem
            {
                EntityId = e.Id,
                CardId = e.CardId,
                Attack = e.TagInt("ATK"),
                Health = e.TagInt("HEALTH"),
                ZonePos = (uint)e.TagInt("ZONE_POSITION"),
                Tier = (byte)e.TagInt("TECH_LEVEL"),
                IsSpell = e.Tag("CARDTYPE") == "BATTLEGROUND_SPELL",
            })
            .OrderBy(s => s.ZonePos)
            .ToList();
    }

    private static int CalculateGold(Entity? playerEntity)
    {
        if (playerEntity == null) return 0;
        var resources = playerEntity.TagInt("RESOURCES");
        var used = playerEntity.TagInt("RESOURCES_USED");
        var temp = playerEntity.TagInt("TEMP_RESOURCES");
        return Math.Max(0, resources - used + temp);
    }

    private void UpdateCardId(uint id, string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return;
        var entity = _entities.Get(id);
        if (entity != null) entity.CardId = cardId;
    }

    private void Reset()
    {
        _entities.Clear();
        _resolver.Clear();
        _phase = GamePhase.NotStarted;
        _turn = 0;
        _gameEntityId = 0;
        _localPlayerEntityId = 0;
        _localPlayerId = 0;
        _players.Clear();
        _activeEntityId = null;
        _activeEntityIndent = 0;
    }
}
