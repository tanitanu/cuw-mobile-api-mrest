using System;
using United.Mobile.Model.Common;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable]
    public class PayPalPayor
    {
        public string PayPalCustomerID { get; set; }
        public string PayPalContactEmailAddress { get; set; }
        public string PayPalContactPhoneNumber { get; set; }
        public string PayPalGivenName { get; set; }
        public string PayPalSurName { get; set; }
        public string PayPalStatus { get; set; }  // EX: Description=VERIFIED
        public MOBAddress PayPalBillingAddress { get; set; }
    }
}
