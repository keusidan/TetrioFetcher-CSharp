using Tetrio.API;
using System.Text.Json.Nodes;
using Tetrio.TetraLeague;
using Tetrio.User.TetraLeague;

namespace Tetrio.User;
public enum GameMode
{
    _40Line,
    Blitz,
    QuickPlay,
    Expert_QuickPlay,
    TetraLeague,
    Zen,
    CustomRoom,
    Zenith
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

public class AggregateStats(double apm, double pps, double vsscore)
{
    public double APM { get; } = apm;
    public double PPS { get; } = pps;
    public double VSScore { get; } = vsscore;
    public double APP => APM / PPS / 60;
}
public class TetraChAccount(string ConnectionUserParameter)
{
    private Lazy<UserInfo?> _LazyInfo = new(() =>
    {
        JsonNode? Data = DelayAPI.GetDataAsync($"https://ch.tetr.io/api/users/{ConnectionUserParameter.ToLower()}").Result;
        return Data != null ? new UserInfo(Data.AsObject()) : null;
    });
    private Lazy<UserLeague?> _LazyLeague = new(() =>
    {
        JsonNode? Data = DelayAPI.GetDataAsync($"https://ch.tetr.io/api/users/{ConnectionUserParameter.ToLower()}/summaries/league").Result;
        return Data != null ? new UserLeague(Data.AsObject()) : null;
    });
    private Lazy<UserLeagueFlow?> _LazyStats = new(() =>
    {
        JsonNode? Data = DelayAPI.GetDataAsync($"https://ch.tetr.io/api/labs/leagueflow/{ConnectionUserParameter.ToLower()}").Result;
        return Data != null ? new UserLeagueFlow(Data) : null;
    });
    public UserInfo? InfoData => _LazyInfo.Value;
    public UserLeague? LeagueData => _LazyLeague.Value;
    public UserLeagueFlow? LeagueFlowData => _LazyStats.Value;
}
public class UserInfo
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
    public UserInfo(JsonNode UserDataJson)
    {
        Cache = new Cache(
            UserDataJson["cache"]["status"].ToString(),
            long.Parse(UserDataJson["cache"]["cached_at"].ToString()),
            long.Parse(UserDataJson["cache"]["cached_until"].ToString()));
        var Data = UserDataJson["data"].AsObject();
        UserID = Data["_id"].ToString();
        UserName = Data["username"].ToString();
        AccountRole = GetRole(Data["role"].ToString());
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
        Avater_Revision = new TetrioAPIPerser(Data).GetLong("avatar_revision");
        if (Avater_Revision == 0) Avater_Revision = null;
        Banner_Revision = new TetrioAPIPerser(Data).GetLong("banner_revision");
        if (Avater_Revision == 0) Avater_Revision = null;
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
    public static Role GetRole(string RoleName)
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
}