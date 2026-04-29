using Draven.ServerModels;
using Draven.Structures;

using RtmpSharp.Messaging;

namespace Draven.Messages.MasteryBookService
{
    class GetMasteryBook : IMessage
    {
        public RemotingMessageReceivedEventArgs HandleMessage(object sender, RemotingMessageReceivedEventArgs e)
        {
            SummonerClient summonerSender = sender as SummonerClient;

            e.ReturnRequired = true;
            e.Data = Draven.DatabaseManager.DatabaseManager.GetMasteryBook(summonerSender._sumId);
            return e;
        }
    }
}
