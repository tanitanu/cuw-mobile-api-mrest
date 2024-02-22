using System;

namespace United.Mobile.Model.ClubPasses
{
    [Serializable]
    public class ClubPassesRequest
    {
        public string AccessCode { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = "en-US";
        public MOBApplication Application { get; set; } = new MOBApplication();
        public string DeviceId { get; set; } = string.Empty;
        public string AirportCode { get; set; } = string.Empty;
        //[Required]
        public string SessionId { get; set; }
        public string ClubType { get; set; }
        public bool BFCOnly { get; set; }

        public ClubPassesRequest()
        {
            Application = new MOBApplication() { Version = new MOBVersion() };
        }
    }
}
