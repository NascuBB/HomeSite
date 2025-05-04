namespace HomeSite.Entities
{
    public class SharedRightsDBO
    {
        public bool EditServerPreferences { get; set; }
        public bool EditMods { get; set; }
        public bool StartStopServer { get; set; }
        public bool UploadMods { get; set; }
        public bool SendCommands { get; set; }
        public bool AddShareds { get; set; }

        public static explicit operator SharedRightsDBO?(SharedRights? rights)
        {
            if (rights == null) return null;
            return new SharedRightsDBO
            {
                EditServerPreferences = rights.editserverpreferences,
                EditMods = rights.editmods,
                StartStopServer = rights.startstopserver,
                UploadMods = rights.uploadmods,
                SendCommands = rights.sendcommands,
                AddShareds = rights.addshareds
            };
        }
    }
}
