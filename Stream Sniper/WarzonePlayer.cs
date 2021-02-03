using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stream_Sniper
{
    public class WarzonePlayer
    {

        public enum PlayerType
        {
            Warzone,
            Modern,
            Cold
        }

        public enum AccountType
        {
            Xbox,
            Play,
            Battle,
            Acti
        }

        public bool PlayerDataFound;
        public bool hasClanTag;
        public string PlayerName { get; set; }
        public string PlayerClanTag { get; set; }
    }
}
