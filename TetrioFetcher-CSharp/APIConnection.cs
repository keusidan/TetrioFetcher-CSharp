using System.Text.Json.Nodes;
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