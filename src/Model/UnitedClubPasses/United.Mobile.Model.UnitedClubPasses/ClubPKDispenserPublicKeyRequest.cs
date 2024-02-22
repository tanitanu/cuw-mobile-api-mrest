using System;
using United.Mobile.Model.Common;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class ClubPKDispenserPublicKeyRequest : MOBRequest
    {
        public string MPNumber { get; set; } = string.Empty;
    }
}
