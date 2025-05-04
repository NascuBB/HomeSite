using System.ComponentModel.DataAnnotations.Schema;

namespace HomeSite.Entities
{
    [Table("shared")]
    public class SharedRights
    {
        public int userid { get; set; }
        public string serverid { get; set; }
        public bool editserverpreferences { get; set; }
        public bool editmods { get; set; }
        public bool startstopserver { get; set; }
        public bool uploadmods { get; set; }
        public bool sendcommands { get; set; }
        public bool addshareds { get; set; }
    }
}
