using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stream_Sniper
{
    public class TwitchVideo
    {
        public string videoID { get; set; }
        public string videoTitle { get; set; }
        public string videoDescription { get; set; }
        public DateTime videoCreated { get; set; }
        public DateTime videoPublished { get; set; }
        public string videoLink { get; set; }
        public string videoThumbnial { get; set; }
        public string videoViews { get; set; }
        public TimeSpan videoDuration { get; set; }
    }
}
