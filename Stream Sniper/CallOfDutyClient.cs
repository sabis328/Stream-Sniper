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
    public class CallOfDutyClient
    {

        public CookieContainer ActivisionCredentialContainer { get; set; }
        public string ActivisionEmail { get; set; }
        public string ActivisionPassword { get; set; }
        public string BattlenetTag { get; set; }

        
        public enum ActiSignInStats
        {
            CookiesFound,
            Validated,
            BadCredentials,
            FailedToFindID,
            Error
        }
        private string XSRFToken;
        public ActiSignInStats ValiStatus { get; private set; }
        public event EventHandler<ActiSignInStats> ValidationUpdate;

        //Sign in to activision
        public void ValidateUser(string email, string password)
        {
            ValiStatus = ActiSignInStats.Error;

            ActivisionEmail = email;
            ActivisionPassword = password;

            ActivisionCredentialContainer = new CookieContainer();

            try
            {
                string ValidationBody;

                HttpWebRequest query = (HttpWebRequest)WebRequest.Create("https://profile.callofduty.com/cod/login");
                query.Method = "GET";
                query.CookieContainer = ActivisionCredentialContainer;

                HttpWebResponse res = (HttpWebResponse)query.GetResponse();

                ActivisionCredentialContainer.Add(res.Cookies);

                foreach (Cookie c in res.Cookies)
                {
                    System.Diagnostics.Debug.Print(c.Name);
                    if (c.Name == "XSRF-TOKEN")
                    {
                        XSRFToken = c.Value;
                    }
                }
                ValiStatus = ActiSignInStats.CookiesFound;
                ValidationUpdate?.Invoke(this, ValiStatus);

                query = (HttpWebRequest)WebRequest.Create("https://profile.callofduty.com/do_login?new_SiteId=cod");
                query.Method = "POST";
                query.CookieContainer = ActivisionCredentialContainer;
                query.ContentType = "application/x-www-form-urlencoded";

                var postData = "username=" + Uri.EscapeDataString(ActivisionEmail);
                postData += "&remember_me=true&password=" + Uri.EscapeDataString(ActivisionPassword) + "&_csrf=" + XSRFToken;
                var data = Encoding.ASCII.GetBytes(postData);

                query.ContentLength = data.Length;

                using (var stream = query.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                res = (HttpWebResponse)query.GetResponse();

                ActivisionCredentialContainer.Add(res.Cookies);

                System.Diagnostics.Debug.Print(res.ResponseUri.ToString());
                if (!res.ResponseUri.ToString().Contains("login?failure=true"))
                {
                    ValiStatus = ActiSignInStats.FailedToFindID;

                    query = (HttpWebRequest)WebRequest.Create("https://www.callofduty.com/api/papi-client/crm/cod/v2/identities");
                    query.Method = "GET";
                    query.CookieContainer = ActivisionCredentialContainer;

                    res = (HttpWebResponse)query.GetResponse();
                    ValidationBody = new StreamReader(res.GetResponseStream()).ReadToEnd();

                    int start = 0;
                    int end = 0;

                   
                    start = ValidationBody.IndexOf("platform\":\"battle", 0);
                    
                    end = ValidationBody.IndexOf("username\":\"", start);
                   
                    start = end + 11;
                    end = ValidationBody.IndexOf("activeDate", start) - 3;

                    BattlenetTag = ValidationBody.Substring(start, end - start);


                   
                    ActivisionCredentialContainer.Add(res.Cookies);

                    
                    ValiStatus = ActiSignInStats.Validated;

                    ValidationUpdate?.Invoke(this, ValiStatus);

                }
                else { ValiStatus = ActiSignInStats.BadCredentials; ValidationUpdate?.Invoke(this, ValiStatus); }


            }
            catch { ValiStatus = ActiSignInStats.Error; ValidationUpdate?.Invoke(this, ValiStatus); }
        }




        //Load Recent warzone matches
        public List<WarzoneMatch> RecentWarzoneMatches { get; set; }
        public bool RecentMatchesLoaded;
        public event EventHandler<bool> RecentLoadComplete;
        public void LoadRecentWarzone()
        {
            string recentAPI = string.Format("https://my.callofduty.com/api/papi-client/crm/cod/v2/title/mw/platform/battle/gamer/{0}/matches/wz/start/0/end/0/details",
               Uri.EscapeDataString(BattlenetTag));

            RecentWarzoneMatches = new List<WarzoneMatch>();
            try
            {

                string ValidationBody;
                HttpWebRequest query = (HttpWebRequest)WebRequest.Create(recentAPI);
                query.Method = "GET";
                query.CookieContainer = ActivisionCredentialContainer;
                HttpWebResponse res = (HttpWebResponse)query.GetResponse();
                ValidationBody = new StreamReader(res.GetResponseStream()).ReadToEnd();

                int start = 0;
                int end = 0;
                int textEnd = ValidationBody.LastIndexOf("teamPlacement");

                //System.Diagnostics.Debug.Print(ValidationBody);

               
                while (start < textEnd)
                {
                    WarzoneMatch match = new WarzoneMatch();

                    start = ValidationBody.IndexOf("mode", end) + 7;
                    end = ValidationBody.IndexOf(",", start) - 1;
                    string tempType = ValidationBody.Substring(start, end - start);

                    switch (tempType)
                    {
                        case "br_brsolos":
                            match.GameMode = WarzoneMatch.MatchType.Solos;
                            break;
                        case "br_brbbsolos":
                            match.GameMode = WarzoneMatch.MatchType.Buy_Back_Solo;
                            break;
                        case "br_brduos":
                            match.GameMode = WarzoneMatch.MatchType.Duos;
                            break;
                        case "br_brbbduos":
                            match.GameMode = WarzoneMatch.MatchType.Buy_Back_BDuo;
                            break;
                        case "br_brtrios":
                            match.GameMode = WarzoneMatch.MatchType.Trios;
                            break;
                        case "br_brbbtrios":
                            match.GameMode = WarzoneMatch.MatchType.Buy_Back_Trio;
                            break;
                        case "br_brquad":
                            match.GameMode = WarzoneMatch.MatchType.Quads;
                            break;
                        case "br_brbbquad":
                            match.GameMode = WarzoneMatch.MatchType.Buy_Back_Quad;
                            break;
                        case "br_rebirth_rbrthtrios":
                            match.GameMode = WarzoneMatch.MatchType.Limited;
                            break;
                        case "br_rebirth_rbrthquads":
                            match.GameMode = WarzoneMatch.MatchType.Limited;
                            break;
                        case "br_rebirth_rbrthduos":
                            match.GameMode = WarzoneMatch.MatchType.Limited;
                            break;
                        default:
                            match.GameMode = WarzoneMatch.MatchType.Limited;
                            break;
                    }


                    start = ValidationBody.IndexOf("matchID", start) + 10;
                    end = ValidationBody.IndexOf("duration", start) - 3;

                   
                    match.MatchID = ValidationBody.Substring(start, end - start);

                   
                    start = ValidationBody.IndexOf("playerStats", end) + 22;
                    end = ValidationBody.IndexOf(",", start);

                    match.PlayerKills = ValidationBody.Substring(start, end - start);

                    start = ValidationBody.IndexOf("kdRatio", end) + 9;
                    end = ValidationBody.IndexOf(",", start);

                    match.PlayerRatio = ValidationBody.Substring(start, end - start);
                    if(match.PlayerRatio.Length > 5)
                    {
                        match.PlayerRatio = match.PlayerRatio.Substring(0, 4);
                    }

                    start = ValidationBody.IndexOf("teamPlacement", end) + 15;
                    end = ValidationBody.IndexOf(",", start);

                    match.PlayerPlacement = ValidationBody.Substring(start, end - start);


                    RecentWarzoneMatches.Add(match);
                    System.Diagnostics.Debug.Print("Match added : " + match.MatchID);
                }
                RecentMatchesLoaded = true;
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.Print(e.ToString());
                RecentMatchesLoaded = false;
            }

            RecentLoadComplete?.Invoke(this, RecentMatchesLoaded);
        }



    }
}
