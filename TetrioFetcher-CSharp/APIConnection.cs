using System.Text.Json.Nodes;
using Tetrio.User;

namespace Tetrio.API;
public class DelayAPI
{
    private static int WaitThreadCount = 0;
    private static object WaitObject = new object();
    public async static Task<JsonNode?> GetDataAsync(string ConnectionUrl)
    {
        int Delayms = 0;
        lock (WaitObject)
        {
            Delayms = WaitThreadCount * 1000;
            WaitThreadCount++;
        }
        await Task.Delay(Delayms);
        lock (WaitObject)
        {
            WaitThreadCount--;
        }
        try
        {
            using (HttpClient TetrioAPI = new())
            {
                string JsonString = await TetrioAPI.GetStringAsync(ConnectionUrl);
                var Json = JsonNode.Parse(JsonString);
                return Json["success"].ToString() == "true" ? Json : null;
            }
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch
        {
            throw;
        }
    }
}

public record Cache
{
    public CacheStatus Status { get; }
    public long Cached_At { get; }
    public long Cached_Until { get; }
    public Cache(string status, long at, long until)
    {
        Status = ToCacheStatus(status);
        Cached_At = at;
        Cached_Until = until;
    }
    public Cache(JsonNode CacheJson)
    {
        Status = ToCacheStatus(CacheJson["status"].ToString());
        Cached_At = long.Parse(CacheJson["cached_at"].ToString());
        Cached_Until = long.Parse(CacheJson["cached_until"].ToString());
    }
    private CacheStatus ToCacheStatus(string status)
    {
        switch (status)
        {
            case "hit": return CacheStatus.Hit;
            case "miss": return CacheStatus.Miss;
            case "awaited": return CacheStatus.Awaited;
            default: throw new NotImplementedException();
        }
        ;
    }
}

public enum CacheStatus
{
    Hit,
    Miss,
    Awaited
}