using System;

namespace United.Mobile.Model.ClubPasses
{
    [Serializable()]
    public class ClubEligibility
    {
        public string Eligibility { get; set; } = string.Empty;
        public string Guests { get; set; } = string.Empty;
    }
}
