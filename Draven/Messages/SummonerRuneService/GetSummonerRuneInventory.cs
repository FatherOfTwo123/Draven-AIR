using Draven.Structures;

using RtmpSharp.IO.AMF3;
using RtmpSharp.Messaging;
using System;
using System.Collections.Generic;

namespace Draven.Messages.SummonerRuneService
{
    using Draven.DatabaseManager;
    using Draven.ServerModels;
    using Draven.Structures.Platform.Summoner;

    class GetSummonerRuneInventory : IMessage
    { 
        public RemotingMessageReceivedEventArgs HandleMessage(object sender, RemotingMessageReceivedEventArgs e)
        {
            SummonerClient summonerSender = sender as SummonerClient;
            SummonerRuneInventory inventory = new SummonerRuneInventory
            {
                DateString = "Thu Jun 27 20:58:33 PDT 2013",
                SummonerId = summonerSender != null ? summonerSender._sumId : int.MaxValue - 1,
                SummonerRunes = new ArrayCollection()
            };

            foreach (var rune in DatabaseManager.AllRunes)
            {
                inventory.SummonerRunes.Add(new SummonerRune
                {
                    SummonerId = inventory.SummonerId,
                    Quantity = rune.Quantity,
                    RuneId = rune.ID,
                    PurchaseDate = new DateTime(2014, 5, 15, 12, 0, 0),
                    Purchased = new DateTime(2014, 5, 15, 12, 0, 0)
                });
            }

            e.ReturnRequired = true;
            e.Data = inventory;

            return e;
        }
    }
}
