using System;
using United.Mobile.Model.Common;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable]
    public class MasterpassSessionDetails
    {
        public string ContactEmailAddress { get; set; }
        public string ContactPhoneNumber { get; set; }
        public string GivenName { get; set; }
        public string SurName { get; set; }
        public string Status { get; set; }
        public string AccountNumber { get; set; }
        public string AccountNumberEncrypted { get; set; }
        public string AccountNumberHMAC { get; set; }
        public string AccountNumberLastFourDigits { get; set; }
        public string AccountNumberMasked { get; set; }
        public string AccountNumberToken { get; set; }
        public string PersistentToken { get; set; }
        public double Amount { get; set; }
        public MOBAddress BillingAddress { get; set; }
        public string OperationID { get; set; }
        public int WalletCategory { get; set; }
        public string Code { get; set; }
        public int CreditCardTypeCode { get; set; }
        public int Description { get; set; }
        public string ExpirationDate { get; set; }
        public string Name { get; set; }
        public MasterpassType MasterpassType { get; set; }
    }
    public class MasterpassType
    {
        public string DefaultIndicator { get; set; }
        public string Description { get; set; }
        public string Key { get; set; }
        public string Val { get; set; }
    }
}
