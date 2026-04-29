using Draven.ServerModels;
using Draven.Structures;

using Newtonsoft.Json;
using RtmpSharp.IO.AMF3;
using RtmpSharp.Messaging;
using System.Linq;

namespace Draven.Messages.MasteryBookService
{
    using Draven.Structures.Platform.Summoner;

    class SaveMasteryBook : IMessage
    {
        public RemotingMessageReceivedEventArgs HandleMessage(object sender, RemotingMessageReceivedEventArgs e)
        {
            SummonerClient summonerSender = sender as SummonerClient;
            object[] bodyParameters = ToObjectArray(e.Body);

            MasteryBookDTO incomingBook = null;
            MasteryBookPageDTO incomingPage = null;

            foreach (var parameter in bodyParameters)
            {
                if (incomingBook == null)
                {
                    MasteryBookDTO parsedBook = ConvertBody<MasteryBookDTO>(parameter);
                    if (parsedBook != null && parsedBook.BookPages != null)
                    {
                        incomingBook = parsedBook;
                        continue;
                    }
                }

                if (incomingPage == null)
                {
                    MasteryBookPageDTO parsedPage = ConvertBody<MasteryBookPageDTO>(parameter);
                    if (parsedPage != null && parsedPage.PageId != 0)
                        incomingPage = parsedPage;
                }
            }

            e.ReturnRequired = true;
            e.Data = Draven.DatabaseManager.DatabaseManager.SaveMasteryBook(summonerSender._sumId, incomingBook, incomingPage);
            return e;
        }

        private static object[] ToObjectArray(object body)
        {
            if (body == null)
                return new object[0];

            object[] bodyArray = body as object[];
            if (bodyArray != null)
                return bodyArray;

            ArrayCollection collection = body as ArrayCollection;
            if (collection != null)
                return collection.Cast<object>().ToArray();

            return new object[] { body };
        }

        private static T ConvertBody<T>(object value) where T : class
        {
            if (value == null)
                return null;

            T typedValue = value as T;
            if (typedValue != null)
                return typedValue;

            try
            {
                return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
            }
            catch
            {
                return null;
            }
        }
    }
}
