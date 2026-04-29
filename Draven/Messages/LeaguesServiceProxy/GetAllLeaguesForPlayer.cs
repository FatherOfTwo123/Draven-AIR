using Draven.ServerModels;
using Draven.Structures;

using RtmpSharp.IO.AMF3;
using RtmpSharp.Messaging;
using System.Collections.Generic;

namespace Draven.Messages.LeaguesServiceProxy
{
    using Draven.Structures.Leagues.Pojo;

    class GetAllLeaguesForPlayer : IMessage
    {
        public RemotingMessageReceivedEventArgs HandleMessage(object sender, RemotingMessageReceivedEventArgs e)
        {
            SummonerClient summonerSender = sender as SummonerClient;
            string playerId = summonerSender != null ? summonerSender._accId.ToString() : "1";
            string playerName = summonerSender != null ? summonerSender._summonername : "Maufeat";

            e.ReturnRequired = true;
            e.Data = new LeagueListDTO
            {
                Name = "LeagueSandbox Is Master",
                Entries = new List<LeagueItemDTO>
                {
                    new LeagueItemDTO
                    {
                        PlayerOrTeamId = playerId,
                        PlayerOrteamName = playerName,
                        LeagueName = "LeagueSandbox Is Master",
                        QueueType = "RANKED_SOLO_5x5",
                        Tier = "CHALLENGER",
                        Rank = "I",
                        LeaguePoints = 1337,
                        PreviousDayLeaguePosition = 5,
                        Wins = 999,
                        Losses = 0,
                        LastPlayed = 0,
                        TimeUntilDecay = -1,
                        InactivityStatus = "OK",
                        TimeUntilInactivityStatusChanges = 0,
                        TimeLastDecayMessageShown = 0,
                        DisplayDecayWarning = false,
                        DemotionWarning = 0,
                        SeasonEndTier = "CHALLENGER",
                        SeasonEndRank = "I",
                        SeasonEndApexPosition = 1,
                        Veteran = true,
                        FreshBlood = true,
                        Inactive = false,
                        HotStreak = true,
                        PlayStyle = new ArrayCollection(),
                        PlayStyleReminingWins = 1,
                        ApexDaysUntilDecay = 0,
                        LeaguePointsDelta = 0,
                        MiniSeries = null
                    }
                },
                Tier = "CHALLENGER",
                Queue = "RANKED_SOLO_5x5",
                RequestorsRank = "null",
                RequestorsName = playerName,
                NextApexUpdate = 84365595,
                MaxLeagueSize = 200
            };

            return e;
        }
    }
}
