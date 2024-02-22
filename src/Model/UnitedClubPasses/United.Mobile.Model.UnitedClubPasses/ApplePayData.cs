using System;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class ApplePayData
    {
        public string ApplicationPrimaryAccountNumber { get; set; } = string.Empty;
        public string ApplicationExpirationDate { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public int TransactionAmount { get; set; }
        public string DeviceManufacturerIdentifier { get; set; } = string.Empty;
        public string PaymentDataType { get; set; } = string.Empty;
        public ApplePayPaymentdata PaymentData { get; set; } 
    }
    [Serializable()]
    public class ApplePayPaymentdata
    {
        public string OnlinePaymentCryptogram { get; set; } = string.Empty;
        public string EciIndicator { get; set; } = string.Empty;
    }
}
