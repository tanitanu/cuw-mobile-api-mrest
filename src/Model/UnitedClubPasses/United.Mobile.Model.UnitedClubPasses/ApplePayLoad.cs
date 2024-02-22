using System;
using System.Collections.Generic;

namespace United.Mobile.Model.UnitedClubPasses
{

    [Serializable()]
    public class ApplePayLoad
    {
        public string Version { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public List<Dictionary<string, string>> AdditionalAppleInfo { get; set; } 
        public Header Header { get; set; }

        public ApplePayLoad()
        {
            AdditionalAppleInfo = new List<Dictionary<string, string>>();
        }
    }

    [Serializable()]
    public class Header
    {
        public string EphemeralPublicKey { get; set; } = string.Empty;
        public string PublicKeyHash { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
    }
}
