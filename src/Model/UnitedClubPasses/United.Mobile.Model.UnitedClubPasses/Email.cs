using System;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class Email
    {
        public string Key { get; set; } = string.Empty;
        public Channel Channel { get; set; }
        public string EmailAddress { get; set; } = string.Empty;
        public bool Ispublic { get; set; }
        public bool IsDefault { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsDayOfTravel { get; set; }
    }
}
