using Draven.Structures;

using RtmpSharp.Messaging;
using System;

namespace Draven.Messages.GameService
{
    class CreateTutorialGame : IMessage
    {
        public RemotingMessageReceivedEventArgs HandleMessage(object sender, RemotingMessageReceivedEventArgs e)
        {
            Console.WriteLine("[LOG] createTutorialGame stub hit. Real match launch still needs game-server integration.");

            e.ReturnRequired = true;
            e.Data = null;
            return e;
        }
    }
}
