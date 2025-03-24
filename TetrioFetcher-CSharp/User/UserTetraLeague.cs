using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Tetrio.API;
using Tetrio.TetraLeague;

namespace Tetrio.User.TetraLeague;

public class UserLeague
{
    public Cache Cache { get; }
    public bool DataNotFind { get; } = false;
    public long GamesPlayed { get; }
    public long GamesWon { get; }
    public double Glicko { get; }
    public double? RD { get; }
    public bool Decaying { get; }
    public double TR { get; }
    public double Gxe { get; }
    public Rank Rank { get; }
    public Rank? BestRank { get; }
    public AggregateStats? Stats { get; }
    public long? Standing { get; }
    public long? Standing_Local { get; }
    public double? Percentile { get; }
    public Rank? Percentile_Rank { get; }
    public Rank? Next_Rank { get; }
    public Rank? Prev_Rank { get; }
    public long? Next_At { get; }
    public long? Prev_At { get; }
    public List<UserPastLeague> PastLeagues { get; } = new();

    public UserLeague(JsonObject LeagueJson)
    {
        Cache = new Cache(
        LeagueJson["cache"]["status"].ToString(),
        long.Parse(LeagueJson["cache"]["cached_at"].ToString()),
        long.Parse(LeagueJson["cache"]["cached_until"].ToString()));
        JsonObject LeagueJsonData = LeagueJson["data"].AsObject();
        if (!LeagueJsonData.TryGetPropertyValue("gamesplayed", out JsonNode jsonNode))
        {
            DataNotFind = true;
            GamesPlayed = 0;
            GamesWon = 0;
            Glicko = 0;
            RD = -1;
            Gxe = -1;
            TR = -1;
            Rank = Rank.Unranked;
            Decaying = false;
            Standing = -1;
            Standing_Local = -1;
            Prev_Rank = null;
            Prev_At = -1;
            Next_Rank = null;
            Next_At = -1;
            Percentile = -1;
            Percentile_Rank = Rank.Unranked;
            return;
        }
        GamesPlayed = long.Parse(LeagueJsonData["gamesplayed"].ToString());
        GamesWon = long.Parse(LeagueJsonData["gameswon"].ToString());
        Glicko = double.Parse(LeagueJsonData["glicko"].ToString());
        if (LeagueJsonData.TryGetPropertyValue("rd", out JsonNode ts) && ts is not null)
            RD = double.Parse(ts.ToString());
        Decaying = bool.Parse(LeagueJsonData["decaying"].ToString());
        TR = double.Parse(LeagueJsonData["tr"].ToString());
        Gxe = double.Parse(LeagueJsonData["gxe"].ToString());
        Rank = GetRank(LeagueJsonData["rank"].ToString());
        if (LeagueJsonData.TryGetPropertyValue("bestrank", out JsonNode bestrank) && bestrank is not null)
            BestRank = GetRank(bestrank.ToString());
        if (LeagueJsonData.TryGetPropertyValue("apm", out JsonNode apm) && apm is not null)
            Stats = new AggregateStats(
                double.Parse(LeagueJsonData["apm"].ToString()),
                double.Parse(LeagueJsonData["pps"].ToString()),
                double.Parse(LeagueJsonData["vs"].ToString())
                );
        if (LeagueJsonData.TryGetPropertyValue("standing", out JsonNode standing) && standing is not null)
            Standing = long.Parse(standing.ToString());
        if (LeagueJsonData.TryGetPropertyValue("standing_local", out JsonNode standing_local) && standing is not null)
            Standing_Local = long.Parse(standing_local.ToString());
        if (LeagueJsonData.TryGetPropertyValue("percentile", out JsonNode percentile) && percentile is not null)
            Percentile = double.Parse(percentile.ToString());
        if (LeagueJsonData.TryGetPropertyValue("percentile_rank", out JsonNode percentile_rank) && percentile_rank is not null)
            Percentile_Rank = GetRank(percentile_rank.ToString());
        if (LeagueJsonData.TryGetPropertyValue("next_rank", out JsonNode next_rank) && next_rank is not null)
            Next_Rank = GetRank(next_rank.ToString());
        if (LeagueJsonData.TryGetPropertyValue("prev_rank", out JsonNode prev_rank) && prev_rank is not null)
            Prev_Rank = GetRank(prev_rank.ToString());
        if (LeagueJsonData.TryGetPropertyValue("next_at", out JsonNode next_at) && next_at is not null)
            Next_At = long.Parse(next_at.ToString());
        if (new TetrioAPIPerser(LeagueJsonData.AsObject()).GetJsonNode("past") is not null and var pastdata)
            foreach (var PastData in pastdata.AsObject()) PastLeagues.Add(new UserPastLeague(PastData.Value.AsObject()));
    }
    public class UserPastLeague
    {
        public string Season { get; }
        public string UserName { get; }
        public string? Country { get; }
        public long? Placement { get; }
        public bool Ranked { get; }
        public long GamesPlayed { get; }
        public long GamesWon { get; }
        public double Glicko { get; }
        public double RD { get; }
        public double TR { get; }
        public double Gxe { get; }
        public Rank Rank { get; }
        public Rank? BestRank { get; }
        public AggregateStats Stats { get; }
        public UserPastLeague(JsonObject UserPastLeagueJsonData)
        {
            Season = UserPastLeagueJsonData["season"].ToString();
            UserName = UserPastLeagueJsonData["username"].ToString();
            if (UserPastLeagueJsonData.TryGetPropertyValue("country", out JsonNode country) && country is not null)
                Country = country.ToString();
            if (UserPastLeagueJsonData.TryGetPropertyValue("placement", out JsonNode placement) && placement is not null)
                Placement = long.Parse(placement.ToString());
            Ranked = bool.Parse(UserPastLeagueJsonData["ranked"].ToString());
            GamesPlayed = long.Parse(UserPastLeagueJsonData["gamesplayed"].ToString());
            GamesWon = long.Parse(UserPastLeagueJsonData["gameswon"].ToString());
            Glicko = double.Parse(UserPastLeagueJsonData["glicko"].ToString());
            RD = double.Parse(UserPastLeagueJsonData["rd"].ToString());
            TR = double.Parse(UserPastLeagueJsonData["tr"].ToString());
            Gxe = double.Parse(UserPastLeagueJsonData["gxe"].ToString());
            Rank = GetRank(UserPastLeagueJsonData["rank"].ToString());
            if (UserPastLeagueJsonData.TryGetPropertyValue("bestrank", out JsonNode bestrank) && bestrank is not null)
                BestRank = GetRank(bestrank.ToString());
            Stats = new AggregateStats(
                double.Parse(UserPastLeagueJsonData["apm"].ToString()),
                double.Parse(UserPastLeagueJsonData["pps"].ToString()),
                double.Parse(UserPastLeagueJsonData["vs"].ToString())
                );
        }
    }
    public static Rank GetRank(string RankName)
    {
        return RankName switch
        {
            "d" => Rank.D,
            "d+" => Rank.Dplus,
            "c-" => Rank.Cminus,
            "c" => Rank.C,
            "c+" => Rank.Cplus,
            "b-" => Rank.Bminus,
            "b" => Rank.B,
            "b+" => Rank.Bplus,
            "a-" => Rank.Aminus,
            "a" => Rank.A,
            "a+" => Rank.Aplus,
            "s-" => Rank.Sminus,
            "s" => Rank.S,
            "s+" => Rank.Splus,
            "ss" => Rank.SS,
            "u" => Rank.U,
            "x" => Rank.X,
            "x+" => Rank.Xplus,
            "z" => Rank.Unranked,
            _ => throw new ArgumentException("存在しないランクです")
        };
    }
}

public class UserLeagueFlow
{
    public UserLeagueFlow(JsonNode UserLeagueFlowJson)
    {
        Cache = new Cache(UserLeagueFlowJson["cache"]);
        StartTime = long.Parse(UserLeagueFlowJson["data"]["startTime"].ToString());
        foreach (var Point in UserLeagueFlowJson["data"]["points"].AsArray())
        {
            Points.Add(new(Point.AsArray(), StartTime));
        }
    }
    public Cache Cache { get; }
    public long StartTime { get; }
    public List<LeagueResultPoint> Points { get; } = new();
    public class LeagueResultPoint
    {
        public LeagueResultPoint(JsonArray PointJson, long StartTime)
        {
            TimeStamp = DateTimeOffset.FromUnixTimeMilliseconds(StartTime + long.Parse(PointJson[0].ToString()));
            Result = (MatchResult)Enum.ToObject(typeof(MatchResult), int.Parse(PointJson[1].ToString()) - 1);
            BeforeTR = int.Parse(PointJson[2].ToString());
            AfterTR = int.Parse(PointJson[3].ToString());
        }
        public DateTimeOffset TimeStamp { get; }
        public MatchResult Result { get; }
        public int BeforeTR { get; }
        public int AfterTR { get; }
        public enum MatchResult
        {
            Victory,
            Defeat,
            Victory_By_Disqualification,
            Defeat_By_Disqualification,
            Tie,
            No_Contest,
            Match_Nullified
        }
    }
}