using System.Data;
using System.Text.Json.Nodes;
using static Tetrio.User.TetrioUserTypes;

namespace Tetrio;

/// <summary>
/// Tetrioのデータを安全にパースするためのクラスです
/// </summary>
/// <param name="data"></param>
public class TetrioAPIPerser(JsonObject data)
{
    private JsonObject Data = data;
    private JsonNode? GetJsonNode(string PropertyName)
    {
        if (Data.TryGetPropertyValue(PropertyName, out JsonNode GetData)) return GetData;
        else return null;
    }
    public string? GetString(string PropertyName) => GetJsonNode(PropertyName).ToString();
    public int? GetInt(string PropertyName) => GetString(PropertyName) is not null and string value ? int.Parse(value) : null;
    public double? GetDouble(string PropertyName) => GetString(PropertyName) is not null and string value ? double.Parse(value) : null;
    public Role? GetRole(string PropertyName) => GetString(PropertyName) is not null and string value ? Tetrio.User.TetrioUserTypes.GetRole(value) : null;
    public Rank? GetRank(string PropertyName) => GetString(PropertyName) is not null and string value ? Tetrio.User.TetrioUserTypes.GetRank(value) : null;
}