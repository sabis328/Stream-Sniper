using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Stream_Sniper
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            _TwitchClient.Twitch_Validation_Event += _TwitchClient_Twitch_Validation_Event;
            _TwitchClient.Search_Channel_Complete += _TwitchClient_Search_Channel_Complete;
            _Codclient.RecentLoadComplete += _Codclient_RecentLoadComplete;
            _Codclient.ValidationUpdate += _Codclient_ValidationUpdate;
            
        }

        #region Cod Client Event Handling
        private void _Codclient_ValidationUpdate(object sender, CallOfDutyClient.ActiSignInStats e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    _Codclient_ValidationUpdate(sender,e);
                });
                return;
            }
            switch (e)
            {

                case CallOfDutyClient.ActiSignInStats.CookiesFound:
                    progressBar1.Value = 2;
                    StatusLabel.Text = "Activision token found, attempting sign in ";
                    break;
                case CallOfDutyClient.ActiSignInStats.Validated:
                    progressBar1.Value = progressBar1.Maximum;
                    StatusLabel.Text = "User signed in, attempting to load matches";
                    label5.Text = "Battle Net : " + _Codclient.BattlenetTag;
                    System.Threading.Thread.Sleep(200);
                    progressBar1.Value = 0; progressBar1.Maximum = 0;
                    Task.Run(() => _Codclient.LoadRecentWarzone());
                    break;
                case CallOfDutyClient.ActiSignInStats.BadCredentials:
                    progressBar1.Value = 0;
                    StatusLabel.Text = "Incorrect email or password for Activision account";
                    break;
                case CallOfDutyClient.ActiSignInStats.FailedToFindID:
                    progressBar1.Value =0 ;
                    StatusLabel.Text = "User signed in, failed to find linked battle net account";
                    break;
                case CallOfDutyClient.ActiSignInStats.Error:
                    progressBar1.Value = 0;
                    StatusLabel.Text = "Error when signing in, please try again";
                    break;
            }
        }

        private void _Codclient_RecentLoadComplete(object sender, bool e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    _Codclient_RecentLoadComplete(sender, e);
                });
                return;
            }
            
            if(e)
            {
                if(_Codclient.RecentWarzoneMatches.Count > 0)
                {
                    progressBar1.Maximum = _Codclient.RecentWarzoneMatches.Count;
                    progressBar1.Value = 0;
                    StatusLabel.Text = "Loading recent matches : 0/" + progressBar1.Maximum;
                    foreach(WarzoneMatch M in _Codclient.RecentWarzoneMatches)
                    {
                        M.PlayerPaseFinish += M_PlayerPaseFinish;
                        Task.Run(() => M.ParsePlayerData());
                    }
                }
            }
            else { StatusLabel.Text = "Failed to load recent matches"; progressBar1.Value = 0; }
        }

        private void M_PlayerPaseFinish(object sender, bool e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    M_PlayerPaseFinish(sender, e);
                });
                return;
            }

            progressBar1.Value += 1;
            StatusLabel.Text = "Loading recent matches : " + progressBar1.Value + "/" + progressBar1.Maximum;
            
            if(progressBar1.Value == progressBar1.Maximum)
            {
                System.Threading.Thread.Sleep(200);
                StatusLabel.Text = "All matches loaded, checking for possible streamers";
                progressBar1.Value = 0;

                _Codclient.RecentWarzoneMatches.Sort((x, y) => y.MatchStart.CompareTo(x.MatchStart));

                foreach(WarzoneMatch match in _Codclient.RecentWarzoneMatches)
                {
                    if(match.MatchParsed)
                    {
                        TreeNode MatchNode = new TreeNode();
                        MatchNode.Text = match.GameMode.ToString() + " , Place #" + match.PlayerPlacement;
                        MatchNode.Nodes.Add("Total Kills : " + match.PlayerKills);
                        MatchNode.Nodes.Add("KD Ratio : " + match.PlayerRatio);
                        MatchNode.Nodes.Add(match.MatchStart.ToString());

                        MatchNode.Tag = match;

                        MatchNode.Expand();

                        foreach (WarzonePlayer player in match.MatchPlayers)
                        {
                            TreeNode playerNode = new TreeNode();
                            if (player.hasClanTag)
                            {
                                playerNode.Text = "[" + player.PlayerClanTag + "]" + player.PlayerName;
                            }
                            else
                            {
                                playerNode.Text = player.PlayerName;
                            }

                            if (playerNode.Text.ToLower().Contains("ttv") || playerNode.Text.ToLower().Contains("twtch")|| playerNode.Text.ToLower().Contains("t.tv") || playerNode.Text.ToLower().Contains("_tv")
                            || playerNode.Text.ToLower().Contains("twitch") || playerNode.Text.ToLower().Contains("twitch") || playerNode.Text.ToLower().Contains("sub2") || playerNode.Text.ToLower().Contains("live"))
                            {
                                playerNode.ForeColor = Color.LightBlue;
                                MatchNode.ForeColor = Color.LightBlue;

                                TreeNode StreamerNode = new TreeNode();
                                StreamerNode.Text = playerNode.Text + " ; " + match.MatchStart.ToString();
                                StreamerNode.Tag = player;
                                TreeNode StreamerSubnode = new TreeNode();
                                StreamerSubnode.Tag = match.MatchStart;
                                StreamerSubnode.Text  = match.MatchStart.ToString();

                                StreamerNode.Nodes.Add(StreamerSubnode);
                                treeView2.Nodes.Add(StreamerNode);
                            }

                            playerNode.Tag = player;
                            MatchNode.Nodes[2].Nodes.Add(playerNode);

                           
                        }
                        treeView1.Nodes.Add(MatchNode);
                    }
                }

                treeView1.Nodes[0].EnsureVisible();

                progressBar1.Maximum = treeView2.Nodes.Count;
                System.Diagnostics.Debug.Print("Streamers to find   :   " + treeView2.Nodes.Count);
                progressBar1.Value = 0;

                Task.Run(() => ProcessPossibleStreamers());
            }
        }

        private void ProcessPossibleStreamers()
        {


            Matched_Streams_Nodes = new List<TreeNode>();
            foreach(TreeNode StreamerNode in treeView2.Nodes)
            {
                System.Diagnostics.Debug.Print("Possible Steamer : " + StreamerNode.Text);
                WarzonePlayer player = (WarzonePlayer)StreamerNode.Tag;
                string findChan = player.PlayerName;
                findChan = findChan.ToLower().Replace("twitch_", "");
                findChan = findChan.ToLower().Replace("twitch", "");
                findChan = findChan.ToLower().Replace("_ttv", "");
                findChan = findChan.ToLower().Replace("ttv_", "");
                findChan = findChan.ToLower().Replace("ttv", "");
                findChan = findChan.ToLower().Replace("_tv", "");
                findChan = findChan.Trim();
                findChan = Uri.EscapeDataString(findChan);


                Twitch_Client chanSnipe = new Twitch_Client();
                chanSnipe.Twitch_ClientID = _TwitchClient.Twitch_ClientID;
                chanSnipe.Twitch_Client_Token = _TwitchClient.Twitch_Client_Token;
                chanSnipe.Twitch_Client_Secret = _TwitchClient.Twitch_Client_Secret;

                
                System.Diagnostics.Debug.Print("Searching for channel : " + findChan);
                chanSnipe.Twitch_Find_Channels(findChan, true);

                if (chanSnipe.Found_Channels.Count > 0)
                {
                    TwitchCreator twitch_Channel = chanSnipe.Found_Channels[0];
                    twitch_Channel.Check_Against = (DateTime)StreamerNode.Nodes[0].Tag;
                    chanSnipe.Videos_Loaded += _TwitchClient_Videos_Loaded;
                    chanSnipe.Videos_Failed_Load += _TwitchClient_Videos_Failed_Load;
                    chanSnipe.Load_Channel_Videos(twitch_Channel);
                   
                }
                else
                {
                    Addprogress();
                }

            }
        }


        #endregion


        #region Twitch Client Event Handling
        private void _TwitchClient_Search_Channel_Complete(object sender, Twitch_Client.Channel_Search_Result e)
        {

            if (this.InvokeRequired)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    _TwitchClient_Search_Channel_Complete(sender, e);
                });
                return;
            }

            if (e == Twitch_Client.Channel_Search_Result.All_Parsed)
              {
                progressBar1.Value += 1;
                int index = progressBar1.Value - 1;

                System.Diagnostics.Debug.Print("Loading channel videos for : " + _TwitchClient.Found_Channels[index].Username.ToString());
                _TwitchClient.Load_Channel_Videos(_TwitchClient.Found_Channels[index]);
              }
        }

        private void _TwitchClient_Videos_Failed_Load(object sender, TwitchCreator e)
        {
            Addprogress();
        }

        private void Addprogress()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    Addprogress();
                });
                return;
            }
            progressBar1.Value += 1;
            System.Diagnostics.Debug.Print("Streamers processed   :   " + progressBar1.Value + "/" + progressBar1.Maximum);
            if (progressBar1.Value == progressBar1.Maximum)
            {
                treeView2.Nodes.Clear();
                foreach(TreeNode N in Matched_Streams_Nodes)
                {
                    treeView2.Nodes.Add(N);
                }

                progressBar1.Value = 0;

                StatusLabel.Text = "Streamers found : " + treeView1.Nodes.Count.ToString();

            }
        }


        private List<TreeNode> Matched_Streams_Nodes;
        private void _TwitchClient_Videos_Loaded(object sender, TwitchCreator e)
        {
            DateTime CheckAgainst = e.Check_Against;
            System.Diagnostics.Debug.Print("Checking loaded videos for " + e.Username + "   :   " + e.Channel_Saved_Videos.Count.ToString());
            foreach (TwitchVideo video in e.Channel_Saved_Videos)
            {
                DateTime AccountforDuration = video.videoCreated;
                AccountforDuration += video.videoDuration;

                if (e.Live_Now && e.Stream_Start.Date == e.Check_Against.Date)
                {
                    TreeNode matchedStream = new TreeNode();

                    matchedStream.Text = e.Username;

                    matchedStream.ForeColor = Color.LightBlue;

                    matchedStream.Nodes.Add("Check later, user is live now");

                    DateTime localTime = CheckAgainst.AddHours(-5);

                    matchedStream.Nodes.Add(localTime.ToString());

                    Matched_Streams_Nodes.Add(matchedStream);
                    break;
                }
                //System.Diagnostics.Debug.Print("Video from user at " + video.videoCreated.Date.ToString() + " , comparing to " + CheckAgainst.Date.ToString());
                if (video.videoCreated.Date.ToString() == CheckAgainst.Date.ToString() || CheckAgainst.Ticks < AccountforDuration.Ticks)
                {


                    System.Diagnostics.Debug.Print("Day matched compaing time of user " + video.videoCreated.TimeOfDay + " duration of stream " + AccountforDuration.TimeOfDay + " , to time of game " + CheckAgainst.TimeOfDay); ; ;

                    if (CheckAgainst.Ticks > video.videoCreated.Ticks && CheckAgainst.Ticks < AccountforDuration.Ticks)
                    {
                        System.Diagnostics.Debug.Print("MATCH FOUND  " + e.Username + "    " + video.videoCreated.ToString());

                        System.Diagnostics.Debug.Print("Check time of day : " + CheckAgainst.TimeOfDay.ToString() + "    vs    Video created time of day : " + video.videoCreated.TimeOfDay.ToString());
                        TimeSpan offset = CheckAgainst.TimeOfDay - video.videoCreated.TimeOfDay;

                        System.Diagnostics.Debug.Print("offseting : " + offset.ToString());

                        if (offset.Hours < 0) 
                        {
                            offset = offset.Add(new TimeSpan(24, 0, 0));
                            System.Diagnostics.Debug.Print("Corrected Negative offset : " + offset.ToString());
                        }

                        
                        TreeNode matchedStream = new TreeNode();

                        matchedStream.Text = e.Username;

                        matchedStream.ForeColor = Color.LightBlue;

                        matchedStream.Nodes.Add(video.videoLink + "?t=" + offset.Hours + "h" + offset.Minutes + "m" + offset.Seconds + "s");

                        //DateTime localTime = CheckAgainst.AddHours(-5);

                        matchedStream.Nodes.Add(CheckAgainst.ToString());

                        Matched_Streams_Nodes.Add(matchedStream);
                    }
                    else
                    {
                        System.Diagnostics.Debug.Print("User " + e.Username + " , video : " + video.videoTitle + " :  " + video.videoCreated.ToString() + "  outside of matchtime  " + e.Check_Against.ToString() + "  video is " + video.videoDuration.ToString());
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Print("User " + e.Username + " , video : " + video.videoTitle + " :  " + video.videoCreated.ToString() + "Match Date" + e.Check_Against.ToString());
                }

               
            }

            Addprogress();


        }

        private void _TwitchClient_Twitch_Validation_Event(object sender, Twitch_Client.Twitch_Validation_Status e)
        {
            //throw new NotImplementedException();
            System.Diagnostics.Debug.Print("Twitch validation complete");
        }

        #endregion
        private Twitch_Client _TwitchClient = new Twitch_Client();
        private CallOfDutyClient _Codclient = new CallOfDutyClient();


        private void button1_Click(object sender, EventArgs e)
        {
            switch (_Codclient.ValiStatus)
            {
                case CallOfDutyClient.ActiSignInStats.Validated:

                    System.Diagnostics.Debug.Print("User already signed in");
                    break;
                case CallOfDutyClient.ActiSignInStats.BadCredentials:
                    progressBar1.Maximum = 4;
                    StatusLabel.Text = "Attempting to sign in";
                    Task.Run(() => _Codclient.ValidateUser(textBox1.Text, textBox2.Text));
                    System.Diagnostics.Debug.Print("Attempting to revalidate");
                    break;
                default:

                    Task.Run(() => _TwitchClient.Validate_Twitch_Client());
                    
                    progressBar1.Maximum = 4;
                    StatusLabel.Text = "Attempting to sign in";
                    Task.Run(() => _Codclient.ValidateUser(textBox1.Text, textBox2.Text));
                    System.Diagnostics.Debug.Print("Attempting validation");
                    break;
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(_Codclient.ValiStatus == CallOfDutyClient.ActiSignInStats.Validated)
            {
                treeView2.Nodes.Clear();
                treeView1.Nodes.Clear();
                StatusLabel.Text = "Refreshing matches ";
                System.Threading.Thread.Sleep(200);
                progressBar1.Value = 0; progressBar1.Maximum = 0;
                Task.Run(() => _Codclient.LoadRecentWarzone());
            }

           
        }

        private void treeView2_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {

            if (e.Node.Text.Contains("https://"))
            {
                Clipboard.SetText(e.Node.Text);
            }
        }
    }
}
