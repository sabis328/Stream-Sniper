using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;

namespace Stream_Sniper
{
    public class WarzoneMatch
    {
        private string matchAPIbase = "https://www.callofduty.com/api/papi-client/crm/cod/v2/title/mw/platform/battle/fullMatch/wz/";
        private string matchAPIend = "/it";
        public DateTime MatchStart { get; set; }
        public TimeSpan MatchDuration { get; set; }

        public string MatchID { get; set; }

        
        public enum MatchType
        {
            Solos,
            Duos,
            Trios,
            Quads,
            Buy_Back_Solo,
            Buy_Back_BDuo,
            Buy_Back_Trio,
            Buy_Back_Quad,
            Limited
        }

        public string PlayerRatio { get; set; }
        public string PlayerKills { get; set; }
        public string PlayerPlacement { get; set; }

        public MatchType GameMode { get; set; }
        public enum MatchPlatform
        {
            Battle,
            Play,
            Xbox
        }

        public bool MatchParsed { get; private set; }

        public event EventHandler<bool> PlayerPaseFinish;

        public List<WarzonePlayer> MatchPlayers { get; set; }
        public void ParsePlayerData()
        {
            MatchParsed = false;
            MatchPlayers = new List<WarzonePlayer>();

            try
            {
                string ValidationBody;
                HttpWebRequest query = (HttpWebRequest)WebRequest.Create(matchAPIbase + MatchID + matchAPIend);
                query.Method = "GET";

                HttpWebResponse res = (HttpWebResponse)query.GetResponse();
                ValidationBody = new StreamReader(res.GetResponseStream()).ReadToEnd();

                int start = 0;
                int end = 0;
                int namesEnd = ValidationBody.LastIndexOf("username");

                start = ValidationBody.IndexOf("utcStartSeconds") + 17;
                end = ValidationBody.IndexOf(",", start);

                string MatchUTC = ValidationBody.Substring(start, end - start);

                MatchStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Convert.ToUInt32(MatchUTC));

                

                while (start < namesEnd)
                {

                    start = ValidationBody.IndexOf("username", start) + 11;
                    end = ValidationBody.IndexOf("brMissionStats", start) - 3;

                    string playerMeta = ValidationBody.Substring(start, end - start);
                    WarzonePlayer player = new WarzonePlayer();

                    if (playerMeta.Contains("clantag"))
                    {
                        int tempstart = playerMeta.IndexOf("clantag", 0) + 10;
                        int tempEnd = playerMeta.Length;
                        player.hasClanTag = true;
                        player.PlayerClanTag = playerMeta.Substring(tempstart, tempEnd - tempstart);
                    }

                    end = ValidationBody.IndexOf("uno", start) - 3;

                    player.PlayerName = ValidationBody.Substring(start, end - start);


                    MatchPlayers.Add(player);


                }
                MatchParsed = true;
                PlayerPaseFinish?.Invoke(this, MatchParsed);
            }
            catch
            {
                MatchParsed = false;
                PlayerPaseFinish?.Invoke(this, MatchParsed);
            }


        }
    }
}
