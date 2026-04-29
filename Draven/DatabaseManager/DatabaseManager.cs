namespace Draven.DatabaseManager
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Net;

    using Draven.Structures.Platform.Catalog;
    using Draven.Structures.Platform.Summoner;

    using MySql.Data.MySqlClient;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using RtmpSharp.IO.AMF3;

    public static class DatabaseManager
    {
        public static ArrayCollection TalentTree { get; set; }
        public static ArrayCollection RuneTree { get; set; }
        public static List<DBRune> AllRunes { get; set; } = new List<DBRune>();
        public static Dictionary<double, MasteryBookDTO> MasteryBooks { get; set; } = new Dictionary<double, MasteryBookDTO>();

        public static MySqlConnection connection { get; set; }
        public static MySqlDataReader rdr = null;
        public static object Locker = new object();

        public static bool InitConnection()
        {
            try
            {
                Console.WriteLine("[LOG] Connecting to database");
                connection = new MySqlConnection("Database=" + Program.database + ";Data Source=" + Program.host + ";User Id = " + Program.user + "; Password = " + Program.pass + "; SslMode=none");
                connection.Open();
                Console.WriteLine("[LOG] Connection established");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[LOG] Couldn't connect to database.\n" + e.Message);
                return false;
            }
        }


        public static void InitMasteryAndRuneTree()
        {
            Dictionary<string, int> _masterySort = new Dictionary<string, int> { { "Offense", 1 }, { "Defense", 2 }, { "Utility", 3 } };

            Console.WriteLine("[LOG] Initialize Mastery and Rune Tree");
            using (WebClient client = new WebClient())
            {
                //Use old mastery tree matching the 4.x AIR client
                string MasteryData = client.DownloadString("https://ddragon.leagueoflegends.com/cdn/4.20.1/data/en_US/mastery.json");
                
                Masteries mData = JsonConvert.DeserializeObject<Masteries>(MasteryData);
                TalentTree = new ArrayCollection();
                int rowId = 1;

                //Parse the data and convert it into a type that is sent in the LoginDataPacket
                foreach (var mastery in mData.tree.OrderBy(x => _masterySort[x.Key]))
                {
                    TalentGroup group = new TalentGroup
                    {
                        Name = mastery.Key,
                        TalentRows = new ArrayCollection(),
                        TltGroupId = _masterySort[mastery.Key],
                        Index = _masterySort[mastery.Key] - 1,
                        Version = 1
                    };

                    for (int i = 0; i < mastery.Value.Count; i++)
                    {
                        ArrayCollection talentList = new ArrayCollection();
                        List<MasteryLite> masteryList = mastery.Value[i];
                        for (int j = 0; j < masteryList.Count; j++)
                        {
                            if (masteryList[j] == null)
                                continue;

                            var data = mData.data[masteryList[j].masteryId];
                            Talent t = new Talent
                            {
                                Index = j,
                                Name = data.name,
                                Level1Desc = GetMasteryDescription(data, 0),
                                Level2Desc = GetMasteryDescription(data, 1),
                                Level3Desc = GetMasteryDescription(data, 2),
                                Level4Desc = GetMasteryDescription(data, 3),
                                Level5Desc = GetMasteryDescription(data, 4),
                                GameCode = data.id,
                                TltId = data.id,
                                MaxRank = data.ranks,
                                MinLevel = 1,
                                MinTier = i + 1,
                                TalentGroupId = group.TltGroupId,
                                TalentRowId = rowId
                            };

                            if (data.preReq != "0")
                                t.PrereqTalentGameCode = Convert.ToInt32(data.preReq);

                            talentList.Add(t);
                        }

                        TalentRow row = new TalentRow
                        {
                            Index = i,
                            Talents = talentList,
                            PointsToActivate = i * 4,
                            TltRowId = rowId,
                            TltGroupId = group.TltGroupId
                        };

                        group.TalentRows.Add(row);
                        rowId += 1;
                    }

                    TalentTree.Add(group);
                }

                #region Rune Loading
                RuneTree = new ArrayCollection();

                //This code is... bad. 
                int Modifier = 0; //Skip 10, 20, 30
                int Take = 3; //Take one each loop and it will increase what it starts from (1 - 2 - 3)
                //Loop from 1-9 and do it 3 times to generate the red, yellow and blue runes
                for (int i = 1; i <= 9; i++)
                {
                    //At 10, 20 and 30 you need to increment the min level.
                    if ((i - 1) % 3 == 0 && i != 1)
                    {
                        Modifier += 1;
                    }

                    //The id goes past 9 so add the required amount that we have looped
                    int IdAdd = (Math.Abs(Take - 3) * 10);
                    //Take the amount that it has gone over
                    IdAdd -= Math.Abs(Take - 3);

                    RuneSlot slot = new RuneSlot()
                    {
                        Id = IdAdd + i,
                        RuneType = new RuneType(),
                        MinLevel = (3 * i + 1) - Take + Modifier
                    };

                    if (Take == 3)
                    {
                        slot.RuneType.Name = "Red";
                        slot.RuneType.Id = 1;
                    }
                    else if (Take == 2)
                    {
                        slot.RuneType.Name = "Yellow";
                        slot.RuneType.Id = 3;
                    }
                    else
                    {
                        slot.RuneType.Name = "Blue";
                        slot.RuneType.Id = 5;
                    }

                    RuneTree.Add(slot);

                    //Re do the loop, reset the modifier and take one from the take so the min level starts off one integer higher than last time
                    if (i == 9 && Take > 1)
                    {
                        Take -= 1;
                        i = 0;
                        Modifier = 0;
                    }
                }

                //Add black runes
                for (int i = 1; i <= 3; i++)
                {
                    RuneSlot slot = new RuneSlot()
                    {
                        Id = 27 + i, //Start id from 27 since thats where we left off
                        RuneType = new RuneType
                        {
                            Id = 7,
                            Name = "Black"
                        },
                        MinLevel = 10 * i
                    };

                    RuneTree.Add(slot);
                }

                string RuneDataJson = client.DownloadString("https://ddragon.leagueoflegends.com/cdn/7.12.1/data/en_US/rune.json");
                JObject runeData = JObject.Parse(RuneDataJson);
                AllRunes = new List<DBRune>();

                foreach (var rune in runeData["data"])
                {
                    JProperty runeProperty = rune as JProperty;

                    if (runeProperty == null)
                        continue;

                    int runeId = Convert.ToInt32(runeProperty.Name);
                    string runeType = runeProperty.Value["rune"]?["type"]?.ToString() ?? "red";

                    AllRunes.Add(new DBRune
                    {
                        ID = runeId,
                        Quantity = runeType == "black" ? 3 : 9
                    });
                }

                #endregion
            }
           
        }



        private static readonly DateTime DefaultMasteryCreateDate = new DateTime(2016, 08, 11, 12, 00, 00);
        private const string DefaultMasteryDateString = "Wed Jul 17 23:05:42 PDT 2013";

        public static MasteryBookDTO GetMasteryBook(double summonerId)
        {
            lock (Locker)
            {
                MasteryBookDTO masteryBook;
                if (!MasteryBooks.TryGetValue(summonerId, out masteryBook))
                {
                    masteryBook = CreateDefaultMasteryBook(summonerId);
                    MasteryBooks[summonerId] = masteryBook;
                }

                return masteryBook;
            }
        }

        public static ArrayCollection GetCurrentMasteryEntries(double summonerId)
        {
            MasteryBookDTO masteryBook = GetMasteryBook(summonerId);
            if (masteryBook == null || masteryBook.BookPages == null)
                return new ArrayCollection();

            foreach (var rawPage in masteryBook.BookPages)
            {
                MasteryBookPageDTO page = rawPage as MasteryBookPageDTO;
                if (page == null || !page.Current)
                    continue;

                return page.Entries ?? new ArrayCollection();
            }

            return new ArrayCollection();
        }

        public static MasteryBookDTO SaveMasteryBook(double summonerId, MasteryBookDTO incomingBook, MasteryBookPageDTO incomingPage)
        {
            lock (Locker)
            {
                MasteryBookDTO masteryBook = GetMasteryBook(summonerId);

                if (incomingBook != null && incomingBook.BookPages != null && incomingBook.BookPages.Count > 0)
                {
                    foreach (var rawPage in incomingBook.BookPages)
                    {
                        MasteryBookPageDTO page = ConvertValue<MasteryBookPageDTO>(rawPage);
                        if (page == null)
                            continue;

                        UpsertMasteryBookPage(masteryBook, NormalizeMasteryBookPage(page, summonerId));
                    }
                }
                else if (incomingPage != null)
                {
                    UpsertMasteryBookPage(masteryBook, NormalizeMasteryBookPage(incomingPage, summonerId));
                }

                EnsureCurrentMasteryPage(masteryBook);
                return masteryBook;
            }
        }

        private static MasteryBookDTO CreateDefaultMasteryBook(double summonerId)
        {
            ArrayCollection pages = new ArrayCollection();

            for (int i = 1; i <= 20; i++)
            {
                pages.Add(new MasteryBookPageDTO
                {
                    Current = i == 1,
                    CreateDate = DefaultMasteryCreateDate,
                    Name = "Mastery Page " + i,
                    PageId = i,
                    SummonerId = summonerId,
                    Entries = new ArrayCollection()
                });
            }

            return new MasteryBookDTO
            {
                SummonerId = summonerId,
                DateString = DefaultMasteryDateString,
                BookPages = pages
            };
        }

        private static MasteryBookPageDTO NormalizeMasteryBookPage(MasteryBookPageDTO page, double summonerId)
        {
            int pageId = page.PageId > 0 ? page.PageId : 1;
            MasteryBookPageDTO normalizedPage = new MasteryBookPageDTO
            {
                Current = page.Current,
                CreateDate = page.CreateDate == default(DateTime) ? DefaultMasteryCreateDate : page.CreateDate,
                Name = String.IsNullOrWhiteSpace(page.Name) ? "Mastery Page " + pageId : page.Name,
                PageId = pageId,
                SummonerId = summonerId,
                Entries = new ArrayCollection()
            };

            if (page.Entries == null)
                return normalizedPage;

            foreach (var rawEntry in page.Entries)
            {
                TalentEntry entry = ConvertValue<TalentEntry>(rawEntry);
                if (entry == null)
                    continue;

                int talentId = entry.TalentId;
                if (talentId == 0 && entry.Talent != null)
                    talentId = entry.Talent.GameCode;

                if (talentId == 0 || entry.Rank <= 0)
                    continue;

                normalizedPage.Entries.Add(new TalentEntry
                {
                    Rank = entry.Rank,
                    TalentId = talentId,
                    Talent = GetTalentById(talentId),
                    SummonerId = summonerId
                });
            }

            return normalizedPage;
        }

        private static void UpsertMasteryBookPage(MasteryBookDTO masteryBook, MasteryBookPageDTO page)
        {
            if (masteryBook == null || page == null)
                return;

            int replaceIndex = -1;
            for (int i = 0; i < masteryBook.BookPages.Count; i++)
            {
                MasteryBookPageDTO existingPage = masteryBook.BookPages[i] as MasteryBookPageDTO;
                if (existingPage == null)
                    continue;

                if (page.Current)
                    existingPage.Current = false;

                if (existingPage.PageId == page.PageId)
                    replaceIndex = i;
            }

            if (replaceIndex >= 0)
                masteryBook.BookPages[replaceIndex] = page;
            else
                masteryBook.BookPages.Add(page);
        }

        private static void EnsureCurrentMasteryPage(MasteryBookDTO masteryBook)
        {
            if (masteryBook == null || masteryBook.BookPages == null || masteryBook.BookPages.Count == 0)
                return;

            MasteryBookPageDTO firstPage = null;
            bool foundCurrent = false;

            foreach (var rawPage in masteryBook.BookPages)
            {
                MasteryBookPageDTO page = rawPage as MasteryBookPageDTO;
                if (page == null)
                    continue;

                if (firstPage == null)
                    firstPage = page;

                if (!page.Current)
                    continue;

                if (!foundCurrent)
                {
                    foundCurrent = true;
                    continue;
                }

                page.Current = false;
            }

            if (!foundCurrent && firstPage != null)
                firstPage.Current = true;
        }

        private static Talent GetTalentById(int talentId)
        {
            if (TalentTree == null)
                return null;

            foreach (var rawGroup in TalentTree)
            {
                TalentGroup group = rawGroup as TalentGroup;
                if (group == null || group.TalentRows == null)
                    continue;

                foreach (var rawRow in group.TalentRows)
                {
                    TalentRow row = rawRow as TalentRow;
                    if (row == null || row.Talents == null)
                        continue;

                    foreach (var rawTalent in row.Talents)
                    {
                        Talent talent = rawTalent as Talent;
                        if (talent == null)
                            continue;

                        if (talent.GameCode == talentId || talent.TltId == talentId)
                            return talent;
                    }
                }
            }

            return null;
        }

        private static string GetMasteryDescription(MasteryData data, int rankIndex)
        {
            if (data == null)
                return String.Empty;

            if (data.description == null || data.description.Count == 0)
                return data.name ?? String.Empty;

            if (rankIndex < data.description.Count)
                return data.description[rankIndex];

            return data.description[data.description.Count - 1];
        }

        private static T ConvertValue<T>(object value) where T : class
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

        public static List<int> ProfileIcons = new List<int>();

        public static int NormalizeProfileIconId(int iconId)
        {
            if (ProfileIcons.Contains(iconId))
                return iconId;

            if (ProfileIcons.Count > 0)
                return ProfileIcons[0];

            return 1;
        }

        public static void InitProfileIcons()
        {
            using (WebClient client = new WebClient())
            {
                Console.WriteLine("[LOG] Initialize Profile Icons");

                string ProfileData = client.DownloadString("https://ddragon.leagueoflegends.com/cdn/4.20.1/data/en_US/profileicon.json");

                ProfileJsonTree mData = JsonConvert.DeserializeObject<ProfileJsonTree>(ProfileData);

                foreach (var iconData in mData.data)
                {
                        ProfileIcons.Add(iconData.Value.id);
                }
            }
        }

        public static Dictionary<string, string> getAccountData(string user, string pass)
        {
            MySqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM accounts WHERE username='" + user + "' AND password='" + pass + "'";
            MySqlDataReader reader = cmd.ExecuteReader();
            DataTable dtCustomers = new DataTable();
            dtCustomers.Load(reader);
            var dataArray = new Dictionary<string, string>();
            foreach (DataRow row in dtCustomers.Rows)
            {
                dataArray["id"] = row["id"].ToString();
                dataArray["summonerId"] = row["summonerId"].ToString();
                dataArray["RP"] = row["RP"].ToString();
                dataArray["IP"] = row["IP"].ToString();
                dataArray["banned"] = row["isBanned"].ToString();
            }
            return dataArray;
        }

        public static Dictionary<string, string> getSummonerData(string sumId)
        {
            MySqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM summoner WHERE id='" + sumId + "'";
            MySqlDataReader reader = cmd.ExecuteReader();
            DataTable dtCustomers = new DataTable();
            dtCustomers.Load(reader);
            var dataArray = new Dictionary<string, string>();
            foreach (DataRow row in dtCustomers.Rows)
            {
                dataArray["id"] = row["id"].ToString();
                dataArray["summonerName"] = row["summonerName"].ToString();
                dataArray["icon"] = row["icon"].ToString();
            }
            return dataArray;
        }

        public class DBChampions
        {
            public int ID { get; set; }
            public bool IsFreeToPlay { get; set; }
        }

        public class DBRune
        {
            public int ID { get; set; }
            public int Quantity { get; set; }
        }

        public static List<DBChampions> getAllChampions()
        {
            MySqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM champions";
            MySqlDataReader reader = cmd.ExecuteReader();
            DataTable dtChampions = new DataTable();
            dtChampions.Load(reader);
            var dataArray = new List<DBChampions>();
            foreach (DataRow row in dtChampions.Rows)
            {
                dataArray.Add(new DBChampions() { ID = Convert.ToInt32(row["id"].ToString()), IsFreeToPlay = Convert.ToBoolean(Convert.ToInt32(row["freeToPlay"].ToString())) });
            }
            return dataArray;
        }

        public static List<int> getAllChampionSkinsForId(int id)
        {
            MySqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM championSkins WHERE championId='"+ id + "'";
            MySqlDataReader reader = cmd.ExecuteReader();
            DataTable dtSkins = new DataTable();
            dtSkins.Load(reader);
            var dataArray = new List<int>();
            foreach (DataRow row in dtSkins.Rows)
            {
                dataArray.Add(Convert.ToInt32(row["id"].ToString()));
            }
            return dataArray;
        }
        

        public static void updateSummonerIconById(int sumId, int iconId)
        {
            try {
                MySqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE summoner SET icon='" + iconId + "' WHERE id='" + sumId + "'";
                cmd.ExecuteNonQuery();
            } catch (MySqlException sex)
            {
                Console.WriteLine(sex.Message);
            }
        }

        public static bool checkAccount(string user, string pass)
        {
            try
            {
                MySqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT count(*) FROM accounts WHERE username='" + user + "' AND password='" + pass + "'";
                int userCount = Convert.ToInt32(cmd.ExecuteScalar());
                if (userCount > 0)
                    return true;
                else
                    return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
    }
}
