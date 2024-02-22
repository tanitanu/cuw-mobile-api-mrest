using System;
using System.Runtime.Serialization;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class CreditCard
    {
        public string Key { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty;
        public string CardTypeDescription { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ExpireMonth { get; set; } = string.Empty;
        public string ExpireYear { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public string UnencryptedCardNumber { get; set; } = string.Empty;
        public string EncryptedCardNumber { get; set; } = string.Empty;
        public string DisplayCardNumber { get; set; } = string.Empty;
        public string CIDCVV2 { get; set; } = string.Empty;
        public string CCName { get; set; } = string.Empty;
        public string AddressKey { get; set; } = string.Empty;
        public string PhoneKey { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string AccountNumberToken { get; set; } = string.Empty;
        public string PersistentToken { get; set; } = string.Empty;
        public string SecurityCodeToken { get; set; } = string.Empty;
        public string BarCode { get; set; } = string.Empty;
        public bool IsCorporate { get; set; }
        public bool IsMandatory { get; set; }
        public string BilledSeperateText { get; set; }
        public bool IsValidForTPIPurchase { get; set; }
        public bool IsOAEPPaddingCatalogEnabled { get; set; }
        private string kid;

        public string Kid
        {
            get { return kid; }
            set { kid = value; }
        }
    }

    [Serializable]
    public enum FormofPayment
    {
        [EnumMember(Value = "CreditCard")]
        CreditCard,
        [EnumMember(Value = "PayPal")]
        PayPal,
        [EnumMember(Value = "PayPalCredit")]
        PayPalCredit,
        [EnumMember(Value = "ApplePay")]
        ApplePay,
        [EnumMember(Value = "Masterpass")]
        Masterpass,
        [EnumMember(Value = "VisaCheckout")]
        VisaCheckout,
        [EnumMember(Value = "MilesFOP")]
        MilesFormOfPayment,
        [EnumMember(Value = "ETC")]
        ETC,
        [EnumMember(Value = "Uplift")]
        Uplift,
        [EnumMember(Value = "FFC")]
        FFC
    }

    [Serializable]
    public class VormetricKeys
    {
        public string AccountNumberToken { get; set; } = string.Empty;
        public string PersistentToken { get; set; } = string.Empty;
        public string SecurityCodeToken { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty;
        public string SecurityCode { get; set; } = string.Empty;
    }
}
