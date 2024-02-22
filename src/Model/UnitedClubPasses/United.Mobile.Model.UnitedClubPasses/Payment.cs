using System;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable]
    public class Payment
    {
        public string Key { get; set; }
        public string TransactionId { get; set; }
        public string ApplicationId { get; set; }
        public string ApplicationVersion { get; set; }
        public string PaymentType { get; set; }
        public double Amount { get; set; }
        public string CurrencyCode { get; set; }
        public int Mileage { get; set; }
        public string Remark { get; set; }
        public string InsertBy { get; set; }
        public string IsTest { get; set; }
        public string SessionId { get; set; }
        public string DeviceId { get; set; }
        public string RecordLocator { get; set; }
        public string MileagePlusNumber { get; set; }
        public string RESTAPIVersion { get; set; }
        public string FormOfPayment { get; set; }
    }
}
