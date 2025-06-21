using System.ComponentModel.DataAnnotations.Schema;

namespace HomeSite.Entities
{
    [Table("shared")]
    public class SharedRights
    {
        [Column("userid")]
        public int UserId { get; set; }
        [Column("serverid")]
        public string ServerId { get; set; }
        [Column("editserverpreferences")]
        public bool EditServerPreferences { get; set; }
        [Column("editmods")]
        public bool EditMods { get; set; }
        [Column("startstopserver")]
        public bool StartStopServer { get; set; }
        [Column("uploadmods")]
        public bool UploadServer { get; set; }
        [Column("sendcommands")]
        public bool SendCommands { get; set; }
        [Column("addshareds")]
        public bool AddShareds { get; set; }
        [Column("seeserverfiles")]
        public bool SeeServerFiles { get; set; }
    }
}
