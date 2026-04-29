using Draven.ServerModels;
using Draven.Structures;

using RtmpSharp.IO.AMF3;
using RtmpSharp.Messaging;
using System;

namespace Draven.Messages.PlayerStatsService
{
    using Draven.Structures.Platform.Statistics;

    class GetRecentGames : IMessage
    {
        public RemotingMessageReceivedEventArgs HandleMessage(object sender, RemotingMessageReceivedEventArgs e)
        {
            SummonerClient summonerSender = sender as SummonerClient;
            long userId = summonerSender != null ? (long)summonerSender._sumId : 0;

            e.ReturnRequired = true;
            e.Data = new PlayerLifetimeStats
            {
                UserId = userId,
                PreviousFirstWinOfDay = new DateTime(2016, 08, 11, 12, 00, 00),
                PlayerStats = new PlayerStats
                {
                    PromoGamesPlayed = 0,
                    PromoGamesPlayedLastUpdate = new DateTime(2016, 08, 11, 12, 00, 00)
                },
                PlayerStatSummaries = new PlayerStatSummaries
                {
                    UserID = Convert.ToInt32(userId),
                    SummaryList = new ArrayCollection()
                },
                GameStatistics = new ArrayCollection
                {
                    new PlayerGameStats
                    {
                        ChampionId = 17,
                        CreateDate = new DateTime(2016, 08, 11, 12, 00, 00),
                        ExperienceEarned = 210,
                        IPEarned = 84,
                        FellowPlayers = new ArrayCollection(),
                        GameId = 1,
                        GameMapId = 11,
                        GameMode = "CLASSIC",
                        GameMutators = new ArrayCollection(),
                        GameType = "PRACTICE_GAME",
                        Invalid = false,
                        Leaver = false,
                        LeveledUp = false,
                        QueueType = "NONE",
                        Ranked = false,
                        RawStats = new ArrayCollection(),
                        Spell1 = 4,
                        Spell2 = 14,
                        Statistics = null,
                        TeamId = 100,
                        UserId = userId
                    }
                }
            };

            return e;
        }
    }
}
