using System.Text;
using System.Text.Json.Nodes;
using Tetrio.User;

namespace Tetrio.Record;

public class RecordData
{
    public string? ReplayId { get; }
    public TetrioUserTypes.GameMode GameMode { get; }
    public DateTimeOffset TimeStamp { get; }
    public TimeSpan AllGameTime => new TimeSpan(UserDatas.First().MatchDatas.Select(x => x.LifeTime).Sum(x => x.Ticks));
    public int Version { get; }
    public bool Is_Broken { get; } = false;
    public int AllMatchCount => UserDatas.First().MatchDatas.Count;
    public int MaxMatchWinCount => UserDatas.Select(x => x.AllWinCount).Max();
    public List<VSUserData> UserDatas { get; } = [];
    public IEnumerable<string> UsersName => UserDatas.Select(x => x.UserName);
    public RecordData(JsonObject ReplayData)
    {
        Version = ReplayData.TryGetPropertyValue("version", out JsonNode version) ? int.Parse(version!.ToString()) : 0;

        switch (Version)
        {
            case 0:
                {
                    if (ReplayData.TryGetPropertyValue("_id", out JsonNode replayid))
                    {
                        ReplayId = replayid.ToString();
                        GameMode = TetrioUserTypes.GameMode.TetraLeague;
                    }
                    else
                    {
                        ReplayId = null;
                        GameMode = TetrioUserTypes.GameMode.TetraLeague;
                    }
                    TimeStamp = DateTimeOffset.Parse(ReplayData["ts"]?.ToString());
                    Is_Broken = false;
                    foreach (var UserName in ReplayData["data"].AsArray().First()["board"].AsArray().Select(x => x["username"].ToString()))
                        UserDatas.Add(new(Version, GameMode, UserName, ReplayData["data"]));
                    break;
                }
            case 1:
                {
                    ReplayId = ReplayData["id"]?.ToString();
                    GameMode = ReplayData["gamemode"]?.ToString() switch
                    {
                        null => TetrioUserTypes.GameMode.CustomRoom,
                        "league" => TetrioUserTypes.GameMode.TetraLeague,
                        _ => throw new NotImplementedException()
                    };
                    TimeStamp = DateTimeOffset.Parse(ReplayData["ts"].ToString());

                    var UsersData = ReplayData["users"].AsArray();
                    if (UsersData.First() is JsonArray)
                    {
                        UsersData = UsersData.First().AsArray();
                        Is_Broken = true;
                    }

                    foreach (var User in UsersData)
                        UserDatas.Add(new VSUserData(Version, GameMode, User["username"].ToString(), ReplayData["replay"]));

                    UserDatas.First().MatchDatas.ForEach(x => AllGameTime.Add(x.LifeTime));
                    break;
                }
        }
    }
    public Dictionary<string, VSMatchData> GetUsersMatchData(int MatchNumber)
    {
        var ReturnData = new Dictionary<string, VSMatchData>();
        foreach (var User in UserDatas)
        {
            ReturnData.Add(User.UserName, User[MatchNumber - 1]);
        }
        return ReturnData;
    }
    public List<Dictionary<string, VSMatchData>> GetUsersMatchData(int MatchNumberStart, int MatchNumberEnd)
    {
        var ReturnData = new List<Dictionary<string, VSMatchData>>();
        for (int i = MatchNumberStart - 1; i < MatchNumberEnd - 1; i++)
        {
            ReturnData.Add(GetUsersMatchData(i));
        }
        return ReturnData;
    }
    public override string ToString()
    {
        return
        $"PlayedGame:{AllMatchCount} " +
        $"ReplayDate:<t:{TimeStamp.ToUnixTimeSeconds()}:f> " +
        $"AllReplayTime:{AllGameTime.ToString(@"hh\:mm\:ss")} ";
    }
    public class VSUserData
    {
        public List<VSMatchData> MatchDatas { get; } = [];
        public VSMatchData this[int Index] => MatchDatas[Index];
        public string UserName { get; }
        public string UserId { get; }
        public int AllWinCount { get; }
        public double AveragePPS { get; }
        public double AverageAPM { get; }
        public double AverageVSScore { get; }
        public double AverageAPP { get; }
        public double PeakPPS => MatchDatas.Select(x => x.PPS).Max();
        public double PeakAPM => MatchDatas.Select(x => x.APM).Max();
        public double PeakVSScore => MatchDatas.Select(x => x.VSScore).Max();
        public double PeakAPP => MatchDatas.Select(x => x.APP).Max();
        public int AllGarbageSent { get; }
        public int AllGarbageRecived { get; }
        public VSUserData(int Version, TetrioUserTypes.GameMode gameMode, string username, JsonNode ReplayData)
        {
            if (gameMode == TetrioUserTypes.GameMode.TetraLeague || gameMode == TetrioUserTypes.GameMode.CustomRoom)
                switch (Version)
                {
                    case 0:
                        {
                            var Datas = ReplayData.AsArray();
                            var Boards = Datas.Select(x => x["board"].AsArray().First(x => x["username"].ToString() == username)).ToList();
                            var ReplayEventEnds = Datas.Select(x => x["replays"].AsArray()
                            .Select(x => x["events"].AsArray()
                            .Last(x => x["type"].ToString() == "end"))
                            .First(x => x["data"]["export"]["options"]["username"].ToString() == username))
                                .ToList();
                            UserName = username;
                            UserId = Boards.First()["id"].ToString();

                            int WonCount = 0;
                            for (int i = 0; i < Boards.Count; i++)
                            {
                                if (gameMode == TetrioUserTypes.GameMode.CustomRoom)
                                {
                                    WonCount = int.Parse(Boards[i]["wins"].ToString());
                                    MatchDatas.Add(new VSMatchData(Version, gameMode, ReplayEventEnds[i], ref WonCount));
                                }
                                else
                                {
                                    MatchDatas.Add(new VSMatchData(Version, gameMode, ReplayEventEnds[i], ref WonCount));
                                }
                            }
                            AverageAPM = MatchDatas.Select(x => x.APM).Average();
                            AveragePPS = MatchDatas.Select(x => x.PPS).Average();
                            AverageVSScore = MatchDatas.Select(x => x.VSScore).Average();
                            AverageAPP = MatchDatas.Select(x => x.APP).Average();
                            AllWinCount = MatchDatas.Last().WonCount;
                            AllGarbageRecived = MatchDatas.Select(x => x.GarbageRecived).Sum();
                            AllGarbageSent = MatchDatas.Select(x => x.GarbageSent).Sum();
                            break;
                        }
                    case 1:
                        {
                            var LeaderBoard = ReplayData["leaderboard"].AsArray().First(x => x["username"].ToString() == username);
                            UserName = username;
                            UserId = LeaderBoard["id"].ToString();
                            var AverageStats = LeaderBoard["stats"];
                            AveragePPS = double.Parse(AverageStats["pps"].ToString());
                            AverageAPM = double.Parse(AverageStats["apm"].ToString());
                            AverageVSScore = double.Parse(AverageStats["vsscore"].ToString());
                            AverageAPP = AverageAPM / AveragePPS / 60;
                            AllWinCount = int.Parse(LeaderBoard["wins"].ToString());
                            AllGarbageSent = int.Parse(AverageStats["garbagesent"].ToString());
                            AllGarbageRecived = int.Parse(AverageStats["garbagereceived"].ToString());

                            var WinCounter = 0;
                            foreach (var Match in ReplayData["rounds"].AsArray())
                            {
                                MatchDatas.Add(new VSMatchData(Version, gameMode, Match.AsArray().First(x => x["id"].ToString() == UserId), ref WinCounter));
                            }
                            break;
                        }
                    default: throw new ArgumentException("非対応のバージョンです");
                }

        }
        public override string ToString()
        {
            StringBuilder ToStringData = new();
            ToStringData.AppendLine($"Point:{AllWinCount}");
            ToStringData.AppendLine("AverageData:");
            ToStringData.AppendLine($" PPS {Math.Round(AveragePPS, 2, MidpointRounding.AwayFromZero)}");
            ToStringData.AppendLine($" APM {Math.Round(AverageAPM, 2, MidpointRounding.AwayFromZero)}");
            ToStringData.AppendLine($" VSScore {Math.Round(AverageVSScore, 2, MidpointRounding.AwayFromZero)}");
            ToStringData.AppendLine($" APP {Math.Round(AverageAPP, 2, MidpointRounding.AwayFromZero)}");
            ToStringData.AppendLine();
            ToStringData.AppendLine("PeakData:");
            ToStringData.AppendLine($" PPS {Math.Round(PeakPPS, 2, MidpointRounding.AwayFromZero)}");
            ToStringData.AppendLine($" APM {Math.Round(PeakAPM, 2, MidpointRounding.AwayFromZero)}");
            ToStringData.AppendLine($" VSScore {Math.Round(PeakVSScore, 2, MidpointRounding.AwayFromZero)}");
            ToStringData.AppendLine($" APP {Math.Round(PeakAPP, 2, MidpointRounding.AwayFromZero)}");
            return ToStringData.ToString();
        }
    }
    public class VSMatchData
    {
        public int WonCount { get; }
        public bool Alive { get; }
        public double PPS { get; }
        public double APM { get; }
        public double VSScore { get; }
        public double APP { get; }
        public TimeSpan LifeTime { get; }
        public int GarbageSent { get; }
        public int GarbageRecived { get; }
        public VSMatchData(int Version, TetrioUserTypes.GameMode gameMode, JsonNode RoundData, ref int woncount)
        {
            switch (Version)
            {
                case 0:
                    {

                        LifeTime = TimeSpan.FromMilliseconds(int.Parse(RoundData["frame"].ToString()) * 1000 / 60);
                        var Data = RoundData["data"];
                        Alive = Data["reason"].ToString() == "winner";
                        WonCount = gameMode == TetrioUserTypes.GameMode.CustomRoom ? woncount : Alive ? woncount : woncount++;
                        var Export = Data["export"];
                        var Garbage = Export["stats"]["garbage"];
                        GarbageSent = int.Parse(Garbage["sent"].ToString());
                        GarbageRecived = int.Parse(Garbage["received"].ToString());
                        var AggregateStats = Export["aggregatestats"];
                        APM = double.Parse(AggregateStats["apm"].ToString());
                        PPS = double.Parse(AggregateStats["pps"].ToString());
                        VSScore = double.Parse(AggregateStats["vsscore"].ToString());
                        APP = APM / PPS / 60;
                        break;
                    }
                case 1:
                    {
                        var Stats = RoundData["stats"];
                        PPS = double.Parse(Stats["pps"].ToString());
                        APM = double.Parse(Stats["apm"].ToString());
                        VSScore = double.Parse(Stats["vsscore"].ToString());
                        APP = APM / PPS / 60;
                        Alive = bool.Parse(RoundData["alive"].ToString());
                        WonCount = Alive ? woncount : woncount++;
                        GarbageSent = int.Parse(Stats["garbagesent"].ToString());
                        GarbageRecived = int.Parse(Stats["garbagereceived"].ToString());
                        LifeTime = TimeSpan.FromMilliseconds(int.Parse(RoundData["lifetime"].ToString()));
                        break;
                    }
                default:
                    throw new ArgumentException
                        ("そのバージョンのリプレイデータはパースできません、というかUserData側で弾けてるはずだろ？なんでまた別のVersion指定してるん？");
            }

        }
        public override string ToString()
        {
            StringBuilder MatchStringData = new();
            MatchStringData.AppendLine($"PPS:{Math.Round(PPS, 2, MidpointRounding.AwayFromZero)}");
            MatchStringData.AppendLine($"APM:{Math.Round(APM, 2, MidpointRounding.AwayFromZero)}");
            MatchStringData.AppendLine($"VSScore:{Math.Round(VSScore, 2, MidpointRounding.AwayFromZero)}");
            MatchStringData.AppendLine($"APP:{Math.Round(APP, 2, MidpointRounding.AwayFromZero)}");
            MatchStringData.AppendLine();
            MatchStringData.AppendLine($"LineSent:{GarbageSent}");
            MatchStringData.AppendLine($"LineRecived:{GarbageRecived}");
            return MatchStringData.ToString();
        }
    }
}