using System;
using United.Mobile.Model.Common;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class UnitedClubMembershipRequest : MOBRequest
    {
        public string HashPinCode { get; set; } = string.Empty;
        public string MPNumber { get; set; } = string.Empty;
    }
}
