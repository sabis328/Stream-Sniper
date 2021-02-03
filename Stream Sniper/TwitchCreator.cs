using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stream_Sniper
{
    public class TwitchCreator
    {
        public enum Channel_result
        {
            Parsed,
            Failed
        }
        public Channel_result is_Parsed { get; private set; }
        public string Username { get; set; }
        public string User_ID_Code { get; set; }
        public bool Live_Now { get; set; }
        public string Thumbnail_Link { get; set; }
        public string Stream_Title { get; set; }
        public DateTime Stream_Start { get; set; }
        public List<TwitchVideo> Channel_Saved_Videos { get; set; }
        
        public DateTime Check_Against { get; set; }
        public void Parse_Channel_Data(string ChannelJSON)
        {
            try
            {
                System.Diagnostics.Debug.Print("Reading channel JSON : " + ChannelJSON);
                int start = 0;
                int end = 0;

                start = ChannelJSON.IndexOf("display_name") + 15;
                end = ChannelJSON.IndexOf("game_id") - 3;

                Username = ChannelJSON.Substring(start, end - start);

                start = ChannelJSON.IndexOf("id", end + 13) + 5;
                end = ChannelJSON.IndexOf("is_live") - 3;

                User_ID_Code = ChannelJSON.Substring(start, end - start);

                start = ChannelJSON.IndexOf("is_live", start) + 9;
                end = ChannelJSON.IndexOf(",", start);

                if (ChannelJSON.Substring(start, end - start) == "true")
                {
                    Live_Now = true;
                }
                else
                {
                    Live_Now = false;
                }

                start = ChannelJSON.IndexOf("thumbnail_url", end) + 16;
                end = ChannelJSON.IndexOf("\",\"title", start);

                Thumbnail_Link = ChannelJSON.Substring(start, end - start);

                start = ChannelJSON.IndexOf("title", end) + 8;
                end = ChannelJSON.IndexOf("started_at", start) - 3;

                Stream_Title = ChannelJSON.Substring(start, end - start);

                if (Live_Now)
                {
                    start = ChannelJSON.IndexOf("started_at") + 13;
                    end = ChannelJSON.Length - 1;
                    string tempHold = ChannelJSON.Substring(start, end - start);

                    Stream_Start = Convert.ToDateTime(tempHold);

                    //System.Diagnostics.Debug.Print(ChannelJSON.ToString());

                }
                else
                {

                }

                is_Parsed = Channel_result.Parsed;
            }
            catch
            {
                is_Parsed = Channel_result.Failed;
            }
        }
    }
}
