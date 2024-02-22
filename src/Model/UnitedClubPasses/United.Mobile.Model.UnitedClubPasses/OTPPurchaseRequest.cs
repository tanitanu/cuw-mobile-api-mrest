using System;
using System.Collections.Generic;
using United.Mobile.Model.Common;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class OTPPurchaseRequest :MOBRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MileagePlusNumber { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public int NumberOfPasses { get; set; }
        public bool TestSystem { get; set; }
        public string SessionId { get; set; }
        public double AmountPaid { get; set; }
        public CreditCard CreditCard { get; set; }
        public MOBAddress Address { get; set; }
        public PayPal PayPal { get; set; }
        public Masterpass Masterpass { get; set; }
        public FormofPayment FormOfPayment { get; set; }
        public ApplePay ApplePayInfo { get; set; }

        //Catalog Items from client
        private List<MOBItem> catalogItems = null;
        public List<MOBItem> CatalogItems
        {
            get { return catalogItems; }
            set { catalogItems = value; }
        }
    }
}
