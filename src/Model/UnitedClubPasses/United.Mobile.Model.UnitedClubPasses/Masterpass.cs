using System;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class Masterpass
    {
        public string OauthToken { get; set; }
        public string Oauth_verifier { get; set; }
        public string CheckoutId { get; set; }
        public string CheckoutResourceURL { get; set; }
        public string Mpstatus { get; set; }
        public string CslSessionId { get; set; } = string.Empty;
    }
}
