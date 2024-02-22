using System;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class ClubPKDispenserPublicKeyResponse : MOBResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public string PkDispenserPublicKey { get; set; } = string.Empty;
        public string MPNumber { get; set; } 
    }
}
