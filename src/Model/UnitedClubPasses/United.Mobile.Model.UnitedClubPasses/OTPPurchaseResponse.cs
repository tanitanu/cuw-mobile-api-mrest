using System;
using System.Collections.Generic;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class OTPPurchaseResponse: MOBResponse
    {
        public OTPPurchaseRequest request { get; set; }
        public List<ClubDayPass> passes { get; set; }
        public string sessionId { get; set; } = string.Empty;
    }
}
