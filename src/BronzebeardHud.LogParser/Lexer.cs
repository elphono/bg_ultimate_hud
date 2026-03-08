using System.Text.RegularExpressions;
using BronzebeardHud.GameState;

namespace BronzebeardHud.LogParser;

public class Lexer
{
    private static readonly Regex LineRe = new(@"^[DWE] ([\d:.]+) (.+)$", RegexOptions.Compiled);
    private static readonly Regex SourceRe = new(@"^(GameState|PowerTaskList)\.DebugPrintPower\(\) - (.+)$", RegexOptions.Compiled);
    private static readonly Regex GamePrintRe = new(@"^GameState\.DebugPrintGame\(\) - (.+)$", RegexOptions.Compiled);
    private static readonly Regex PlayerNameRe = new(@"^PlayerID=(\d+), PlayerName=(.+)$", RegexOptions.Compiled);
    private static readonly Regex GameEntityRe = new(@"^GameEntity EntityID=(\d+)$", RegexOptions.Compiled);
    private static readonly Regex PlayerEntityRe = new(@"^Player EntityID=(\d+) PlayerID=(\d+) GameAccountId=", RegexOptions.Compiled);
    private static readonly Regex TagValueRe = new(@"^tag=(\S+) value=(.+)$", RegexOptions.Compiled);
    private static readonly Regex FullEntityCreateRe = new(@"^FULL_ENTITY - Creating ID=(\d+) CardID=(\w*)$", RegexOptions.Compiled);
    private static readonly Regex FullEntityUpdateRe = new(@"^FULL_ENTITY - Updating (.+) CardID=(\w*)$", RegexOptions.Compiled);
    private static readonly Regex ShowEntityRe = new(@"^SHOW_ENTITY - Updating Entity=(.+) CardID=(\w+)$", RegexOptions.Compiled);
    private static readonly Regex ChangeEntityRe = new(@"^CHANGE_ENTITY - Updating Entity=(.+) CardID=(\w+)$", RegexOptions.Compiled);
    private static readonly Regex HideEntityRe = new(@"^HIDE_ENTITY - Entity=(.+) tag=(\w+) value=(\w+)$", RegexOptions.Compiled);
    private static readonly Regex TagChangeRe = new(@"^TAG_CHANGE Entity=(.+) tag=(\w+) value=(\w+)\s*(DEF CHANGE)?$", RegexOptions.Compiled);
    private static readonly Regex BlockStartRe = new(@"^BLOCK_START BlockType=(\w+) Entity=(.+?) EffectCardId=.* EffectIndex=[-\d]+ Target=(.+?) SubOption=[-\d]+", RegexOptions.Compiled);
    private static readonly Regex MetaDataRe = new(@"^META_DATA - Meta=(\w+) Data=(\S+) InfoCount=(\d+)$", RegexOptions.Compiled);
    private static readonly Regex MetaDataInfoRe = new(@"^Info\[(\d+)\] = (.+)$", RegexOptions.Compiled);
    private static readonly Regex BracketEntityRe = new(@"^\[entityName=(.*?) id=(\d+) zone=(\w+) zonePos=(\d+) cardId=(\S*) player=(\d+)\]$", RegexOptions.Compiled);

    public LogLine? ParseLine(string line)
    {
        var lineMatch = LineRe.Match(line);
        if (!lineMatch.Success) return null;

        var timestamp = lineMatch.Groups[1].Value;
        var rest = lineMatch.Groups[2].Value;

        var sourceMatch = SourceRe.Match(rest);
        if (sourceMatch.Success)
        {
            var isGameState = sourceMatch.Groups[1].Value == "GameState";
            var rawPayload = sourceMatch.Groups[2].Value;
            var trimmed = rawPayload.TrimStart();
            var spaces = rawPayload.Length - trimmed.Length;
            var indent = (uint)(spaces / 4);

            return new LogLine
            {
                Timestamp = timestamp,
                Indent = indent,
                IsGameState = isGameState,
                Packet = ParsePacket(trimmed),
            };
        }

        var gameMatch = GamePrintRe.Match(rest);
        if (!gameMatch.Success) return null;

        var payload = gameMatch.Groups[1].Value;
        var nameMatch = PlayerNameRe.Match(payload);
        if (!nameMatch.Success) return null;

        return new LogLine
        {
            Timestamp = timestamp,
            Indent = 0,
            IsGameState = true,
            Packet = new RawPacket.PlayerName(
                uint.Parse(nameMatch.Groups[1].Value),
                nameMatch.Groups[2].Value),
        };
    }

    public EntityRef ParseEntity(string s)
    {
        s = s.Trim();
        if (s == "GameEntity") return EntityRef.GameEntity();
        if (s == "0") return EntityRef.ById(0);

        var bracketMatch = BracketEntityRe.Match(s);
        if (bracketMatch.Success)
        {
            return EntityRef.Bracket(
                bracketMatch.Groups[1].Value,
                uint.Parse(bracketMatch.Groups[2].Value),
                bracketMatch.Groups[3].Value,
                uint.Parse(bracketMatch.Groups[4].Value),
                bracketMatch.Groups[5].Value,
                uint.Parse(bracketMatch.Groups[6].Value));
        }

        if (uint.TryParse(s, out var id)) return EntityRef.ById(id);
        return EntityRef.ByName(s);
    }

    private RawPacket? ParsePacket(string payload)
    {
        payload = payload.Trim();

        if (payload == "CREATE_GAME") return new RawPacket.CreateGame();
        if (payload == "BLOCK_END") return new RawPacket.BlockEnd();

        Match m;

        if ((m = GameEntityRe.Match(payload)).Success)
            return new RawPacket.GameEntity(uint.Parse(m.Groups[1].Value));

        if ((m = PlayerEntityRe.Match(payload)).Success)
            return new RawPacket.PlayerEntity(
                uint.Parse(m.Groups[1].Value),
                uint.Parse(m.Groups[2].Value));

        if ((m = TagValueRe.Match(payload)).Success)
            return new RawPacket.TagValue(m.Groups[1].Value, m.Groups[2].Value.Trim());

        if ((m = FullEntityCreateRe.Match(payload)).Success)
            return new RawPacket.FullEntityCreate(
                uint.Parse(m.Groups[1].Value),
                m.Groups[2].Value);

        if ((m = FullEntityUpdateRe.Match(payload)).Success)
            return new RawPacket.FullEntityUpdate(
                ParseEntity(m.Groups[1].Value),
                m.Groups[2].Value);

        if ((m = ShowEntityRe.Match(payload)).Success)
            return new RawPacket.ShowEntity(
                ParseEntity(m.Groups[1].Value),
                m.Groups[2].Value);

        if ((m = ChangeEntityRe.Match(payload)).Success)
            return new RawPacket.ChangeEntity(
                ParseEntity(m.Groups[1].Value),
                m.Groups[2].Value);

        if ((m = HideEntityRe.Match(payload)).Success)
            return new RawPacket.HideEntity(
                ParseEntity(m.Groups[1].Value),
                m.Groups[2].Value,
                m.Groups[3].Value);

        if ((m = TagChangeRe.Match(payload)).Success)
            return new RawPacket.TagChange(
                ParseEntity(m.Groups[1].Value),
                m.Groups[2].Value,
                m.Groups[3].Value,
                m.Groups[4].Success);

        if ((m = BlockStartRe.Match(payload)).Success)
            return new RawPacket.BlockStart(
                m.Groups[1].Value,
                ParseEntity(m.Groups[2].Value),
                ParseEntity(m.Groups[3].Value));

        if ((m = MetaDataRe.Match(payload)).Success)
            return new RawPacket.MetaData(
                m.Groups[1].Value,
                m.Groups[2].Value,
                uint.Parse(m.Groups[3].Value));

        if ((m = MetaDataInfoRe.Match(payload)).Success)
            return new RawPacket.MetaDataInfo(
                uint.Parse(m.Groups[1].Value),
                ParseEntity(m.Groups[2].Value));

        return null;
    }
}
