namespace UmaMusumeToolbox.DataDownload.Models
{
    public sealed class Settings
    {
        public bool IsDebugMode { get; set; }
        public string MasterDbFilepath { get; set; }
        public string MetaDbFilepath { get; set; }
        public bool SkipExistingFiles { get; set; }
        public int TimeoutFromMinutes { get; set; }
        public string UserSavedLocation { get; set; }
    }
}
