using static Tetrio.User.TetrioUserTypes;
using System.Text.Json.Nodes;

namespace Tetrio.User;
public static class DelayGetAPI
{
    private static List<string> SyncURLs = new();
    public static Task<JsonNode?> GetDataAsync(string GetAPIURL)
    {
        lock (SyncURLs)
        {
            SyncURLs.Add(GetAPIURL);
        }
        while (SyncURLs.First() != GetAPIURL) ;
        JsonNode ResultData = JsonNode.Parse("{}")!;
        lock (SyncURLs)
        {
            using (HttpClient TetrioClient = new())
            {
                try
                {
                    ResultData = JsonNode.Parse(TetrioClient.GetStringAsync(GetAPIURL.ToLower()).Result);
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException("指定されたユーザーは見つかりませんでした");
                }
                catch
                {
                    throw;
                }
            }
        }
        Thread.Sleep(1000);
        SyncURLs.Remove(GetAPIURL);
        return Task.FromResult<JsonNode>(ResultData);
    }
}
public static class TetrioUserTypes
{
    public enum GameMode
    {
        _40Line,
        Blitz,
        QuickPlay,
        Expert_QuickPlay,
        TetraLeague,
        Zen,
        CustomRoom,
        Zenith,
        Other
    }
    public enum Summaries
    {
        _40Line,
        Blitz,
        QuickPlay,
        Expert_QuickPlay,
        TetraLeague,
        Zen,
        Achievements
    }
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
    public enum CacheStatus
    {
        Hit,
        Miss,
        Awaited
    }
    public enum AchievementRank
    {
        Bronze = 1,
        Silver = 2,
        Gold = 3,
        Platinum = 4,
        Diamond = 5,
        IssueRanked = 100,
        Top3,
        Top5,
        Top10,
        Top25,
        Top50,
        Top100,
    }
    public enum Role
    {
        Anon,
        User,
        Bot,
        HarfMod,
        Mod,
        Admin,
        Sysop,
        Hidden,
        Banned
    }
    public static Role GetRole(this string RoleName)
    {
        return RoleName switch
        {
            "anon" => Role.Anon,
            "user" => Role.User,
            "bot" => Role.Bot,
            "harfmod" => Role.HarfMod,
            "mod" => Role.Mod,
            "admin" => Role.Admin,
            "sysop" => Role.Sysop,
            "hidden" => Role.Hidden,
            "banned" => Role.Banned
        };
    }
    public static Rank GetRank(this string RankName)
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
public class AggregateStats(double apm, double pps, double vsscore)
{
    public double APM { get; } = apm;
    public double PPS { get; } = pps;
    public double VSScore { get; } = vsscore;
    public double APP => APM / PPS / 60;
}
public record Cache
{
    public CacheStatus Status { get; }
    public long Cached_At { get; }
    public long Cached_Until { get; }
    public Cache(string status, long at, long until)
    {
        Status = status switch
        {
            "hit" => CacheStatus.Hit,
            "miss" => CacheStatus.Miss,
            "awaited" => CacheStatus.Awaited,
            _ => throw new NotImplementedException()
        };
        Cached_At = at;
        Cached_Until = until;
    }
}
public class Account(string connectionuserparameter)
{
    public string ConnectionUserParameter = connectionuserparameter;
    private Lazy<Info?> _LazyInfo = new(() =>
    {
        try
        {
            return new Info(DelayGetAPI.GetDataAsync($"https://ch.tetr.io/api/users/{connectionuserparameter}").Result.AsObject());
        }
        catch
        {
            return null;
        }
    });
    private Lazy<League?> _LazyLeague = new(() =>
    {
        try
        {
            return new League(DelayGetAPI.GetDataAsync($"https://ch.tetr.io/api/users/{connectionuserparameter}/summaries/league").Result.AsObject());
        }
        catch
        {
            return null;
        }
    });
    public Info? InfoData => _LazyInfo.Value;
    public League? LeagueData => _LazyLeague.Value;
}
public class Info
{
    public Cache Cache { get; }
    public string UserID { get; }
    public string UserName { get; }
    public Role AccountRole { get; }
    public DateTimeOffset? TS { get; }
    public string? BotMaster { get; }
    public List<Badge> Badges { get; } = new();
    public double XP { get; }
    public long GamesPlayed { get; }
    public long GamesWon { get; }
    public double GameTime { get; }
    public string? Country { get; }
    public bool? BadStanding { get; }
    public bool Supporter { get; }
    public long Supporter_Tier { get; }
    public long? Avater_Revision { get; }
    public long? Banner_Revision { get; }
    public string? Bio { get; }
    public Connection? Connections { get; }
    public long Friend_Count { get; }
    public DistinguishmentData? Distinguishment { get; }
    public List<AchievementRank> Achievements { get; } = new();
    public long AR { get; }
    public Dictionary<AchievementRank, long> AR_Count { get; }
    public Info(JsonNode UserDataJson)
    {
        Cache = new Cache(
            UserDataJson["cache"]["status"].ToString(),
            long.Parse(UserDataJson["cache"]["cached_at"].ToString()),
            long.Parse(UserDataJson["cache"]["cached_until"].ToString()));
        var Data = UserDataJson["data"].AsObject();
        UserID = Data["_id"].ToString();
        UserName = Data["username"].ToString();
        AccountRole = TetrioUserTypes.GetRole(Data["role"].ToString());
        if (Data.TryGetPropertyValue("ts", out JsonNode ts))
            TS = DateTimeOffset.Parse(ts.ToString());
        if (Data.TryGetPropertyValue("botmaster", out JsonNode botmaster))
            BotMaster = botmaster.ToString();
        foreach (var badge in Data["badges"].AsArray())
        {
            string? Group = null;
            DateTimeOffset? BadgeTS = null;
            if (badge.AsObject().TryGetPropertyValue("group", out JsonNode group) && group is not null)
                Group = group.ToString();
            if (badge.AsObject().TryGetPropertyValue("ts", out JsonNode badgets) && badgets is not null && badgets.ToString() != "false")
                BadgeTS = DateTimeOffset.Parse(badgets.ToString());
            Badges.Add(new(badge["id"].ToString(), badge["label"].ToString(), Group, BadgeTS));
        }
        XP = double.Parse(Data["xp"].ToString());
        GamesPlayed = long.Parse(Data["gamesplayed"].ToString());
        GamesWon = long.Parse(Data["gameswon"].ToString());
        GameTime = double.Parse(Data["gametime"].ToString());
        Country = Data["country"]?.ToString();
        if (Data.TryGetPropertyValue("badstanding", out JsonNode badstanding) && badstanding is not null)
            BadStanding = bool.Parse(badstanding.ToString());
        Supporter = bool.Parse(Data["supporter"].ToString());
        Supporter_Tier = long.Parse(Data["supporter_tier"].ToString());
        if (Data.TryGetPropertyValue("avatar_revision", out JsonNode avatar_revision) && avatar_revision is not null)
            Avater_Revision = long.Parse(avatar_revision.ToString());
        if (Data.TryGetPropertyValue("banner_revision", out JsonNode banner_revision) && banner_revision is not null)
            Banner_Revision = long.Parse(banner_revision.ToString());
        if (Data.TryGetPropertyValue("bio", out JsonNode bio) && bio is not null)
            Bio = bio.ToString();
        Connections = new Connection(Data["connections"].AsObject());
        Friend_Count = long.Parse(Data["friend_count"].ToString());
        if (Data.TryGetPropertyValue("distinguishments", out JsonNode distinguishments) && distinguishments is not null)
            Distinguishment = new DistinguishmentData(distinguishments["type"].ToString());
        // Achievements =;
        AR = long.Parse(Data["ar"].ToString());
        AR_Count = Achievement_Counter(Data["ar_counts"].AsObject());
    }
    public enum Achievement
    {

    }
    public record Badge(string Id, string? Label, string Group, DateTimeOffset? TS);
    public record Connection
    {
        public DefaultAccountDataFormat? Discord { get; }
        public DefaultAccountDataFormat? Twitch { get; }
        public DefaultAccountDataFormat? Twitter { get; }
        public DefaultAccountDataFormat? Reddit { get; }
        public DefaultAccountDataFormat? Youtube { get; }
        public DefaultAccountDataFormat? Steam { get; }
        public Connection(JsonObject ConnectionsJson)
        {
            JsonNode AccountData;
            if (ConnectionsJson.TryGetPropertyValue("discord", out AccountData))
            {
                Discord = new DefaultAccountDataFormat(AccountData["id"].ToString(), AccountData["username"].ToString(), AccountData["display_username"].ToString());
            }
            if (ConnectionsJson.TryGetPropertyValue("twitch", out AccountData))
            {
                Twitch = new DefaultAccountDataFormat(AccountData["id"].ToString(), AccountData["username"].ToString(), AccountData["display_username"].ToString());
            }
            if (ConnectionsJson.TryGetPropertyValue("twitter", out AccountData))
            {
                Twitter = new DefaultAccountDataFormat(AccountData["id"].ToString(), AccountData["username"].ToString(), AccountData["display_username"].ToString());
            }
            if (ConnectionsJson.TryGetPropertyValue("reddit", out AccountData))
            {
                Reddit = new DefaultAccountDataFormat(AccountData["id"].ToString(), AccountData["username"].ToString(), AccountData["display_username"].ToString());
            }
            if (ConnectionsJson.TryGetPropertyValue("youtube", out AccountData))
            {
                Youtube = new DefaultAccountDataFormat(AccountData["id"].ToString(), AccountData["username"].ToString(), AccountData["display_username"].ToString());
            }
            if (ConnectionsJson.TryGetPropertyValue("steam", out AccountData))
            {
                Steam = new DefaultAccountDataFormat(AccountData["id"].ToString(), AccountData["username"].ToString(), AccountData["display_username"].ToString());
            }
        }
        public record DefaultAccountDataFormat(string id, string username, string displayname)
        {
            public string ID { get; } = id;
            public string UserName { get; } = username;
            public string DisplayName { get; } = displayname;
        }
    }
    public record DistinguishmentData(string Type)
    {
        public string Type { get; } = Type;
    }
    public Dictionary<AchievementRank, long> Achievement_Counter(JsonObject AchievementJson)
    {
        var ReturnDictionary = new Dictionary<AchievementRank, long>();
        foreach (var Achievement in AchievementJson)
        {
            long AchievementCount = long.Parse(Achievement.Value.ToString());
            if (long.TryParse(Achievement.Key, out long AchievementAmount))
            {
                ReturnDictionary.Add((AchievementRank)AchievementAmount, AchievementCount);
            }
            else
            {
                switch (Achievement.Key)
                {
                    case "t3":
                        {
                            ReturnDictionary.Add(AchievementRank.Top3, AchievementCount);
                            break;
                        }
                    case "t5":
                        {
                            ReturnDictionary.Add(AchievementRank.Top5, AchievementCount);
                            break;
                        }
                    case "t10":
                        {
                            ReturnDictionary.Add(AchievementRank.Top10, AchievementCount);
                            break;
                        }
                    case "t25":
                        {
                            ReturnDictionary.Add(AchievementRank.Top25, AchievementCount);
                            break;
                        }
                    case "t50":
                        {
                            ReturnDictionary.Add(AchievementRank.Top50, AchievementCount);
                            break;
                        }
                    case "t100":
                        {
                            ReturnDictionary.Add(AchievementRank.Top100, AchievementCount);
                            break;
                        }
                }
            }
        }
        return ReturnDictionary;
    }
}
public class League
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

    public League(JsonObject LeagueJson)
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
        Rank = TetrioUserTypes.GetRank(LeagueJsonData["rank"].ToString());
        if (LeagueJsonData.TryGetPropertyValue("bestrank", out JsonNode bestrank) && bestrank is not null)
            BestRank = TetrioUserTypes.GetRank(bestrank.ToString());
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
            Percentile_Rank = TetrioUserTypes.GetRank(percentile_rank.ToString());
        if (LeagueJsonData.TryGetPropertyValue("next_rank", out JsonNode next_rank) && next_rank is not null)
            Next_Rank = TetrioUserTypes.GetRank(next_rank.ToString());
        if (LeagueJsonData.TryGetPropertyValue("prev_rank", out JsonNode prev_rank) && prev_rank is not null)
            Prev_Rank = TetrioUserTypes.GetRank(prev_rank.ToString());
        if (LeagueJsonData.TryGetPropertyValue("next_at", out JsonNode next_at) && next_at is not null)
            Next_At = long.Parse(next_at.ToString());
        foreach (var PastData in LeagueJsonData["past"].AsObject()) PastLeagues.Add(new UserPastLeague(PastData.Value.AsObject()));
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
            Rank = TetrioUserTypes.GetRank(UserPastLeagueJsonData["rank"].ToString());
            if (UserPastLeagueJsonData.TryGetPropertyValue("bestrank", out JsonNode bestrank) && bestrank is not null)
                BestRank = TetrioUserTypes.GetRank(bestrank.ToString());
            Stats = new AggregateStats(
                double.Parse(UserPastLeagueJsonData["apm"].ToString()),
                double.Parse(UserPastLeagueJsonData["pps"].ToString()),
                double.Parse(UserPastLeagueJsonData["vs"].ToString())
                );
        }
    }
}