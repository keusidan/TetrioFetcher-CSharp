using System.Text.Json.Nodes;
using Tetrio.API;
using Tetrio.User;
using Tetrio.User.TetraLeague;

namespace Tetrio.TetraLeague;


public enum Rank
{
    D,
    Dplus,
    Cminus,
    C,
    Cplus,
    Bminus,
    B,
    Bplus,
    Aminus,
    A,
    Aplus,
    Sminus,
    S,
    Splus,
    SS,
    U,
    X,
    Xplus,
    Unranked
}

public class TetraLeagueStatus
{
    TetraLeagueStatus()
    {
        JsonObject Status = DelayAPI.GetDataAsync("https://ch.tetr.io/api/labs/league_ranks").Result.AsObject();
        Cache = new Cache(Status["cache"]);
        _id = Status["data"]["_id"].ToString();
        Total = int.Parse(Status["data"]["data"]["total"].ToString());
        TimeStamp = DateTimeOffset.Parse(Status["data"]["t"].ToString());
        foreach (var RankData in Status["data"]["data"].AsObject())
        {
            if (RankData.Key != "total")
            {
                Ranks.Add(
                    UserLeague.GetRank(RankData.Key),
                    new RankStatus(RankData.Value)
                    );
            }
        }
    }
    public Cache Cache { get; }
    public string _id { get; }
    public int Total { get; }
    public DateTimeOffset TimeStamp { get; }
    public Dictionary<Rank, RankStatus> Ranks { get; }
    public class RankStatus
    {
        public RankStatus(JsonNode rankjson)
        {
            Position = int.Parse(rankjson["pos"].ToString());
            Percentile = double.Parse(rankjson["percentile"].ToString());
            TR = double.Parse(rankjson["tr"].ToString());
            TargetTR = double.Parse(rankjson["targettr"].ToString());
            if (rankjson.AsObject().TryGetPropertyValue("apm", out _))
            {
                Stats = new AggregateStats(
                double.Parse(rankjson["apm"].ToString()),
                double.Parse(rankjson["pps"].ToString()),
                double.Parse(rankjson["vs"].ToString())
                );
            }
            Count = int.Parse(rankjson["count"].ToString());
        }
        public int Position { get; }
        public double Percentile { get; }
        public double TR { get; }
        public double TargetTR { get; }
        public AggregateStats? Stats { get; }
        public int Count { get; }
    }
}